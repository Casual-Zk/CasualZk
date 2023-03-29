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
    public void TakeDamage(int _damage)
    {
        if (matchManager.isGameOver) return; // Don't get hurt if the time is up

        health -= _damage;

        if (health <= 0)
        {
            if (controller.isOwner)
            {
                RoomManager.Instance.RespawnPlayer();
            }
            Destroy(gameObject);
        }
        
        slider.value = health;
    }
}
