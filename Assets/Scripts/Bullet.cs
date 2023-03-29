using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TarodevController;

public class Bullet : MonoBehaviour
{
    public bool isOwner { get; set; }

    [SerializeField] int damage = 10;
    [SerializeField] float speed = 1f;    

    private void FixedUpdate()
    {
        transform.position += transform.right * Time.deltaTime * speed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // If we are the owner and the hit object has a health component, hit that ass
        if (!isOwner && collision.gameObject.GetComponent<Health>())
        {
            // Can componenti varsa, PhotonView'?n? �a??r, ondaki PunRPC olarak i?aretledi?imiz
            // TakeDamage fonksiyonunu �a??r. PhotonView ile PunRPC fonksiyonunu �a??rd???m?z i�in,
            // bu i?lem t�m oyunculara g�nderiliyor. Ama sadece hedef oyuncunun can? gidiyor tabi
            collision.gameObject.GetComponent<PhotonView>().RPC("TakeDamage", RpcTarget.All, damage);
        }
        
        Destroy(gameObject);
    }
}
