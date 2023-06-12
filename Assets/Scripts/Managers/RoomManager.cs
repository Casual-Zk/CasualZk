using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TarodevController;
using Photon.Realtime;
using TMPro;
using Cinemachine;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager Instance;

    [SerializeField] GameObject player;
    [Space][SerializeField] Transform[] spawnPoints;
    [SerializeField] public GameObject connectingUI;
    [SerializeField] TextMeshProUGUI roomNameText;
    [SerializeField] float playerRespawnTime;
    [SerializeField] CinemachineVirtualCamera player_v_cam;

    int roomCounter;
    int onlineCounter;

    MatchManager matchManager;
    FirebaseDataManager dataManager;
    MenuManager menuManager;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        dataManager = FindObjectOfType<FirebaseDataManager>();
        matchManager = FindObjectOfType<MatchManager>();
        menuManager = FindObjectOfType<MenuManager>();

        if (matchManager.GetComponent<PhotonView>().IsMine)
        {
            Debug.Log("Connecting to server...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public void FindMatch()
    {
        if (!matchManager.GetComponent<PhotonView>().IsMine) return;
        if (PhotonNetwork.IsConnected)
            PhotonNetwork.JoinLobby();
        else
        {
            Debug.LogError("NOT CONNECTED TO MASTER SERVER !!!");
            FindObjectOfType<DisplayMessage>().Display("Not connected to server!", 3f);
            return;
        }

        connectingUI.SetActive(true);
        roomNameText.enabled = false;        
    }

    private void Update()
    {
        if (matchManager == null) matchManager = FindObjectOfType<MatchManager>();
        if (!matchManager.isGameOver) connectingUI.SetActive(false);    // Prevent connectiong UI to open if the game is on!

        // If connected and online counter has changed, update it
        if (PhotonNetwork.IsConnected && onlineCounter != PhotonNetwork.CountOfPlayers)
        {
            onlineCounter = PhotonNetwork.CountOfPlayers;
            menuManager.UpdateOnlineCounter(onlineCounter);
        }
    }

    public void UpdateCounter()
    {
        onlineCounter = PhotonNetwork.CountOfPlayers;
        menuManager.UpdateOnlineCounter(onlineCounter);
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        Debug.Log("Connected to server");

        onlineCounter = PhotonNetwork.CountOfPlayers;
        menuManager.UpdateOnlineCounter(onlineCounter);
    }

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();
        Debug.Log("We're in the lobby.");        
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        roomCounter = 0;
        int maxPlayerAmount = dataManager.gameInfo.playerAmount;

        foreach (RoomInfo room in roomList)
        {
            Debug.Log("Room Found: " + room.Name);

            if (room.Name.Contains("Room") && room.PlayerCount >= maxPlayerAmount) roomCounter++;
        }

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = (byte)maxPlayerAmount;
        PhotonNetwork.JoinOrCreateRoom("Room_" + roomCounter, roomOptions, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        roomNameText.enabled = true;
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;

        Debug.Log("We're connected and in a room!");
        StartCoroutine(CloseConnectingUIOnDelay());
        SpawnPlayer();

        FindAnyObjectByType<MatchManager>().StartMatch();
        FindAnyObjectByType<MatchManager>().GetComponent<PhotonView>().RPC(
            "AddPlayer", RpcTarget.All, PlayerPrefs.GetString("Nickname"));
    }

    IEnumerator CloseConnectingUIOnDelay()
    {
        yield return new WaitForSeconds(2f);
        connectingUI.SetActive(false);
        matchManager.waitingPlayersCanvas.SetActive(true);
    }

    public void RespawnPlayer()
    {
        StartCoroutine(Respawn());
    }
    
    IEnumerator Respawn()
    {
        yield return new WaitForSeconds(playerRespawnTime);
        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject _player = PhotonNetwork.Instantiate(player.name, sp.position, Quaternion.identity);
        _player.GetComponent<SimpleContoller>().isOwner = true;
        //_player.GetComponent<PlayerCamera>().enabled = true;
        player_v_cam.Follow = _player.transform;
        _player.GetComponent<PhotonView>().Controller.NickName = PlayerPrefs.GetString("Nickname");
    }
}
