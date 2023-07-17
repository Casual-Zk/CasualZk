using System;
using System.Collections.Generic;
using Firebase.Firestore;

[FirestoreData]
public class PlayerInfo
{
    // Basic Info

    // Size (bytes) -- Doc title size: users/6YcqslYgTFX3lZlvHG0sj1LGw347 -> 34 bytes
    /* 9+16 */        [FirestoreProperty] public string nickname { get; set; }
    /* 14+43 */        [FirestoreProperty] public string walletAddress { get; set; }
    /* 9+1  */        [FirestoreProperty] public bool isTester { get; set; }
    /* 19+8  */        [FirestoreProperty] public Timestamp weekInfoUpdateTime { get; set; }
    /* 10+8  */        [FirestoreProperty] public Timestamp lastLogin { get; set; }
    /* 11+8  */        [FirestoreProperty] public int matchCount { get; set; }
    /* 9+8  */        [FirestoreProperty] public int winCount { get; set; }
    /* 6+   */        [FirestoreProperty] public Dictionary<string, object> snaps { get; set; }
              
              // Total Egg number and weapon info
    /* 5+   */  [FirestoreProperty] public Dictionary<string, object> eggs { get; set; }
    /* -- 12+8  */  [FirestoreProperty] public int knifeAmount { get; set; }
    /* -- 12+8  */  [FirestoreProperty] public int glockAmount { get; set; }
    /* -- 14+8  */  [FirestoreProperty] public int shotgunAmount { get; set; }
    /* -- 9+8  */  [FirestoreProperty] public int m4Amount { get; set; }
    /* -- 10+8  */  [FirestoreProperty] public int awpAmount { get; set; }
               
               // Amount of ammo that user have
    /* -- 16+8  */  [FirestoreProperty] public int wallet_12_gauge { get; set; }
    /* -- 11+8  */  [FirestoreProperty] public int wallet_9mm { get; set; }
    /* -- 14+8  */  [FirestoreProperty] public int wallet_5_56mm { get; set; }
    /* -- 14+8  */  [FirestoreProperty] public int wallet_7_62mm { get; set; }
    /* 14+8  */  [FirestoreProperty] public int game_12_gauge { get; set; }
    /* 10+8  */  [FirestoreProperty] public int game_9mm { get; set; }
    /* 12+8  */  [FirestoreProperty] public int game_5_56mm { get; set; }
    /* 12+8  */  [FirestoreProperty] public int game_7_62mm { get; set; }
    /* -- 18+8  */  [FirestoreProperty] public int consumed_12_gauge { get; set; }
    /* -- 13+8  */  [FirestoreProperty] public int consumed_9mm { get; set; }
    /* -- 16+8  */  [FirestoreProperty] public int consumed_5_56mm { get; set; }
    /* -- 16+8  */  [FirestoreProperty] public int consumed_7_62mm { get; set; }

    /*
     * Normal ones: 287 bytes
     * Unnecessary: 279 bytes
     * Maps: 12 + 16 for each week. 76-> mounth
     * 
     * 
     * 
     */
}
