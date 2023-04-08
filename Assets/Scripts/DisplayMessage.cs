using System.Collections;
using UnityEngine;
using TMPro;

public class DisplayMessage : MonoBehaviour
{
    [SerializeField] GameObject messageObject;
    [SerializeField] TextMeshProUGUI messageText;

    private void Start()
    {
        messageText.text = "";
        messageObject.SetActive(false);
    }

    public void Display(string msg, float duration)
    {
        StartCoroutine(DisplayCoroutine(msg, duration));
    }

    IEnumerator DisplayCoroutine(string msg, float duration)
    {
        messageObject.SetActive(true);
        messageText.text = msg;

        yield return new WaitForSeconds(duration);

        messageText.text = "";
        messageObject.SetActive(false);
    }
}
