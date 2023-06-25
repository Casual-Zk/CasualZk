using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;

public class Health : MonoBehaviourPunCallbacks
{
    [SerializeField] int health;
    [SerializeField] int armor;
    [SerializeField] Slider slider;
    [SerializeField] Slider armorSlider;
    [SerializeField] GameObject armorObject;
    [SerializeField] TextMeshProUGUI armorAmountText;
    [SerializeField] SimpleContoller controller;
    [SerializeField] Image hitImage;
    [SerializeField] bool DEBUG_getHit;

    float closeSpeed;
    float armorChance;

    bool deadAlready;

    MatchManager matchManager;
    AudioManager audioManager;
    FirebaseDataManager dm;

    private void Start()
    {
        matchManager = FindObjectOfType<MatchManager>();
        audioManager = FindObjectOfType<AudioManager>();
        dm = FindObjectOfType<FirebaseDataManager>();

        closeSpeed = dm.dv.hitEffectSpeed;
        armorChance = dm.dv.player_ArmorChance;
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
    public void SetArmor()
    {
        // Display armor amount in the inventory
        armorAmountText.text = dm.playerInfo.game_Armor.ToString();

        if (dm.playerInfo.game_lastArmorHealth <= 0) return;

        armorObject.SetActive(true);

        this.armor = dm.playerInfo.game_lastArmorHealth;
        armorSlider.value = dm.playerInfo.game_lastArmorHealth;
        armorSlider.maxValue = dm.dv.player_Armor;
    }

    [PunRPC]
    public void UseArmor()
    {
        armorObject.SetActive(true);    // Open slider

        dm.playerInfo.game_Armor--; // Decrease the current armor supply
        armorAmountText.text = dm.playerInfo.game_Armor.ToString(); // Update UI

        // give max value to all variables
        this.armor = dm.dv.player_Armor;
        dm.playerInfo.game_lastArmorHealth = dm.dv.player_Armor;
        armorSlider.value = dm.dv.player_Armor;
        armorSlider.maxValue = dm.dv.player_Armor;
    }

    [PunRPC]
    public void TakeDamage(int damage, string shooterName, string targetName)
    {
        if (deadAlready) return;    // avoid multi death

        //Debug.Log("Shooter: " + shooterName);
        if (matchManager == null) matchManager = FindObjectOfType<MatchManager>();
        if (matchManager.isGameOver) return; // Don't get hurt if the time is up

        // if we have armor and the shooter not a death wall, take the chance
        if (armor > 0 && !shooterName.Contains("Death") && Random.Range(0f, 1f) < armorChance) armor -= damage;
        else health -= damage;

        // Show hit effect to the client
        if (controller.isOwner)
        {
            hitImage.color = new Color(1f, 1f, 1f, 1f);
            if (health > 0)
            {
                if (damage > 30) controller.HitCamShake(2);
                else controller.HitCamShake(1);
            }
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

        if (armor <= 0) armorObject.SetActive(false);   // close the armor slider

        slider.value = health;
        armorSlider.value = armor;
        dm.playerInfo.game_lastArmorHealth = armor;
    }
}
