using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private GameObject RoomManager,MatchManager;
    [SerializeField] private TMP_InputField Nickname_Input;

    public void FindMatchButton(){
        if(check_nickname()){
        RoomManager.SetActive(true);
        MatchManager.SetActive(true);
        gameObject.SetActive(false); 
        }
        
    }
    private bool check_nickname(){
        if(Nickname_Input.text != ""){
        Debug.Log("Nickname :"+Nickname_Input.text);    
        return true;
        }
        else{
        Debug.Log("Nickname is null");
        return false;
        }
    }
        
}
