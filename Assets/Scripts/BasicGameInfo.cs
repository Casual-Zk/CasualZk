using System;
using System.Collections.Generic;
using Firebase.Firestore;

[FirestoreData]
public class BasicGameInfo
{
    [FirestoreProperty]
    public string currentWeek { get; set; }
}
