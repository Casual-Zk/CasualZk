using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Threading.Tasks;
using System;

public class FirebaseDataManager : MonoBehaviour
{
    private FirebaseFirestore firestore;
    private FirebaseAuth auth;

    private ListenerRegistration registration;

    private string dataPath = "";

    void Start()
    {
        firestore = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;

        registration = firestore.Document(dataPath).Listen(snaphot =>
        {
            var playerInfo = snaphot.ConvertTo<PlayerInfo>();

            // Use your player info here as you wish
        });
    }

    private void OnDestroy()
    {
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
        /*
        Dictionary<string, object> eggs = new Dictionary<string, object>();

        DocumentReference userRef = firestore.Document("users/" + auth.CurrentUser.UserId);
        userRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Failed to get eggs: " + task.Exception);
                return;
            }

            Dictionary<int, int> eggs = task.Result.GetValue<Dictionary<int, int>>("eggs");
            Debug.Log("Eggs: " + eggs.Count);
        });
        
        int currentEggNumber = eggs.Keys("3");

        PlayerInfo playerInfo = new PlayerInfo { eggs = eggs };
        firestore.Document("users/" + auth.CurrentUser.UserId).
            SetAsync(playerInfo, SetOptions.MergeFields("eggs"));
        */
    }
    
    public async Task UpdateNickname(string nickname)
    {
        // await IncrementEggForWeek(3);
        PlayerInfo playerInfo = new PlayerInfo { nickname = PlayerPrefs.GetString("Nickname") };

        firestore.Document("users/" + auth.CurrentUser.UserId).
            SetAsync(playerInfo, SetOptions.MergeFields("nickname"));
    }
}
