using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Threading.Tasks;

public class FirebaseDataManager : MonoBehaviour
{
    [SerializeField] Canvas connectingUI;

    public PlayerInfo playerInfo { get; set; }
    public BasicGameInfo gameInfo { get; set; }

    private ListenerRegistration playerReg;
    private ListenerRegistration gameReg;

    private FirebaseFirestore firestore;
    private FirebaseAuth auth;

    public bool[] hasWeapon = new bool[5];
    public int[] ammoBalance = new int[5];

    private void Awake()
    {
        firestore = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        playerInfo = null;
        gameInfo = null;
    }
    
    public void OnLogin()
    {
        playerReg = firestore.Document("users/" + auth.CurrentUser.UserId).Listen(snaphot =>
        {
            playerInfo = snaphot.ConvertTo<PlayerInfo>();
            Debug.Log("Player Nickname: " + playerInfo.nickname);

            // Use your player info here as you wish
            // Update nicknames
            FindObjectOfType<MenuManager>().DisplayInfo();

            // UPDATE weapon balance from blockchain
            if (playerInfo.walletAddress != null)
                StartCoroutine(FindObjectOfType<ChainManager>().GetWeaponBalances(playerInfo.walletAddress));

            // Save ammo balance locally
            ammoBalance[1] = playerInfo.ammo_9mm;
            ammoBalance[2] = playerInfo.ammo_12_gauge;
            ammoBalance[3] = playerInfo.ammo_5_65mm;
            ammoBalance[4] = playerInfo.ammo_7_62mm;
        });

        gameReg = firestore.Document("gameInfo/basicInfo").Listen(snaphot =>
        {   
            gameInfo = snaphot.ConvertTo<BasicGameInfo>();
            Debug.Log("Current Week : " + gameInfo.currentWeek);
        });
    }

    private void OnDestroy()
    {
        UpdateAmmoBalance();
        if (playerReg != null) playerReg.Stop();
        if (gameReg != null) gameReg.Stop();
    }
    
    public void OnWeaponBalanceReturn(List<BigInteger> balances)
    {
        for (int i = 0; i < balances.Count; i++)
        {
            if (balances[i] > 0) hasWeapon[i] = true; 
        }

        connectingUI.enabled = false;
    }

    public void GiveEgg() { Debug.Log("Button pressed"); _ = IncrementEggForWeek(); }
    
    private async Task IncrementEggForWeek()
    {
        int currentEggNmber = 0;
        if (playerInfo.eggs[gameInfo.currentWeek] != null)
        {
            int.TryParse(playerInfo.eggs[gameInfo.currentWeek].ToString(), out currentEggNmber);
        }

        playerInfo.eggs[gameInfo.currentWeek] = currentEggNmber + 1;

        await firestore.Document("users/" + auth.CurrentUser.UserId).SetAsync(playerInfo, SetOptions.MergeFields("eggs"));

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
