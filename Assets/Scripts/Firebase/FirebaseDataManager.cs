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

    public int[] weaponBalance = new int[5];    // On-Chain
    public int[] magSize = new int[5];          // On-Chain
    public int[] ammoBalance = new int[5];      // Off-chain, on DB
    public bool[] isWeaponActive = new bool[5]; // Off-chain, on DB

    Dictionary<string, object>[] allTopUsers;
    Dictionary<string, object>[] snapContainers;

    int topUserRecordCounter = 0;
    int weekRecordDiff = 0;
    bool firstOfThisWeek = false;

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
            ammoBalance[1] = playerInfo.game_9mm; 
            ammoBalance[2] = playerInfo.game_12_gauge;
            ammoBalance[3] = playerInfo.game_5_56mm;
            ammoBalance[4] = playerInfo.game_7_62mm;

            // Save last login
            playerInfo.lastLogin = Timestamp.FromDateTime(DateTime.Now);
        });

        gameReg = firestore.Document("gameInfo/basicInfo").Listen(snaphot =>
        {   
            gameInfo = snaphot.ConvertTo<BasicGameInfo>();
            //Debug.Log("Current Week : " + gameInfo.currentWeek);
            //Debug.Log("Needed player amount : " + gameInfo.playerAmount);

            FindObjectOfType<RoomManager>().UpdateCounter();// Update counter at start
            menuManager.OnGameInfoReceived();

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
                    menuManager.OnCurrentWeekTopUserUpdate(allTopUsers[gameInfo.topUserRecordAmount - 1]);
                    //Debug.Log("Got current week");
                    topUserRecordCounter++;
                }
                else
                {
                    firstOfThisWeek = true;
                    UpdateTopUsers();  // If there is no topUsers doc, then create one
                }
                
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
                            Debug.Log("***** " + index + " *****");
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

            // Save active weapons
            isWeaponActive[0] = dv.weaponActive_Knife;
            isWeaponActive[1] = dv.weaponActive_Glock;
            isWeaponActive[2] = dv.weaponActive_Shotgun;
            isWeaponActive[3] = dv.weaponActive_M4;
            isWeaponActive[4] = dv.weaponActive_AWP;
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
        playerInfo.knifeAmount = 1; // Mandatory in-game asset
        playerInfo.glockAmount = (int)balances[1];
        playerInfo.shotgunAmount = (int)balances[2];
        playerInfo.m4Amount = (int)balances[3];
        playerInfo.awpAmount = (int)balances[4];
        playerInfo.wallet_12_gauge = (int)balances[5];
        playerInfo.wallet_9mm = (int)balances[6];
        playerInfo.wallet_5_56mm = (int)balances[7];
        playerInfo.wallet_7_62mm = (int)balances[8];

        // TEST - Give mags
        magSize[1] = 10;    // Glock default 10
        magSize[2] = 1;    // Shoutgun
        magSize[3] = 20;    // M4 default 20
        magSize[4] = 1;    // AWP

        connectingUI.enabled = false;
    }

    public void GiveEgg() { _ = IncrementEggForWeek(); }
    
    private async Task IncrementEggForWeek()
    {
        int currentEggNmber = 0;
        string crrWeek = gameInfo.currentWeek.ToString();
        if (playerInfo.eggs[crrWeek] != null)
        {
            int.TryParse(playerInfo.eggs[crrWeek].ToString(), out currentEggNmber);
        }

        playerInfo.eggs[crrWeek] = currentEggNmber + 1;
        playerInfo.winCount++;

        await firestore.Document("users/" + auth.CurrentUser.UserId).SetAsync(playerInfo, 
            SetOptions.MergeFields("eggs", "winCount", "lastLogin"));
    }

    public void SetNickname(string nickname)
    {
        playerInfo.nickname = nickname;

        firestore.Document("users/" + auth.CurrentUser.UserId).
            SetAsync(playerInfo, SetOptions.MergeFields("nickname", "lastLogin"));
    }

    public void SetRefCode(string refCode)
    {
        refCode = refCode.ToUpper();    // Make all chars upper case

        ////// Check if the ref code is valid
        if (gameInfo.refList.Contains(refCode))
        {
            WriteRefCode(refCode);
        }

        // If it is not in the general list, check the one-time list
        else if (gameInfo.refListOne.Contains(refCode))
        {
            if (gameInfo.refListUsed.Contains(refCode))
                messageUI.Display("This reference code has already been activated!", 3f);
            else
            {
                // Then the player has one-time ref code and it is not used!
                WriteRefCode(refCode);

                // Then write it among the used ones
                gameInfo.refListUsed.Add(refCode);

                firestore.Document("gameInfo/basicInfo").
                    SetAsync(gameInfo, SetOptions.MergeFields("refListUsed"));
            }
        }

        // If it is not in any list, show error message
        else
            messageUI.Display("Invalid reference code! Check your code please...", 3f);
    }

    private void WriteRefCode(string refCode)
    {
        playerInfo.refCode = refCode;

        firestore.Document("users/" + auth.CurrentUser.UserId).
            SetAsync(playerInfo, SetOptions.MergeFields("refCode", "lastLogin"));
    }

    public void UpdateAmmoBalance()
    {
        playerInfo.game_9mm = ammoBalance[1];
        playerInfo.game_12_gauge = ammoBalance[2];
        playerInfo.game_5_56mm = ammoBalance[3];
        playerInfo.game_7_62mm = ammoBalance[4];

        firestore.Document("users/" + auth.CurrentUser.UserId).
            SetAsync(playerInfo, SetOptions.MergeFields("game_5_56mm", "game_7_62mm", "game_9mm", "game_12_gauge",
            "matchCount", "lastLogin"));
    }

    // Queries
    public async void UpdateTopUsers()
    {
        // Get the most recent update from users
        Query updateQuery = firestore.Collection("users").OrderByDescending("weekInfoUpdateTime").Limit(1);

        //Debug.Log("Updating this weeks topUsers - Week: " + eggsOfTheWeek);

        var updateQuerySnapshot = await updateQuery.GetSnapshotAsync();
        PlayerInfo updatePlayer = new PlayerInfo(); // The player who made the recent update

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
        int playerCounter = 0;
        int snapDocIndex = 0;
        Dictionary<string, object> topUsers = new Dictionary<string, object>();

        // Get the info of this user about how many snap this dude took
        int crrSnaps = 0;
        string crrWeek = gameInfo.currentWeek.ToString();

        // If the user don't have the mapping in the first place, give it to him
        if (playerInfo.snaps == null)
        {
            playerInfo.snaps = new Dictionary<string, object>(); 
        }

        // If the current week has snap count in it, get it.
        if (playerInfo.snaps.ContainsKey(crrWeek))
        {
            int.TryParse(playerInfo.snaps[crrWeek].ToString(), out crrSnaps);
        }
        Debug.Log("Updater has taken this amount of snapshot for this week: " + crrSnaps);
        
        snapContainers = new Dictionary<string, object>[10000]; // Clear and create the containers

        // Iteraite through all of them and calculate total eggs and create a map of current values
        foreach (DocumentSnapshot userSnap in usersQuerySnapshot.Documents)
        {
            PlayerInfo weeklyPlayer = userSnap.ConvertTo<PlayerInfo>();
            playerCounter++;

            sumOfEggs += int.Parse(JsonConvert.SerializeObject
                (weeklyPlayer.eggs[gameInfo.currentWeek.ToString()]));

            // ** Divide players info 2000 documents. Each doc contains 2000 players
            // ** Each doc should contain the time of creation
            snapDocIndex = Mathf.CeilToInt((float)playerCounter / gameInfo.snapDocLimit);
            //Debug.Log("Player Counter: " + playerCounter);
            //Debug.LogWarning("DocIndex:" + snapDocIndex);

            // If the container empty, then put a doc in it and add the creation time along with it
            if (snapContainers[snapDocIndex] == null)
            {
                snapContainers[snapDocIndex] = new Dictionary<string, object>();
                snapContainers[snapDocIndex].Add("0creationTime", Timestamp.FromDateTime(DateTime.Now));
            }

            // Add the user to the doc
            snapContainers[snapDocIndex].Add(userSnap.Id, weeklyPlayer);

            // Don't create topUser entry after desired amount, just continue to count eggs
            if (playerCounter > dv.topUsers) { continue; }

            // Create top user map
            Dictionary<string, object> _user = new Dictionary<string, object>();
            _user["eggs"] = weeklyPlayer.eggs[gameInfo.currentWeek.ToString()];
            _user["matchCount"] = weeklyPlayer.matchCount;
            _user["nickname"] = weeklyPlayer.nickname;
            _user["walletAddress"] = weeklyPlayer.walletAddress;
            _user["userID"] = userSnap.Id;

            topUsers.Add(playerCounter.ToString(), _user);   // Add users in order 1, 2, 3...
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

        if (firstOfThisWeek)
        {
            allTopUsers[gameInfo.topUserRecordAmount - 1] = topUsers;
            FindObjectOfType<MenuManager>().OnCurrentWeekTopUserUpdate(topUsers);
            topUserRecordCounter++;
            
            Debug.Log("First of this week (update) executed");

            if (topUserRecordCounter == gameInfo.topUserRecordAmount)
            {
                menuManager.OnReturnAllTopUsers(allTopUsers);
            }
        }

        // Add the map with all updated values as document into DB as topUsers of the week
        string topUsersDocPath = "gameInfo/topUsers_" + gameInfo.currentWeek.ToString();
        await firestore.Document(topUsersDocPath).SetAsync(topUsers);
        Debug.LogWarning("Updated top users on DB");

        int createdSnapCounter = 0;

        // Add the snaps to DB
        foreach (Dictionary<string, object> doc in snapContainers)
        {
            if (doc == null) continue; // skip the empty ones

            createdSnapCounter++;

            string docPath = "snapWeek_" + gameInfo.currentWeek.ToString() // Collection path
            + "/" + auth.CurrentUser.UserId + "_" + (crrSnaps + createdSnapCounter);  // Add doc ID: userID_SnapCount
            
            await firestore.Document(docPath).SetAsync(doc);
            Debug.LogWarning("Snap doc uploaded to the DB");
        }

        playerInfo.snaps[crrWeek] = crrSnaps + createdSnapCounter;   // Save the snap counts

        // Save the update time so that nobody can request update shortly
        playerInfo.weekInfoUpdateTime = Timestamp.FromDateTime(DateTime.Now);

        // update snapshots count and the week info update time field
        await firestore.Document("users/" + auth.CurrentUser.UserId).
            SetAsync(playerInfo, SetOptions.MergeFields("weekInfoUpdateTime", "snaps", "lastLogin"));

        snapContainers = null; // clear the containers
    }
}
