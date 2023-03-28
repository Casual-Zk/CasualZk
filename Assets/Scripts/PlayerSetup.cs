using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TarodevController;

public class PlayerSetup : MonoBehaviour
{
    public PlayerController controller;

    public void IsLocalPlayer()
    {
        controller.enabled = true;
    }
}
