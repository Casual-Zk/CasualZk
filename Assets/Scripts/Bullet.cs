using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TarodevController;

public class Bullet : MonoBehaviour
{
    public int damage = 10;
    public float speed = 1f;
    public bool isOwner;

    private void FixedUpdate()
    {
        transform.position += transform.right * Time.deltaTime * speed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // If we are the owner and the hit object has a health component, hit that ass
        if (!isOwner && collision.gameObject.GetComponent<Health>())
        {
            // Can componenti varsa, PhotonView'?n? ça??r, ondaki PunRPC olarak i?aretledi?imiz
            // TakeDamage fonksiyonunu ça??r. PhotonView ile PunRPC fonksiyonunu ça??rd???m?z için,
            // bu i?lem tüm oyunculara gönderiliyor. Ama sadece hedef oyuncunun can? gidiyor tabi
            collision.gameObject.GetComponent<PhotonView>().RPC("TakeDamage", RpcTarget.All, damage);
        }

        
        Destroy(gameObject);
    }
}
