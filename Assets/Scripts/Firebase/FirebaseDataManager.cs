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
using Random = UnityEngine.Random;
using System.Linq;

public class FirebaseDataManager : MonoBehaviour
{
    [SerializeField] Canvas connectingUI;
    [SerializeField] DisplayMessage messageUI;
    [SerializeField] GameObject testerLoginButton;
    [SerializeField] GameObject onlineCounterObject;

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
    Dictionary<string, object>[] topComs;
    Dictionary<string, object>[] snapContainers;

    int topUserRecordCounter = 0;
    int weekRecordDiff = 0;
    bool firstOfThisWeek = false;
    List<string> comCodesUpper = new List<string>();

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

            // Show/Hide online counter and testerlogin
            onlineCounterObject.SetActive(gameInfo.openOnlineCounter);
            testerLoginButton.SetActive(gameInfo.openTesterLogin);

            // Create a full uppercase version of community codes to compare
            foreach (string comCode in gameInfo.refList) { comCodesUpper.Add(comCode.ToUpper()); }

            GetTopUsers();
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
        menuManager.DisplayInventory();
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
        refCode = refCode.ToUpper();    // Make all chars upper case to compare

        ////// Check if the ref code is valid
        if (comCodesUpper.Contains(refCode))
        {
            // Get the index of matched come code
            // Get the original come code from gameInfo in that index and write it to DB
            WriteRefCode(gameInfo.refList[comCodesUpper.IndexOf(refCode)]);
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
        //####                                         ####\\
        //####  Get the most recent update from users  ####\\
        //####                                         ####\\
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

        //####                                                                 ####\\
        //####  If not updated, then get all the users of this week to update  ####\\
        //####                                                                 ####\\

        // Get current week's users
        string eggsOfTheWeek = "eggs." + gameInfo.currentWeek;
        //Debug.Log("Updating this weeks topUsers - Week: " + eggsOfTheWeek);

        // Get all players of this week who has played (therefore has eggs)
        Query usersQuery = firestore.Collection("users").
            WhereGreaterThan(eggsOfTheWeek, 0).OrderByDescending(eggsOfTheWeek);

        var usersQuerySnapshot = await usersQuery.GetSnapshotAsync();
        //Debug.Log("Total users of this week: " + usersQuerySnapshot.Count);

        //####                                                                 ####\\
        //####  Now we got all users of this week! Create variable to fill DB  ####\\
        //####                                                                 ####\\

        int sumOfEggs = 0;
        int tempEgg = 0;
        int playerCounter = 0;
        int snapDocIndex = 0;
        // List of top users
        Dictionary<string, object> topUsers = new Dictionary<string, object>();

        // List of all communities
        List<Community> coms = new List<Community>();
        // Fill all the communities in our records
        foreach (string comCode in gameInfo.refList) { 
            coms.Add(new Community() { ComCode = comCode, ComScore = 0 }); 
        }
        

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

        //####                                                        ####\\
        //####  Now go through all users count eggs, top users, etc.  ####\\
        //####                                                        ####\\

        // Iteraite through all of them and calculate total eggs and create a map of current values
        foreach (DocumentSnapshot userSnap in usersQuerySnapshot.Documents)
        {
            PlayerInfo weeklyPlayer = userSnap.ConvertTo<PlayerInfo>();
            playerCounter++;

            //###  Count Total Eggs  ###\\

            tempEgg = int.Parse(JsonConvert.SerializeObject
                (weeklyPlayer.eggs[gameInfo.currentWeek.ToString()]));

            sumOfEggs += tempEgg;   // add to total


            //###  Add eggs to community as score  ###\\
            if (gameInfo.refList.Contains(weeklyPlayer.refCode))
            {
                // Get the community from the list
                Community communityToUpdate = 
                    coms.FirstOrDefault(com => com.ComCode == weeklyPlayer.refCode);

                // Increase the score as much as the egg number of the member
                communityToUpdate.ComScore += tempEgg;
            }


            //###  Get snapshot of all players  ###\\

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



            //###  Get Top Users  ###\\

            // Don't create topUser entry after desired amount, just continue to count eggs
            // Since we got users starting with the most eggs, we can easily get the top from here
            if (playerCounter > dv.topUsers) { continue; }

            // Create top user map
            Dictionary<string, object> _user = new Dictionary<string, object>();
            _user["eggs"] = weeklyPlayer.eggs[gameInfo.currentWeek.ToString()];
            _user["matchCount"] = weeklyPlayer.matchCount;
            _user["nickname"] = weeklyPlayer.nickname;
            _user["walletAddress"] = weeklyPlayer.walletAddress;
            _user["refCode"] = weeklyPlayer.refCode;
            _user["userID"] = userSnap.Id;

            topUsers.Add(playerCounter.ToString(), _user);   // Add users in order 1, 2, 3...
        }
        //Debug.Log("Total Eggs of this week: " + sumOfEggs);

        // Add week info to the map
        Dictionary<string, object> softWeekInfo = new Dictionary<string, object>(){
            { "0creationTime", Timestamp.FromDateTime(DateTime.Now) },
            { "eggCount", sumOfEggs },
            { "playerCount", usersQuerySnapshot.Count }
        };

        topUsers.Add("0", softWeekInfo);    // Add week info to index 0
        //Debug.Log("Soft info added as well");

        // Display updated info
        menuManager.OnCurrentWeekTopUserUpdate(topUsers);

        // Turn the coms list to an db dictionary
        Dictionary<string, object> comScoreList = new Dictionary<string, object>(); // Create the dict.
        coms.Sort();    // Sort the list first
        int orderIndex = 1;
        for (int i = coms.Count - 1; i >= 0; i--)   // It was sorted reverse!
        {
            // Create com sub dict
            Dictionary<string, object> newCom = new Dictionary<string, object>();
            newCom["comCode"] = coms[i].ComCode;
            newCom["comScore"] = coms[i].ComScore;

            // Add all of them to the dictionary
            comScoreList.Add(orderIndex.ToString(), newCom);

            orderIndex++;
        }
        comScoreList.Add("0", softWeekInfo);    // Add the info about the list to top

        // Display updated info
        menuManager.OnCurrentWeekTopComUpdate(comScoreList);

        if (firstOfThisWeek)
        {
            allTopUsers[gameInfo.topUserRecordAmount - 1] = topUsers;
            topComs[gameInfo.topUserRecordAmount - 1] = comScoreList;

            menuManager.OnReturnAllTopUsers(allTopUsers);
            menuManager.OnReturnAllTopComs(topComs);
        }

        //####                                ####\\
        //####  Now write all of these to DB  ####\\
        //####                                ####\\

        //###  Write Communities ###\\

        string comListPath = "comListWeek_" + crrWeek
            + "/" + auth.CurrentUser.UserId + "_" + crrSnaps;

        await firestore.Document(comListPath).SetAsync(comScoreList);
        Debug.LogWarning("Updated communities on DB");


        //###  Write Top Users ###\\

        // Add the map with all updated values as document into DB under top week / User ID
        string topUsersDocPath = "topWeek_" + crrWeek 
            + "/" + auth.CurrentUser.UserId + "_" + crrSnaps;

        await firestore.Document(topUsersDocPath).SetAsync(topUsers);
        Debug.LogWarning("Updated top users on DB");


        //###  Write Snaps ###\\

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


        //###  Save Updater Data ###\\

        // Save the update time so that nobody can request update shortly
        playerInfo.weekInfoUpdateTime = Timestamp.FromDateTime(DateTime.Now);

        // update snapshots count and the week info update time field
        await firestore.Document("users/" + auth.CurrentUser.UserId).
            SetAsync(playerInfo, SetOptions.MergeFields("weekInfoUpdateTime", "snaps", "lastLogin"));

        snapContainers = null; // clear the containers
    }

    private async void GetTopUsers()
    {
        // Set all top users array because we now have current week value
        allTopUsers = new Dictionary<string, object>[gameInfo.topUserRecordAmount];
        topComs = new Dictionary<string, object>[gameInfo.topUserRecordAmount];

        // Get top users
        int topRecordCount = 0;
        do
        {
            // Path
            string path = "topWeek_" + (gameInfo.currentWeek - topRecordCount);

            // Get the most recent update
            Query updateQuery = firestore.Collection(path).OrderByDescending("0.0creationTime").Limit(1);

            var updateQuerySnapshot = await updateQuery.GetSnapshotAsync();

            // If there is no such collection, then we don't have this week's data. Update and write it
            if (updateQuerySnapshot.Count == 0)
            {
                Debug.Log("NO DATA !! Updating top users for this week");
                firstOfThisWeek = true;
                UpdateTopUsers();  // If there is no topUsers doc, then create one
            }
            else // Which means we have the data, write it down to the week's spot
            {
                DocumentSnapshot doc = updateQuerySnapshot[0];
                int index = gameInfo.topUserRecordAmount - (topRecordCount + 1);
                allTopUsers[index] = doc.ToDictionary();

                // Sent the all top users to menu manager
                menuManager.OnReturnAllTopUsers(allTopUsers);

                // If this one is the current week, send it to the current week update to display
                if (topRecordCount == 0) menuManager.OnCurrentWeekTopUserUpdate(doc.ToDictionary());
            }

            topRecordCount++;
        }
        while (topRecordCount < gameInfo.topUserRecordAmount);

        // Get top Communities as well, same process
        topRecordCount = 0;
        do
        {
            // Path
            string path = "comListWeek_" + (gameInfo.currentWeek - topRecordCount);

            // Get the most recent update
            Query updateQuery = firestore.Collection(path).OrderByDescending("0.0creationTime").Limit(1);

            var updateQuerySnapshot = await updateQuery.GetSnapshotAsync();

            // If there is no such collection, then we don't have this week's data. Update and write it
            if (updateQuerySnapshot.Count > 0)
            {
                DocumentSnapshot doc = updateQuerySnapshot[0];
                int index = gameInfo.topUserRecordAmount - (topRecordCount + 1);
                topComs[index] = doc.ToDictionary();

                // Sent the all top users to menu manager
                menuManager.OnReturnAllTopComs(topComs);

                // If this one is the current week, send it to the current week update to display
                if (topRecordCount == 0) menuManager.OnCurrentWeekTopComUpdate(doc.ToDictionary());
            }

            topRecordCount++;
        }
        while (topRecordCount < gameInfo.topUserRecordAmount);
    }
}
