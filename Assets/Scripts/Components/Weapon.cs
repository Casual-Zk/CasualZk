using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TarodevController;

public class Weapon : MonoBehaviourPunCallbacks
{
    public SimpleContoller controller { get; set; }

    [SerializeField] float fireRate;
    [SerializeField] Transform nozzle;
    [SerializeField] Transform[] shotgunDirections;
    [SerializeField] Bullet bullet;
    [SerializeField] SimpleContoller controllerToSet;
    [SerializeField] bool isKnife;
    [SerializeField] bool isGlock;
    [SerializeField] bool isShotgun;
    [SerializeField] bool isM4;
    [SerializeField] bool isAWP;

    MatchManager matchManager;
    FirebaseDataManager dm;
    
    float nextFire;
    bool fireRateUpdated;

    private void Start()
    {
        controller = controllerToSet;
        matchManager = FindObjectOfType<MatchManager>();
        dm = FindObjectOfType<FirebaseDataManager>();
    }

    void Update()
    {
        if (matchManager.isGameOver) return; // Don't fire if the time is up
        if (!controller.isOwner) return;   // Do not execute any code if it's not owner!

        // If we have the data and it is not updated yet, update!
        if (dm.dv != null && !fireRateUpdated) 
        {
            if (isKnife) fireRate = dm.dv.weaponFireRate_Knife;
            if (isGlock) fireRate = dm.dv.weaponFireRate_Glock;
            if (isShotgun) fireRate = dm.dv.weaponFireRate_Shotgun;
            if (isM4) fireRate = dm.dv.weaponFireRate_M4;
            if (isAWP) fireRate = dm.dv.weaponFireRate_AWP;

            fireRateUpdated = true;
        }

        // Decrease the timer
        if (nextFire > 0) nextFire -= Time.deltaTime;

        // fire if the time is up and button pressed
        if (controller.CanFire() && nextFire <= 0)
        {
            // Set the timer
            nextFire = 1 / fireRate;
            Fire();
        }
    }

    void Fire()
    {
        if (isKnife)
        {
            controller.photonView.RPC("TriggerKnife", RpcTarget.All, dm.dv.weaponDamage_Knife, PhotonNetwork.NickName);

            // Play SFX
            controller.GetComponent<PhotonView>().RPC("PlayFireSFX", RpcTarget.All, "Knife_Attack_SFX");
        }
        else if (isShotgun)
        {
            // Spawn all bullets
            foreach (Transform nozzle in shotgunDirections)
            {
                GameObject newBullet = PhotonNetwork.Instantiate(bullet.name, nozzle.position, nozzle.rotation);
                newBullet.GetComponent<Bullet>().isOwner = true;
                newBullet.GetComponent<PhotonView>().RPC("SetOwner", RpcTarget.All, PhotonNetwork.NickName);
                newBullet.GetComponent<Bullet>().damage = dm.dv.weaponDamage_Shotgun;
            }

            // Play SFX
            controller.GetComponent<PhotonView>().RPC("PlayFireSFX", RpcTarget.All, "Shotgun_SFX");
        }
        else
        {
            // Spawn the bullet
            GameObject newBullet = PhotonNetwork.Instantiate(bullet.name, nozzle.position, nozzle.rotation);
            newBullet.GetComponent<Bullet>().isOwner = true;
            newBullet.GetComponent<PhotonView>().RPC("SetOwner", RpcTarget.All, PhotonNetwork.NickName);

            if (isGlock)
                newBullet.GetComponent<Bullet>().damage = dm.dv.weaponDamage_Glock;
            else if (isM4)
                newBullet.GetComponent<Bullet>().damage = dm.dv.weaponDamage_M4;
            else if (isAWP)
                newBullet.GetComponent<Bullet>().damage = dm.dv.weaponDamage_AWP;

            if (isAWP) controller.GetComponent<PhotonView>().RPC("PlayFireSFX", RpcTarget.All, "Sniper_SFX");
            else controller.GetComponent<PhotonView>().RPC("PlayFireSFX", RpcTarget.All, "Short_Fire");
        }


        // Update bullet count on controller    
        controller.Fired();
    }
}
