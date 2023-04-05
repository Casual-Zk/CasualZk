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
    [SerializeField] bool isShotgun;
    [SerializeField] bool isSniper;

    MatchManager matchManager;
    private float nextFire;

    private void Start()
    {
        controller = controllerToSet;
        matchManager = FindObjectOfType<MatchManager>();
    }

    void Update()
    {
        if (matchManager.isGameOver) return; // Don't fire if the time is up
        if (!controller.isOwner) return;   // Do not execute any code if it's not owner!

        // Decrease the timer
        if (nextFire > 0) nextFire -= Time.deltaTime;

        // fire if the time is up and button pressed
        if (Input.GetButton("Fire1") && nextFire <= 0)
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
            // TODO
        }
        else if (isShotgun)
        {
            // Spawn all bullets
            foreach (Transform nozzle in shotgunDirections)
            {
                GameObject newBullet = PhotonNetwork.Instantiate(bullet.name, nozzle.position, nozzle.rotation);
                newBullet.GetComponent<Bullet>().isOwner = true;
                newBullet.GetComponent<PhotonView>().RPC("SetOwner", RpcTarget.All, PhotonNetwork.NickName);
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

            if (isSniper) controller.GetComponent<PhotonView>().RPC("PlayFireSFX", RpcTarget.All, "Sniper_SFX");
            else controller.GetComponent<PhotonView>().RPC("PlayFireSFX", RpcTarget.All, "Short_Fire");
        }
    }
}
