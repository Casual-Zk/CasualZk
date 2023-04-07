using System;
using System.Collections.Generic;
using Firebase.Firestore;

[FirestoreData]
public class PlayerInfo
{
    // Basic Info
    [FirestoreProperty]
    public string nickname { get; set; }
    [FirestoreProperty]
    public int userID { get; set; }
    [FirestoreProperty]
    public string walletAddress { get; set; }

    // Total Egg number and weapon info
    [FirestoreProperty]
    public Dictionary<string, object> eggs { get; set; }
    [FirestoreProperty]
    public bool hasKnife { get; set; }
    [FirestoreProperty]
    public bool hasGlock { get; set; }
    [FirestoreProperty]
    public bool hasShotgun { get; set; }
    [FirestoreProperty]
    public bool hasM4 { get; set; }
    [FirestoreProperty]
    public bool hasAWP { get; set; }

    // Amount of ammo that user have
    [FirestoreProperty]
    public int ammo_5_65mm { get; set; }
    [FirestoreProperty]
    public int ammo_7_62mm { get; set; }
    [FirestoreProperty]
    public int ammo_9mm { get; set; }
    [FirestoreProperty]
    public int ammo_12_gauge { get; set; }
}
