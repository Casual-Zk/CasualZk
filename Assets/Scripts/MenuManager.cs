using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private GameObject RoomManager;

    public void FindMatchButton(){
        RoomManager.SetActive(true);
        gameObject.SetActive(false); 
    }
}
