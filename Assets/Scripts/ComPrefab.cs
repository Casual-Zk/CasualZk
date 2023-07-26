using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ComPrefab : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI orderText;
    [SerializeField] TextMeshProUGUI codeText;
    [SerializeField] TextMeshProUGUI scoreText;

    public void DisplayValues(int order, string code, string score)
    {
        this.orderText.text = order.ToString() + ")";
        this.codeText.text = code;
        this.scoreText.text = score.ToString();
    }
}
