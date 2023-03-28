using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TarodevController;

public class Weapon : MonoBehaviour
{
    public float fireRate;
    public Transform nozzle;
    public Bullet bullet;
    public PlayerController controller;

    private float nextFire;

    void Update()
    {
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
        // Spawn the bullet
        GameObject newBullet = PhotonNetwork.Instantiate(bullet.name, nozzle.position, transform.rotation);
        newBullet.GetComponent<Bullet>().isOwner = true;
    }
}
