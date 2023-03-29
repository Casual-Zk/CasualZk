using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class Health : MonoBehaviour
{
    [SerializeField] int health;
    [SerializeField] Slider slider;

    [PunRPC]
    public void TakeDamage(int _damage)
    {
        health -= _damage;

        if (health <= 0)
        {
            Destroy(gameObject);
        }
        
        slider.value = health;
    }
}
