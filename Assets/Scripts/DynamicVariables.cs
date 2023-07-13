using System;
using System.Collections.Generic;
using Firebase.Firestore;

[FirestoreData]
public class DynamicVariables
{
    [FirestoreProperty] public float matchTime { get; set; }
    [FirestoreProperty] public int topUsers { get; set; }
    [FirestoreProperty] public float hitEffectSpeed { get; set; }
    [FirestoreProperty] public float hitCamShake_Amplitude { get; set; }
    [FirestoreProperty] public float hitCamShake_Frequency { get; set; }
    [FirestoreProperty] public float hitCamShake_Speed { get; set; }
    [FirestoreProperty] public float multipleKillTime { get; set; }
    [FirestoreProperty] public int leaveWaitTime { get; set; }

    // Player
    [FirestoreProperty] public float player_CamOrthoSize { get; set; }
    [FirestoreProperty] public float player_CamXdamping { get; set; }
    [FirestoreProperty] public float player_FollowCamDistance { get; set; }
    [FirestoreProperty] public int player_Health { get; set; }
    [FirestoreProperty] public float player_JumpForce { get; set; }
    [FirestoreProperty] public float player_RunSpeed { get; set; }
    [FirestoreProperty] public float player_GravityScale { get; set; }
    [FirestoreProperty] public float player_Mass { get; set; }


    // Looping Text
    [FirestoreProperty] public string loopingText { get; set; }
    [FirestoreProperty] public float loopingTextSpeed { get; set; }


    // Other Weapons-Related stuff
    [FirestoreProperty] public float armorHealth { get; set; }
    [FirestoreProperty] public float supportHealth { get; set; }
    [FirestoreProperty] public float supportFireDamage { get; set; }

    // Weapon Damage
    [FirestoreProperty] public int weaponDamage_AWP { get; set; }
    [FirestoreProperty] public int weaponDamage_Glock { get; set; }
    [FirestoreProperty] public int weaponDamage_Knife { get; set; }
    [FirestoreProperty] public int weaponDamage_M4 { get; set; }
    [FirestoreProperty] public int weaponDamage_Shotgun { get; set; }

    // Fire Rate
    [FirestoreProperty] public float weaponFireRate_AWP { get; set; }
    [FirestoreProperty] public float weaponFireRate_Glock { get; set; }
    [FirestoreProperty] public float weaponFireRate_Knife { get; set; }
    [FirestoreProperty] public float weaponFireRate_M4 { get; set; }
    [FirestoreProperty] public float weaponFireRate_Shotgun { get; set; }
    
    // Reload Speed
    //[FirestoreProperty] public float weaponReloadSpeed_AWP { get; set; }
    //[FirestoreProperty] public float weaponReloadSpeed_Glock { get; set; }
    //[FirestoreProperty] public float weaponReloadSpeed_M4 { get; set; }
    //[FirestoreProperty] public float weaponReloadSpeed_Shotgun { get; set; }
    
    // Weapon Activation
    [FirestoreProperty] public bool weaponActive_AWP { get; set; }
    [FirestoreProperty] public bool weaponActive_Glock { get; set; }
    [FirestoreProperty] public bool weaponActive_Knife { get; set; }
    [FirestoreProperty] public bool weaponActive_M4 { get; set; }
    [FirestoreProperty] public bool weaponActive_Shotgun { get; set; }
    
    // Weapon Range
    [FirestoreProperty] public float weaponRange_AWP { get; set; }
    [FirestoreProperty] public float weaponRange_Glock { get; set; }
    [FirestoreProperty] public float weaponRange_M4 { get; set; }
    [FirestoreProperty] public float weaponRange_Shotgun { get; set; }
}

