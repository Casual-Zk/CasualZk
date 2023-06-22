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
    [SerializeField] SimpleContoller controller;
    [SerializeField] Image hitImage;
    [SerializeField] bool DEBUG_getHit;
    float closeSpeed;

    bool deadAlready;

    MatchManager matchManager;
    AudioManager audioManager;

    private void Start()
    {
        matchManager = FindObjectOfType<MatchManager>();
        audioManager = FindObjectOfType<AudioManager>();

        closeSpeed = FindAnyObjectByType<FirebaseDataManager>().dv.hitEffectSpeed;
    }

    private void Update()
    {
        if (DEBUG_getHit) 
        {
            TakeDamage(5, "", "");
            DEBUG_getHit = false;
        }
        // If we have an alpha other then 0, then decrease it
        if (hitImage.color.a != 0)
        {
            //Debug.Log("Current Alpha: " + hitImage.color.a);

            // Decrease and Limit the alpha between 0 and MaxValue
            float newAlpha = Mathf.Clamp(hitImage.color.a - (closeSpeed * Time.deltaTime), 0, 1f);

            // Apply the new alpha value
            hitImage.color = new Color(1f, 1f, 1f, newAlpha);
        }
    }

    public void SetHealth(int health)
    {
        if (health <= 0) return;

        this.health = health;
        slider.maxValue = health;
        slider.value = health;
    }

    [PunRPC]
    public void TakeDamage(int damage, string shooterName, string targetName)
    {
        if (deadAlready) return;    // avoid multi death

        //Debug.Log("Shooter: " + shooterName);
        if (matchManager == null) matchManager = FindObjectOfType<MatchManager>();
        if (matchManager.isGameOver) return; // Don't get hurt if the time is up

        health -= damage;

        // Show hit effect to the client
        if (controller.isOwner)
        {
            hitImage.color = new Color(1f, 1f, 1f, 1f);
            if (health > 0) controller.HitCamShake();
        }

        if (health <= 0)
        {
            deadAlready = true;
            audioManager.Play("Chicken_Death");

            //Debug.Log("Killer: " + shooterName);


            if (controller.isOwner)
            {
                FindObjectOfType<FirebaseDataManager>().UpdateAmmoBalance(); // Save ammo balance to DB before die

                matchManager.GetComponent<PhotonView>().RPC("ScoreEvent", RpcTarget.All, shooterName, targetName);
                RoomManager.Instance.RespawnPlayer();
            }
            Destroy(gameObject);
        }
        
        slider.value = health;
    }
}
