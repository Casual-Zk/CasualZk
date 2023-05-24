using System;
using System.Collections.Generic;
using Firebase.Firestore;

[FirestoreData]
public class BasicGameInfo
{
    [FirestoreProperty] public int currentWeek { get; set; }
    [FirestoreProperty] public int playerAmount { get; set; }
}
