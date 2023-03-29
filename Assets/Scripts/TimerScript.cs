using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using TMPro;

public class TimerScript : MonoBehaviour
{

    [SerializeField] float time;
    [SerializeField] TextMeshProUGUI timeText;

    void Update()
    {
        if (time < 0f) return;

        // Update the timer every second
        time -= Time.deltaTime;
        timeText.text = ((int)time).ToString();
        
        if (time < 0f)
        {
            FindObjectOfType<MatchManager>().isGameOver = true;
        }
    }

    // Send timer update events to other players
    public void SendTimerUpdate()
    {
        object[] data = new object[] { time }; // Send the current timer value as a parameter
        PhotonNetwork.RaiseEvent(1, data, RaiseEventOptions.Default, SendOptions.SendReliable);
    }
}
