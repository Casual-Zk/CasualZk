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
using Newtonsoft.Json;

public class FirebaseDataManager : MonoBehaviour
{
    [SerializeField] Canvas connectingUI;
    [SerializeField] DisplayMessage messageUI;

    public PlayerInfo playerInfo { get; set; }
    public BasicGameInfo gameInfo { get; set; }
    public DynamicVariables dv { get; set; }
    public int onlineCounter { get; set; }

    private ListenerRegistration playerReg;
    private ListenerRegistration gameReg;
    private ListenerRegistration dvReg;

    private FirebaseFirestore firestore;
    private FirebaseAuth auth;
    private MenuManager menuManager;

    public int[] weaponBalance = new int[5];
    public int[] ammoBalance = new int[5];

    Dictionary<string, object>[] allTopUsers;

    int topUserRecordCounter = 0;
    int weekRecordDiff = 0;

    private void Awake()
    {
        firestore = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        playerInfo = null;
        gameInfo = null;
        dv = null;
    }
    
    public void OnLogin()
    {
        menuManager = FindObjectOfType<MenuManager>();

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

            // Set all top users array because we now have current week value
            allTopUsers = new Dictionary<string, object>[gameInfo.topUserRecordAmount];

            // Set the difference to adjust indexing right
            weekRecordDiff = (gameInfo.currentWeek - gameInfo.topUserRecordAmount) + 1;

            // Get top users
            string topUsersDocPath = "gameInfo/topUsers_" + gameInfo.currentWeek;

            // Get top user documents
            firestore.Document(topUsersDocPath).GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                DocumentSnapshot document = task.Result;

                if (document.Exists)
                {
                    //Dictionary<string, object> user = (Dictionary<string, object>)topUsers["1"];
                    //Debug.Log(user);
                    //Debug.Log(user["userID"]);
                    allTopUsers[gameInfo.topUserRecordAmount - 1] = document.ToDictionary();
                    FindObjectOfType<MenuManager>().
                    OnCurrentWeekTopUserUpdate(allTopUsers[gameInfo.topUserRecordAmount - 1]);
                    //Debug.Log("Got current week");
                    topUserRecordCounter++;
                }
                else
                    UpdateTopUsers();  // If there is no topUsers doc, then create one
                
                // Then get previous weeks' top users as well
                for (int i = gameInfo.currentWeek - 1; i > gameInfo.currentWeek - gameInfo.topUserRecordAmount; i--)
                {
                    //Debug.Log("Path: " + "gameInfo/topUsers_" + i.ToString());

                    firestore.Document("gameInfo/topUsers_" + i.ToString()).GetSnapshotAsync().ContinueWithOnMainThread(task =>
                    {
                        DocumentSnapshot document = task.Result;

                        // We get the index from the document, not from the for loop
                        // because while we are waiting to data to come, for loop completes and i goes far below
                        string docID = document.Id.ToString();
                        char last = docID[docID.Length - 1];
                        int index = int.Parse(last.ToString());

                        if (document.Exists)
                        {
                            //Debug.Log("***** " + index + " *****");
                            Dictionary<string, object> prevTopUsers = document.ToDictionary();

                            // index (week number) - diff gives the accurate index in the array
                            allTopUsers[index - weekRecordDiff] = prevTopUsers; 
                            topUserRecordCounter++;

                            if (topUserRecordCounter == gameInfo.topUserRecordAmount)
                            {
                                menuManager.OnReturnAllTopUsers(allTopUsers);
                            }
                        }
                        else
                            Debug.LogError("Top user info for week " + i + " doesn not exist!!");
                    });
                }                
            });
        });

        dvReg = firestore.Document("gameInfo/dynamicVariables").Listen(snaphot =>
        {
            dv = snaphot.ConvertTo<DynamicVariables>();
        });
    }

    private void OnDestroy()
    {
        Debug.Log("OnDestroy called! Updating ammo balance!");
        UpdateAmmoBalance();
        if (playerReg != null) playerReg.Stop();
        if (gameReg != null) gameReg.Stop();
        if (dvReg != null) dvReg.Stop();
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

    // Queries
    public async void UpdateTopUsers()
    {
        // Get the most recent update from users
        Query updateQuery = firestore.Collection("users").OrderByDescending("weekInfoUpdateTime").Limit(1);

        //Debug.Log("Updating this weeks topUsers - Week: " + eggsOfTheWeek);

        var updateQuerySnapshot = await updateQuery.GetSnapshotAsync();
        PlayerInfo updatePlayer = new PlayerInfo();

        // If there is any time record for this weeks update, then check the time
        if (updateQuerySnapshot.Count > 0)
        {
            DocumentSnapshot updateQuerySnap = updateQuerySnapshot[0];
            updatePlayer = updateQuerySnap.ConvertTo<PlayerInfo>();
            //Debug.Log("Most recent time: " + updatePlayer.weekInfoUpdateTime.ToDateTime());

            TimeSpan timeDifference = DateTime.UtcNow - updatePlayer.weekInfoUpdateTime.ToDateTime();

            // If recently updated then skip whole update stuff
            if (timeDifference.TotalMinutes <= gameInfo.recordUpdateTime)
            {
                // Less than 5 minutes have passed between the timestamps
                Debug.LogWarning("The data updated less than " + gameInfo.recordUpdateTime + "minutes ago!");
                messageUI.Display("The data has been updated less than " + gameInfo.recordUpdateTime + " minutes ago!", 3f);
                return;
            }
        }

        ///// If not updated, then get all the users of this week to update    /////

        // Get current week's users
        string eggsOfTheWeek = "eggs." + gameInfo.currentWeek;
        //Debug.Log("Updating this weeks topUsers - Week: " + eggsOfTheWeek);

        // Get all players of this week who has played (therefore has eggs)
        Query usersQuery = firestore.Collection("users").
            WhereGreaterThan(eggsOfTheWeek, 0).OrderByDescending(eggsOfTheWeek);

        var usersQuerySnapshot = await usersQuery.GetSnapshotAsync();
        //Debug.Log("Total users of this week: " + usersQuerySnapshot.Count);

        int sumOfEggs = 0;
        int index = 1;
        Dictionary<string, object> topUsers = new Dictionary<string, object>();

        // Iteraite through all of them and calculate total eggs and create a map of current values
        foreach (DocumentSnapshot userSnap in usersQuerySnapshot.Documents)
        {
            PlayerInfo weeklyPlayer = userSnap.ConvertTo<PlayerInfo>();

            sumOfEggs += int.Parse(JsonConvert.SerializeObject
                (weeklyPlayer.eggs[gameInfo.currentWeek.ToString()]));

            // Don't create topUser entry after desired amount, just continue to count eggs
            if (index > gameInfo.topUserRecordAmount) { continue; }

            // Create top user map
            Dictionary<string, object> _user = new Dictionary<string, object>();
            _user["eggs"] = weeklyPlayer.eggs[gameInfo.currentWeek.ToString()];
            //user["matches"] = player.matches;
            _user["nickname"] = weeklyPlayer.nickname;
            _user["walletAddress"] = weeklyPlayer.walletAddress;
            _user["userID"] = userSnap.Id;

            topUsers.Add(index.ToString(), _user);   // Add users in order 1, 2, 3...
            index++;
        }
        //Debug.Log("Total Eggs of this week: " + sumOfEggs);

        // Add week info to the map
        Dictionary<string, object> softWeekInfo = new Dictionary<string, object>(){
            { "eggCount", sumOfEggs },
            { "playerCount", usersQuerySnapshot.Count }
        };

        topUsers.Add("0", softWeekInfo);    // Add week info to index 0
        //Debug.Log("Soft info added as well");

        // Display updated info
        menuManager.OnCurrentWeekTopUserUpdate(topUsers);

        // Add the map with all updated values as document into DB as topUsers of the week
        string topUsersDocPath = "gameInfo/topUsers_" + gameInfo.currentWeek.ToString();
        await firestore.Document(topUsersDocPath).SetAsync(topUsers);
        Debug.LogWarning("Updated top users on DB");

        // Save the update time so that nobody can request update shortly
        updatePlayer.weekInfoUpdateTime = Timestamp.FromDateTime(DateTime.Now);

        // update only the week info update time field
        await firestore.Document("users/" + auth.CurrentUser.UserId).
            SetAsync(updatePlayer, SetOptions.MergeFields("weekInfoUpdateTime"));
    }
}
