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
    [FirestoreProperty] public bool appPaused { get; set; }
    [FirestoreProperty] public bool openFPS { get; set; }
    [FirestoreProperty] public int topUserRecordAmount { get; set; }
    [FirestoreProperty] public int recordUpdateTime { get; set; }
    [FirestoreProperty] public int snapDocLimit { get; set; }

    // Ref lists
    [FirestoreProperty] public List<string> refList { get; set; }
    [FirestoreProperty] public List<string> refListOne { get; set; }
    [FirestoreProperty] public List<string> refListUsed { get; set; }
}
