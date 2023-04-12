using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class MenuManager : MonoBehaviour
{
    MatchManager matchManager;
    RoomManager roomManager;
    FirebaseDataManager dm;

    [Header("Objects")]
    [SerializeField] DisplayMessage messageUI;
    [SerializeField] Canvas MenuCanvas;
    [SerializeField] Canvas profileCanvas;
    [SerializeField] public GameObject findMatchButton;

    [Header("Nickname")]
    [SerializeField] TMP_InputField Nickname_Input;
    [SerializeField] GameObject[] setNicknameUIs;
    [SerializeField] TextMeshProUGUI[] nicknameTexts;

    [Header("Info UI")]
    [SerializeField] TextMeshProUGUI walletAdddressText;
    [SerializeField] TextMeshProUGUI tokenBalanceText;
    [SerializeField] TextMeshProUGUI[] eggBalanceTexts;
    [SerializeField] TextMeshProUGUI[] currentWeekTexts;

    [Header("Inventory UI")]
    [SerializeField] GameObject[] weapons;
    [SerializeField] GameObject[] ammo;
    [SerializeField] TextMeshProUGUI[] ammoBalanceText;

    [Header("Lottery UI")]
    [SerializeField] TMP_InputField weekCounterInput;
    int weekCounter = 0;

    private void Start()
    {
        StartMenu();
        weekCounterInput.text = weekCounter.ToString();
    }

    public void StartMenu()
    {
        matchManager = FindObjectOfType<MatchManager>();
        roomManager = FindObjectOfType<RoomManager>();
        dm = FindObjectOfType<FirebaseDataManager>();
    }

    public void Btn_SetNickname()
    {
        if (Nickname_Input.text == null || Nickname_Input.text == "")
            messageUI.Display("Nickname can not be empty!", 2f);
        else
            FindAnyObjectByType<FirebaseDataManager>().SetNickname(Nickname_Input.text);
    }

    public void DisplayInfo()
    {
        string nickname = dm.playerInfo.nickname;
        string walletAddress = dm.playerInfo.walletAddress;
        string currentWeek = dm.gameInfo.currentWeek;
        var eggCount = dm.playerInfo.eggs[currentWeek];

        if (nickname != null)
        {
            foreach (GameObject ui in setNicknameUIs) ui.SetActive(false);
            foreach (TextMeshProUGUI text in nicknameTexts) text.text = nickname;
            PlayerPrefs.SetString("Nickname", nickname);
        }
        else
        {
            foreach (GameObject ui in setNicknameUIs) ui.SetActive(true);
            foreach (TextMeshProUGUI text in nicknameTexts) text.text = "";
            PlayerPrefs.DeleteKey("Nickname");
        }

        walletAdddressText.text = walletAddress;

        foreach (TextMeshProUGUI text in currentWeekTexts) { text.text = currentWeek; }
        foreach (TextMeshProUGUI text in eggBalanceTexts) { text.text = "x " + eggCount; }
    }

    public void FindMatchButton(){
        bool hasWeapons = false;

        foreach(bool weapons in dm.hasWeapon)
        {
            if (weapons) { hasWeapons = true; break; }
        }

        if (!hasWeapons) { messageUI.Display("You don't have any weapons to play with!!", 5f); return; }

        if (PlayerPrefs.HasKey("Nickname"))
        {
            roomManager.FindMatch();
            MenuCanvas.enabled = false;
        }
        else { messageUI.Display("Error: Set a username first!", 3f); }
    }

    // ------ PROFILE FUNCTIONS ------ //

    // ------ INVENTORY FUNCTIONS ------ //
    public void DisplayInventory()
    {
        for (int i = 0; i < dm.hasWeapon.Length; i++)
        {
            if (dm.hasWeapon[i]) weapons[i].SetActive(true);

            if (i == 0) continue; // skip knife
            if (dm.ammoBalance[i] > 0)
            {
                ammo[i].SetActive(true); 
                ammoBalanceText[i].text = dm.ammoBalance[i].ToString();
            }
            else  // if ammo is finished, then disable
                ammo[i].SetActive(false);
        }
    }

    // ------ LOTTERY FUNCTIONS ------ //
    public void WeekCounterButtons(bool right)
    {
        if (right)
        {
            if (weekCounter >= 10) return; 

            weekCounter++;
            weekCounterInput.text = weekCounter.ToString();
        }
        else
        {
            weekCounter--;

            if (weekCounter <= 0) weekCounter = 0;

            weekCounterInput.text = weekCounter.ToString();
        }
    }

}
