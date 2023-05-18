using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TarodevController;

public class Bullet : MonoBehaviourPunCallbacks
{
    public bool isOwner { get; set; }
    public string ownerName { get; set; }

    [SerializeField] int damage = 10;
    [SerializeField] float speed = .1f;

    bool hit;

    void Start()
    {
        StartCoroutine(DestroyObjectAfterDelay());
    }

    private void FixedUpdate()
    {
        transform.position += transform.right * Time.deltaTime * speed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Avoid bullets hitting each other
        if (collision.GetComponent<Bullet>() != null) return;

        // Prevent double hit
        if (hit) return;
        hit = true;

        GameObject obj = collision.gameObject;

        // If we are the owner and the hit object has a health component but not a owner(myself), hit that ass
        if (isOwner && obj.GetComponent<Health>())
        {
            string targetName = obj.GetComponent<PhotonView>().Controller.NickName;
            // Don't hit yourself
            if (!obj.GetComponent<SimpleContoller>().isOwner)
                collision.gameObject.GetComponent<PhotonView>().RPC(
                    "TakeDamage", RpcTarget.All, damage, ownerName, targetName);
        }
        
        Destroy(gameObject);
    }

    [PunRPC]
    public void SetOwner(string owner)
    {
        ownerName = owner;
    }

    // Destroy bullet after 1 second to avoid buggy behavior.
    IEnumerator DestroyObjectAfterDelay()
    {
        yield return new WaitForSeconds(1f); // Wait for one second

        // Destroy the object
        Destroy(gameObject);
    }
}
