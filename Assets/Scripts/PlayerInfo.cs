using System;
using System.Collections.Generic;
using Firebase.Firestore;

[FirestoreData]
public class PlayerInfo
{
    // Basic Info
    [FirestoreProperty] public string nickname { get; set; }
    [FirestoreProperty] public string walletAddress { get; set; }
    [FirestoreProperty] public bool isTester { get; set; }
    [FirestoreProperty] public Timestamp weekInfoUpdateTime { get; set; }
    [FirestoreProperty] public Timestamp lastLogin { get; set; }
    [FirestoreProperty] public int matchCount { get; set; }
    [FirestoreProperty] public int winCount { get; set; }

    // Total Egg number and weapon info
    [FirestoreProperty] public Dictionary<string, object> eggs { get; set; }
    [FirestoreProperty] public int knifeAmount { get; set; }
    [FirestoreProperty] public int glockAmount { get; set; }
    [FirestoreProperty] public int shotgunAmount { get; set; }
    [FirestoreProperty] public int m4Amount { get; set; }
    [FirestoreProperty] public int awpAmount { get; set; }

    // Amount of ammo that user have
    [FirestoreProperty] public int wallet_12_gauge { get; set; }
    [FirestoreProperty] public int wallet_9mm { get; set; }
    [FirestoreProperty] public int wallet_5_56mm { get; set; }
    [FirestoreProperty] public int wallet_7_62mm { get; set; }
    [FirestoreProperty] public int game_12_gauge { get; set; }
    [FirestoreProperty] public int game_9mm { get; set; }
    [FirestoreProperty] public int game_5_56mm { get; set; }
    [FirestoreProperty] public int game_7_62mm { get; set; }
    [FirestoreProperty] public int consumed_12_gauge { get; set; }
    [FirestoreProperty] public int consumed_9mm { get; set; }
    [FirestoreProperty] public int consumed_5_56mm { get; set; }
    [FirestoreProperty] public int consumed_7_62mm { get; set; }
}
