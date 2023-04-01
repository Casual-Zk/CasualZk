using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class DeathWall : MonoBehaviourPunCallbacks
{
    [SerializeField] int damage = 999;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        GameObject obj = collision.gameObject;
        // If we are the owner and the hit object has a health component, hit that ass
        if (obj.GetComponent<Health>())
        {
            string killer = obj.GetComponent<PhotonView>().Controller.NickName + "/DeathWall";
            obj.GetComponent<PhotonView>().RPC("TakeDamage", RpcTarget.All, 999, killer);
        }
        else
        {
            Destroy(obj);
        }
    }
}
