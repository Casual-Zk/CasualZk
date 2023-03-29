using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class DeathWall : MonoBehaviourPunCallbacks
{
    [SerializeField] int damage = 999;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // If we are the owner and the hit object has a health component, hit that ass
        if (collision.gameObject.GetComponent<Health>())
        {
            collision.gameObject.GetComponent<PhotonView>().RPC("TakeDamage", RpcTarget.All, 999);
        }
        else
        {
            Destroy(collision.gameObject);
        }
    }
}
