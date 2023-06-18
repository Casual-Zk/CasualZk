using System;
using System.Collections.Generic;
using Firebase.Firestore;

[FirestoreData]
public class BasicGameInfo
{
    [FirestoreProperty] public int currentWeek { get; set; }
    [FirestoreProperty] public int playerAmount { get; set; }
    [FirestoreProperty] public float onlineSeed { get; set; }
    [FirestoreProperty] public string appVersion { get; set; }
    [FirestoreProperty] public int topUserRecordAmount { get; set; }
}
