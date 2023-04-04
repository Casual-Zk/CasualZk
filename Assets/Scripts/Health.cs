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
        //Debug.Log("Shooter: " + shooterName);
        if (matchManager.isGameOver) return; // Don't get hurt if the time is up

        health -= damage;

        if (health <= 0)
        {
            audioManager.Play("Chicken_Death");

            //Debug.Log("Killer: " + shooterName);

            matchManager.GetComponent<PhotonView>().RPC("ScoreEvent", RpcTarget.Others, shooterName, targetName);

            if (controller.isOwner)
            {
                RoomManager.Instance.RespawnPlayer();
            }
            Destroy(gameObject);
        }
        
        slider.value = health;
    }
}
