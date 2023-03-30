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

    private float time;
    private Coroutine timerCoroutine;

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
        InitializeTimer();
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

    private void InitializeTimer()
    {
        time = matchTime;
        RefreshTimerUI();

        Debug.Log("initilizing Timer");

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Is Master Client");
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
}
