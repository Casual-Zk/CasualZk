using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private GameObject RoomManager,MatchManager;

    public void FindMatchButton(){
        //RoomManager.SetActive(true);
        MatchManager.SetActive(true);
        gameObject.SetActive(false); 
    }
}
