using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TarodevController;

public class Health : MonoBehaviourPunCallbacks
{
    [SerializeField] int health;
    [SerializeField] Slider slider;
    [SerializeField] SimpleContoller controller;

    bool deadAlready;

    MatchManager matchManager;
    AudioManager audioManager;

    private void Start()
    {
        matchManager = FindObjectOfType<MatchManager>();
        audioManager = FindObjectOfType<AudioManager>();
    }

    [PunRPC]
    public void TakeDamage(int damage, string shooterName, string targetName)
    {
        if (deadAlready) return;    // avoid multi death

        //Debug.Log("Shooter: " + shooterName);
        if (matchManager.isGameOver) return; // Don't get hurt if the time is up

        health -= damage;

        if (health <= 0)
        {
            deadAlready = true;
            audioManager.Play("Chicken_Death");

            //Debug.Log("Killer: " + shooterName);


            if (controller.isOwner)
            {
                matchManager.GetComponent<PhotonView>().RPC("ScoreEvent", RpcTarget.All, shooterName, targetName);
                RoomManager.Instance.RespawnPlayer();
            }
            Destroy(gameObject);
        }
        
        slider.value = health;
    }
}
