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
    [SerializeField] private TMP_InputField Nickname_Input;

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

    public void FindMatchButton(){
        if (check_nickname())
        {
            roomManager.FindMatch();
            MenuCanvas.enabled = false;
        }
        
    }
    private bool check_nickname(){
        string nickname = Nickname_Input.text;

        if(nickname != ""){
            Debug.Log("Nickname :" + nickname);
            PlayerPrefs.SetString("Nickname", nickname);
            //FindObjectOfType<FirebaseDataManager>().UpdateNickname(nickname);
            return true;
        }
        else{
            Debug.Log("Nickname is null");
            return false;
        }
    }
        
}
