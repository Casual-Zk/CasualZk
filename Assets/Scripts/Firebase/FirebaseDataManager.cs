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
    private ListenerRegistration registration;

    private FirebaseFirestore firestore;
    private FirebaseAuth auth;

    private void Awake()
    {
        firestore = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
    }
    
    public void OnLogin()
    {
        registration = firestore.Document("users/" + auth.CurrentUser.UserId).Listen(snaphot =>
        {
            PlayerInfo info = snaphot.ConvertTo<PlayerInfo>();
            Debug.Log("Player Nickname: " + info.nickname);

            // Use your player info here as you wish
            FindObjectOfType<MenuManager>().DisplayNickname(info.nickname);
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
    }
    
    public void SetNickname(string nickname)
    {
        PlayerInfo playerInfo = new PlayerInfo { nickname = nickname };

        firestore.Document("users/" + auth.CurrentUser.UserId).
            SetAsync(playerInfo, SetOptions.MergeFields("nickname"));
    }
  
    
}
