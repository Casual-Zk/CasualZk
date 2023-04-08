using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class MenuManager : MonoBehaviour
{
    MatchManager matchManager;
    RoomManager roomManager;

    [SerializeField] Canvas MenuCanvas;
    [SerializeField] GameObject setNicknameUI;
    [SerializeField] TMP_InputField Nickname_Input;
    [SerializeField] TextMeshProUGUI nicknameText;

    private void Start()
    {
        StartMenu();
    }

    public void StartMenu()
    {
        matchManager = FindObjectOfType<MatchManager>();
        roomManager = FindObjectOfType<RoomManager>();
        if (PlayerPrefs.HasKey("Nickname")) Nickname_Input.text = PlayerPrefs.GetString("Nickname");
    }

    public void Btn_SetNickname()
    {
        FindAnyObjectByType<FirebaseDataManager>().SetNickname(Nickname_Input.text);
    }

    public void DisplayNickname(string nickname)
    {
        if (nickname != null)
        {
            setNicknameUI.SetActive(false);
            nicknameText.text = nickname;
            PlayerPrefs.SetString("Nickname", nickname);
        }
        else
        {
            setNicknameUI.SetActive(true);
            nicknameText.text = "";
            PlayerPrefs.DeleteKey("Nickname");
        }
    }

    public void FindMatchButton(){
        if (PlayerPrefs.HasKey("Nickname"))
        {
            roomManager.FindMatch();
            MenuCanvas.enabled = false;
        }
        
    }
        
}
