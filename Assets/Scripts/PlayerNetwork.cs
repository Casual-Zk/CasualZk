using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

public class PlayerNetwork : NetworkBehaviour 
{
    private NetworkVariable<int> randomNumber = new NetworkVariable<int>(13, 
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public override void OnNetworkSpawn()
    {
        randomNumber.OnValueChanged += (int previousValue, int newValue) =>
        {
            Debug.Log(OwnerClientId + " - Random number: " + randomNumber.Value);
        };
    }

    void Update()
    {
        // Only control the client
        if (!IsOwner) return;

        Vector3 moveDir = new Vector3(0, 0);

        if (Input.GetKey(KeyCode.T)) randomNumber.Value = UnityEngine.Random.Range(0, 100);

        if (Input.GetKey(KeyCode.D)) moveDir.x = +1f;
        if (Input.GetKey(KeyCode.A)) moveDir.x = -1f;
        if (Input.GetKey(KeyCode.W)) moveDir.y = +1f;
        if (Input.GetKey(KeyCode.S)) moveDir.y = -1f;

        float moveSpeed = 3f;
        transform.position += moveDir * moveSpeed * Time.deltaTime;
    }
}
