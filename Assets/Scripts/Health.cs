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
    [SerializeField] PlayerController controller;

    MatchManager matchManager;

    private void Start()
    {
        matchManager = FindObjectOfType<MatchManager>();
    }

    [PunRPC]
    public void TakeDamage(int damage, string shooterName)
    {
        Debug.Log("Shooter: " + shooterName);
        if (matchManager.isGameOver) return; // Don't get hurt if the time is up

        health -= damage;

        if (health <= 0)
        {
            Debug.Log("Killer: " + shooterName);
            int score = 1;

            if (shooterName.Contains("DeathWall")) 
            { 
                shooterName = shooterName.Split('/')[0];
                score = -1;
                Debug.LogError("Death wall hit by " + shooterName);
            }

            matchManager.GetComponent<PhotonView>().RPC("AddPlayerScore", RpcTarget.Others, shooterName, score);

            if (controller.isOwner)
            {
                RoomManager.Instance.RespawnPlayer();
            }
            Destroy(gameObject);
        }
        
        slider.value = health;
    }
}
