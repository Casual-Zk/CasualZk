using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TopUser : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI order;
    [SerializeField] TextMeshProUGUI wallet;
    [SerializeField] TextMeshProUGUI nickname;
    [SerializeField] TextMeshProUGUI matches;
    [SerializeField] TextMeshProUGUI eggs;

    public void AssignValues(int order, string wallet, string nickname, string matches, string eggs)
    {
        this.order.text = order.ToString() + ")";
        this.wallet.text = wallet;
        this.nickname.text = nickname;
        this.matches.text = matches;
        this.eggs.text = eggs;
    }
}
