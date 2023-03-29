using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TarodevController;
using Photon.Pun;
using TMPro;

public class MatchManager : MonoBehaviourPunCallbacks
{
    public bool isGameOver { get; set; }

    void Start()
    {
        InvokeRepeating("SendTimerUpdate", 0f, 1f); // Send timer update events every second
    }

    private void SendTimerUpdate()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // If the current player is the master client, send the timer update event
            FindObjectOfType<TimerScript>().SendTimerUpdate();
        }
    }

}
