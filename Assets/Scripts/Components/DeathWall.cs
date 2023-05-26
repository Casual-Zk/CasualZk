using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class DeathWall : MonoBehaviourPunCallbacks
{
    [SerializeField] int damage = 999;

    List<int> hitIDs = new List<int>();

    private void OnTriggerEnter2D(Collider2D collider)
    {
        GameObject obj = collider.gameObject;

        if (!obj.GetComponent<SimpleContoller>()) return;   // don't take action if the obj is not player

        // Don't hit target Colliders
        if (collider.isTrigger) return;

        // Prevent double death by double collider
        if (hitIDs.Contains(obj.GetInstanceID())) return;
        hitIDs.Add(obj.GetInstanceID());

        // If we are the owner and the hit object has a health component, hit that ass
        if (obj.GetComponent<Health>())
        {
            string target = obj.GetComponent<PhotonView>().Controller.NickName;
            obj.GetComponent<PhotonView>().RPC("TakeDamage", RpcTarget.All, 999, "DeathWall", target);
        }
        else
        {
            Destroy(obj); 
        }
    }
}
