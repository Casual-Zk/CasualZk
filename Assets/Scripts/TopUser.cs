using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TopUser : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI order;
    [SerializeField] TextMeshProUGUI nickname;
    [SerializeField] TextMeshProUGUI communityCode;
    [SerializeField] TextMeshProUGUI matches;
    [SerializeField] TextMeshProUGUI eggs;

    public void AssignValues(int order, string communityCode, 
        string nickname, string matches, string eggs)
    {
        this.order.text = order.ToString() + ")";
        this.communityCode.text = communityCode;
        this.nickname.text = nickname;
        this.matches.text = matches;
        this.eggs.text = eggs;
    }
}
