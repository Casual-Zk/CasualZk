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
    public int onlineCounter { get; set; }

    private ListenerRegistration playerReg;
    private ListenerRegistration gameReg;

    private FirebaseFirestore firestore;
    private FirebaseAuth auth;

    public int[] weaponBalance = new int[5];
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
            ammoBalance[1] = playerInfo.game_12_gauge;
            ammoBalance[2] = playerInfo.game_9mm;
            ammoBalance[3] = playerInfo.game_5_56mm;
            ammoBalance[4] = playerInfo.game_7_62mm;
        });

        gameReg = firestore.Document("gameInfo/basicInfo").Listen(snaphot =>
        {   
            gameInfo = snaphot.ConvertTo<BasicGameInfo>();
            //Debug.Log("Current Week : " + gameInfo.currentWeek);
            //Debug.Log("Needed player amount : " + gameInfo.playerAmount);

            FindObjectOfType<RoomManager>().UpdateCounter();// Update counter at start
        });
    }

    private void OnDestroy()
    {
        Debug.Log("OnDestroy called! Updating ammo balance!");
        UpdateAmmoBalance();
        if (playerReg != null) playerReg.Stop();
        if (gameReg != null) gameReg.Stop();
    }

    public void OnWeaponBalanceReturn(List<BigInteger> balances)
    {
        for (int i = 0; i < 5; i++)
        {
            weaponBalance[i] = (int)balances[i]; 
        }

        // On chain info saved onto local player info
        playerInfo.knifeAmount = (int)balances[0];
        playerInfo.glockAmount = (int)balances[1];
        playerInfo.shotgunAmount = (int)balances[2];
        playerInfo.m4Amount = (int)balances[3];
        playerInfo.awpAmount = (int)balances[4];
        playerInfo.wallet_12_gauge = (int)balances[5];
        playerInfo.wallet_9mm = (int)balances[6];
        playerInfo.wallet_5_56mm = (int)balances[7];
        playerInfo.wallet_7_62mm = (int)balances[8];

        connectingUI.enabled = false;
    }

    public void GiveEgg() { Debug.Log("Button pressed"); _ = IncrementEggForWeek(); }
    
    private async Task IncrementEggForWeek()
    {
        int currentEggNmber = 0;
        string crrWeek = gameInfo.currentWeek.ToString();
        if (playerInfo.eggs[crrWeek] != null)
        {
            int.TryParse(playerInfo.eggs[crrWeek].ToString(), out currentEggNmber);
        }

        playerInfo.eggs[crrWeek] = currentEggNmber + 1;

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
        playerInfo.game_12_gauge = ammoBalance[1];
        playerInfo.game_9mm = ammoBalance[2];
        playerInfo.game_5_56mm = ammoBalance[3];
        playerInfo.game_7_62mm = ammoBalance[4];

        firestore.Document("users/" + auth.CurrentUser.UserId).
            SetAsync(playerInfo, SetOptions.MergeFields("game_5_56mm", "game_7_62mm", "game_9mm", "game_12_gauge"));
    }
}
