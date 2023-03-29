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

    [PunRPC]
    public void TakeDamage(int _damage)
    {
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
