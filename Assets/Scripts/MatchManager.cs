using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TarodevController;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using TMPro;

public class MatchManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public bool isGameOver { get; set; }

    [SerializeField] float matchTime;
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] TextMeshProUGUI playerA;
    [SerializeField] TextMeshProUGUI playerB;

    private float time;
    private Coroutine timerCoroutine;

    List<CasualPlayer> players = new List<CasualPlayer>();

    public enum EventCodes : byte
    {
        RefreshTimer
    }
    public enum GameState
    {
        Waiting = 0,
        Starting = 1,
        Playing = 2,
        Ending = 3
    }

    void Start()
    {
        PhotonNetwork.AddCallbackTarget(this);
        InitializeTimer(); 
    }

    void Update()
    {
    }
    void OnDestroy()
    {
        // Unsubscribe from the OnMasterClientSwitched callback
        PhotonNetwork.RemoveCallbackTarget(this);
    }
    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room CALLED");
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("I am the master client");
            InitializeTimer();
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log("Master Switched");
        // Check if the current player is the new master client
        if (newMasterClient == PhotonNetwork.LocalPlayer)
        {
            Debug.Log("Local is the master");
            InitializeTimer();
        }
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code >= 200) return;

        EventCodes eventCode = (EventCodes) photonEvent.Code;
        object[] eventData = (object[])photonEvent.CustomData;

        switch (eventCode)
        {
            case EventCodes.RefreshTimer:
                RefreshTimer_Receive(eventData);
                break;
        }
    }

    private void EndGame()
    {
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        time = 0;
        RefreshTimerUI();
    }

    // ------------- TIMER ------------- //

    private void InitializeTimer()
    {
        time = matchTime;
        RefreshTimerUI();

        Debug.Log("initilizing Timer");

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("This is the master client");
            timerCoroutine = StartCoroutine(Timer());
        }
    }

    private void RefreshTimerUI()
    {
        timeText.text = ((int)time).ToString();
    }

    private IEnumerator Timer()
    {
        yield return new WaitForSeconds(1f);
        time--;

        if (time <= 0)
        {
            Debug.Log("Time is up");
            timerCoroutine = null;
            UpdatePlayers_Send(0); // Game over

            timeText.enabled = false;
            isGameOver = true;
        }
        else
        {
            RefreshTimer_Send();
            timerCoroutine = StartCoroutine(Timer());
        }
    }

    private void UpdatePlayers_Send(int state)
    {

    }

    private void RefreshTimer_Send()
    {
        object[] package = new object[] { time };

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.RefreshTimer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }

    private void RefreshTimer_Receive(object[] data)
    {
        time = (float)data[0];
        if (time < 2) isGameOver = true;
        RefreshTimerUI();
    }


    // ------------- SCORE ------------- //
    [PunRPC]
    public void AddPlayer(string playerNickname)
    {
        CasualPlayer newPlayer = new CasualPlayer();
        newPlayer.nickName = playerNickname;

        Debug.LogError("New player: " + playerNickname);

        players.Add(newPlayer);

        foreach (CasualPlayer player in players) 
        {
            photonView.RPC("UpdatePlayers", RpcTarget.Others, player.nickName);
        }


        photonView.RPC("RefreshPlayerScoreUI", RpcTarget.Others);
    }

    [PunRPC]
    public void UpdatePlayers(string playerNickname)
    {
        bool inTheList = false;

        foreach (CasualPlayer player in players)
        {
            if (player.nickName == playerNickname) inTheList = true;
        }

        if (!inTheList)
        {
            Debug.LogError("I haven't had " + playerNickname + " but added now!");
            AddPlayer(playerNickname);
        }
    }

    [PunRPC]
    public void AddPlayerScore(string playerName, int scoreToAdd)
    {
        Debug.LogError("Adding Score: " + playerName + " : " + scoreToAdd);

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].nickName == playerName)
            {
                players[i].score += scoreToAdd;
                break;
            }
        }

        photonView.RPC("RefreshPlayerScoreUI", RpcTarget.Others);
    }

    [PunRPC]
    public void RefreshPlayerScoreUI()
    {
        Debug.LogError("Refresing score UI");

        if (players.Count == 1) 
        {
            playerA.text = players[0].nickName + " : " + players[0].score; 
        }
        if (players.Count == 2) 
        {
            playerA.text = players[0].nickName + " : " + players[0].score; 
            playerB.text = players[1].nickName + " : " + players[1].score;
        }
    }


}
