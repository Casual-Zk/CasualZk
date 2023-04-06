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
        MenuCanvas.enabled = true;
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
        if(Nickname_Input.text != ""){
            Debug.Log("Nickname :"+Nickname_Input.text);
            PlayerPrefs.SetString("Nickname", Nickname_Input.text);
            return true;
        }
        else{
            Debug.Log("Nickname is null");
            return false;
        }
    }
        
}
