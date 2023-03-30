using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CasualPlayer : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;

    public void SetName()
    {
        nameText.text = PlayerPrefs.GetString("Nickname");
    }
}
