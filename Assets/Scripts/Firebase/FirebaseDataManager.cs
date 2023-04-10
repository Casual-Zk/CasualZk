using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Threading.Tasks;

public class FirebaseDataManager : MonoBehaviour
{
    public PlayerInfo playerInfo { get; set; }
    private ListenerRegistration registration;

    private FirebaseFirestore firestore;
    private FirebaseAuth auth;

    public bool[] hasWeapon = new bool[5];
    public int[] ammoBalance = new int[5];

    private void Awake()
    {
        firestore = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
    }
    
    public void OnLogin()
    {
        registration = firestore.Document("users/" + auth.CurrentUser.UserId).Listen(snaphot =>
        {
            playerInfo = snaphot.ConvertTo<PlayerInfo>();
            Debug.Log("Player Nickname: " + playerInfo.nickname);

            // Use your player info here as you wish
            // Update nicknames
            FindObjectOfType<MenuManager>().DisplayInfo();

            // UPDATE weapon balance from blockchain
            hasWeapon[0] = true;
            hasWeapon[1] = true;
            hasWeapon[4] = true;

            // Save ammo balance locally
            ammoBalance[1] = playerInfo.ammo_9mm;
            ammoBalance[2] = playerInfo.ammo_12_gauge;
            ammoBalance[3] = playerInfo.ammo_5_65mm;
            ammoBalance[4] = playerInfo.ammo_7_62mm;
        });
    }

    private void OnDestroy()
    {
        UpdateAmmoBalance();
        if (registration != null) registration.Stop();
    }
    
    public async Task IncrementEggForWeek(int week)
    {
        DocumentReference docRef = firestore.Document("users/" + auth.CurrentUser.UserId);
        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

        if (snapshot.Exists)
        {
            Dictionary<string, object> eggs = snapshot.GetValue<Dictionary<string, object>>("eggs");

            if (eggs.ContainsKey(week.ToString()))
            {
                int eggCount = Convert.ToInt32(eggs[week.ToString()]);
                eggCount++;
                eggs[week.ToString()] = eggCount;
                await docRef.UpdateAsync("eggs", eggs);
            }
        }
    }
    
    public void SetNickname(string nickname)
    {
        PlayerInfo playerInfo = new PlayerInfo { nickname = nickname };

        firestore.Document("users/" + auth.CurrentUser.UserId).
            SetAsync(playerInfo, SetOptions.MergeFields("nickname"));
    }

    public void UpdateAmmoBalance()
    {
        playerInfo.ammo_9mm = ammoBalance[1];
        playerInfo.ammo_12_gauge = ammoBalance[2];
        playerInfo.ammo_5_65mm = ammoBalance[3];
        playerInfo.ammo_7_62mm = ammoBalance[4];

        firestore.Document("users/" + auth.CurrentUser.UserId).
            SetAsync(playerInfo, SetOptions.MergeFields("ammo_5_65mm", "ammo_7_62mm", "ammo_9mm", "ammo_12_gauge"));
    }
}
