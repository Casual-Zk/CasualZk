using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TarodevController;
using Photon.Realtime;
using TMPro;

public class RoomManager : MonoBehaviourPunCallbacks
{
    public static RoomManager Instance;

    [SerializeField] GameObject player;
    [Space][SerializeField] Transform[] spawnPoints;
    [SerializeField] GameObject connectingUI;
    [SerializeField] TextMeshProUGUI roomNameText;
    [SerializeField] float playerRespawnTime;

    int duelCounter;
    bool duelRoomFound;

    MatchManager matchManager;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Debug.Log("Connecting...");
        PhotonNetwork.ConnectUsingSettings();
    }

    private void Update()
    {
        if (matchManager == null) FindObjectOfType<MatchManager>();
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        Debug.Log("Connected to server");
        PhotonNetwork.JoinLobby();  
    }

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();
        Debug.Log("We're in the lobby.");        
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        duelCounter = 0;

        foreach (RoomInfo room in roomList)
        {
            Debug.LogError("Room Found: " + room.Name);

            if (room.Name.Contains("Duel")) duelRoomFound = true;
            if (room.Name.Contains("Duel") && room.PlayerCount >= 2) duelCounter++;
        }

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 4;

        if (!duelRoomFound) PhotonNetwork.JoinOrCreateRoom("Duel_0", roomOptions, TypedLobby.Default);
        else PhotonNetwork.JoinOrCreateRoom("Duel_" + duelCounter, roomOptions, TypedLobby.Default);
    }



    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;

        Debug.Log("We're connected and in a room!");
        connectingUI.SetActive(false);
        SpawnPlayer();
        FindAnyObjectByType<MatchManager>().GetComponent<PhotonView>().RPC(
            "AddPlayer", RpcTarget.All, PlayerPrefs.GetString("Nickname"));
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
        _player.GetComponent<PlayerCamera>().enabled = true;
        _player.GetComponent<PhotonView>().Controller.NickName = PlayerPrefs.GetString("Nickname");
    }
}
