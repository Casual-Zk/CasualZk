using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;

public class CasualPlayer : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;

    public string nickName { get; private set; }
    public string playerScore { get; set; }

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
