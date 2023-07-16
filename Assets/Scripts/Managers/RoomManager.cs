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
    [SerializeField] GameObject cancelPanel;
    [SerializeField] LayerMask playerLayer;
    [SerializeField] float spXarea;
    [SerializeField] float spYarea;

    int onlineCounter;
    bool waitingToTryAgain;

    MatchManager matchManager;
    FirebaseDataManager dataManager;
    MenuManager menuManager;

    private void Awake()
    {
        Instance = this;
    }

    public void Start()
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
        Debug.Log("On FindMatch in RoomManager");
        if (waitingToTryAgain) return;
        
        Debug.Log("Continue to check master connection");
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("Connected to master! Clearing the game and joining random room!");
            matchManager.ClearGame(false);
            PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            Debug.Log("No master connection! Trying to connect again!");
            // If we are not connected at the moment, try it once more
            StartCoroutine(TryAgainToFindMatch());
            FindObjectOfType<DisplayMessage>().Display("Not connected to server! Trying to re-connect in 3 seconds!", 3f);
            menuManager.SetMenuCanvas(true, false);
            return;
        }

        Debug.Log("Opening the Finding Match transition scene!");

        // Starting to join a game
        menuManager.SetMenuCanvas(false, false);
        connectingUI.SetActive(true);
        cancelPanel.SetActive(false);  // first close the button
        StartCoroutine(ShowcancelPanel()); // Open if after a while
        roomNameText.enabled = false;        
    }

    private IEnumerator TryAgainToFindMatch()
    {
        Debug.LogWarning("Trying to reconnect and find match again!");

        waitingToTryAgain = true;

        Debug.Log("Connecting to server...");
        PhotonNetwork.ConnectUsingSettings();

        yield return new WaitForSeconds(3f);

        waitingToTryAgain = false;
        FindMatch();
    }

    public void Btn_CancelMatchMaking()
    {
        try { PhotonNetwork.Disconnect(); } catch { Debug.LogError("Cancel MM, Can't disconnect!"); }

        menuManager.SetMenuCanvas(true, true);
        connectingUI.SetActive(false);
        roomNameText.enabled = false;
    }
    private IEnumerator ShowcancelPanel()
    {
        yield return new WaitForSeconds(dataManager.dv.leaveWaitTime * 1.5f);

        cancelPanel.SetActive(true);  // first close the button
    }

    private void Update()
    {
        if (matchManager == null) matchManager = FindObjectOfType<MatchManager>();
        if (!matchManager.isGameOver) connectingUI.SetActive(false);    // Prevent connectiong UI to open if the game is on!

        // If connected and online counter has changed, update it
        if (PhotonNetwork.IsConnected && onlineCounter != PhotonNetwork.CountOfPlayers)
        {
            menuManager.UpdateOnlineCounter(PhotonNetwork.CountOfPlayers);
        }
    }

    public void UpdateCounter()
    {
        menuManager.UpdateOnlineCounter(PhotonNetwork.CountOfPlayers);
    }
    
    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        Debug.Log("Connected to server");

        menuManager.UpdateOnlineCounter(PhotonNetwork.CountOfPlayers);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogError("Join Random Room Failed! No random room available, so we create one.\nCalling: PhotonNetwork.CreateRoom");

        // Set the max player
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = (byte)dataManager.gameInfo.playerAmount;

        // Set random room name
        string roomName = "Room_" + Random.Range(0, 100000);

        // Create the room
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        base.OnCreateRoomFailed(returnCode, message);

        Debug.LogError("RETURN CODE: " + returnCode + " MESSAGE: " + message);

        Debug.LogWarning("Trying to join a room AGAIN!");
        
        if (PhotonNetwork.IsConnected)
            PhotonNetwork.JoinRandomRoom();
        else
            StartCoroutine(TryAgainToFindMatch());
    }

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();
        Debug.Log("We're in the lobby.");        
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        /*
        roomCounter = 0;
        int maxPlayerAmount = dataManager.gameInfo.playerAmount;

        foreach (RoomInfo room in roomList)
        {
            Debug.LogWarning("Room Found: " + room.Name);

            if (room.Name.Contains("Room") && room.PlayerCount >= maxPlayerAmount) roomCounter++;
        }

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = (byte)maxPlayerAmount;
        PhotonNetwork.JoinOrCreateRoom("Room_" + roomCounter, roomOptions, TypedLobby.Default);
        */
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        cancelPanel.SetActive(false);
        roomNameText.enabled = true;
        roomNameText.text = PhotonNetwork.CurrentRoom.Name;

        Debug.Log("We're connected and in a room!");
        StartCoroutine(CloseConnectingUIOnDelay());
        SpawnPlayer();

        FindObjectOfType<MatchManager>().StartMatch();
        FindObjectOfType<MatchManager>().GetComponent<PhotonView>().RPC(
            "AddPlayer", RpcTarget.All, PlayerPrefs.GetString("Nickname"));

        Debug.LogWarning("CONNECTED REGION: " + PhotonNetwork.CloudRegion);
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
        Collider2D[] enemies = null;
        Transform sp = null;
        float distance = 0f;

        // Get random spawn point until you find somewhere empty
        do
        {
            sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
            //Debug.Log("SP:" + sp.gameObject.name);
            enemies = Physics2D.OverlapBoxAll(sp.position, new Vector3(spXarea, spYarea, 0f), 0f, playerLayer);
            if (enemies.Length > 0) 
            {
                // get the first one's distance
                distance = Vector3.Distance(enemies[0].transform.position, sp.position);

                // compare it others and find the nearst
                foreach (Collider2D col in enemies)
                {
                    float curDist = Vector3.Distance(col.transform.position, sp.position);
                    if (curDist < distance) { distance = curDist; }
                }
                //Debug.Log("Distance of the nearest enemy: " + distance);
            }            
        }
        // if an enemy collider detected and their nearst one is in the sp, when pick another one
        while (enemies.Length > 0 && distance < spXarea / 2);   

        GameObject _player = PhotonNetwork.Instantiate(player.name, sp.position, Quaternion.identity);
        _player.GetComponent<SimpleContoller>().isOwner = true;
        //_player.GetComponent<PlayerCamera>().enabled = true;
        player_v_cam.Follow = _player.transform;
        _player.GetComponent<PhotonView>().Controller.NickName = PlayerPrefs.GetString("Nickname");
    }

    private void OnDrawGizmos()
    {
        foreach (Transform sp in spawnPoints)
        {
            Gizmos.DrawWireCube(sp.position, new Vector3(spXarea, spYarea, 0f));
        }
    }
}
