using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

public class CasualPlayer : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;

    public PlayerInfo playerInfo { get; set; }
    public int score { get; set; }

    private void Start()
    {
        if (!GetComponent<PhotonView>().IsMine)
        {
            nameText.text = GetComponent<PhotonView>().Controller.NickName;
        }
        else
        {
            nameText.text = PlayerPrefs.GetString("Nickname");
        }
    }
}
