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

    [SerializeField] TextMeshProUGUI onlinePlayerCounter;

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
    [SerializeField] TextMeshProUGUI[] weaponBalanceText;
    [SerializeField] TextMeshProUGUI[] ammoBalanceText;

    [Header("Lottery UI")]
    [SerializeField] TMP_InputField weekCounterInput;
    int weekCounter = 0;

    [Header("Settings UI")]
    [SerializeField] GameObject settingsUI;
    [SerializeField] float moveSpeed;
    [SerializeField] Slider musicVolSlider;
    [SerializeField] Slider SFXVolSlider;
    bool openStart;
    bool closeStart;
    bool settingsOpen;
    float closedPos = 1600f;
    float openedPos = 600f;
    float moveDistance;
    Vector3 localPos;       // same but for localPos as a whole

    private void Start()
    {
        StartMenu();
        weekCounterInput.text = weekCounter.ToString();

        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        if (!PlayerPrefs.HasKey("MusicVol")) Btn_SaveSettings();
        else
        {
            musicVolSlider.value = PlayerPrefs.GetFloat("MusicVol");
            SFXVolSlider.value = PlayerPrefs.GetFloat("SFXVol");
        }

        moveDistance = closedPos - openedPos;
    }

    private void Update()
    {
        if (openStart)
        {
            localPos = settingsUI.transform.localPosition;

            // Stop when arrive
            if (localPos.x <= openedPos) 
            {
                openStart = false;
                settingsOpen = true;
                settingsUI.transform.localPosition = new Vector3(openedPos - 1f, localPos.y, localPos.z);
                return;
            }

            // Move left with normal speed till get the 60% of the distance
            if (localPos.x > closedPos - (moveDistance * 0.6f))
            {
                settingsUI.transform.localPosition -= new Vector3(moveSpeed * Time.deltaTime, 0, 0);
            }
            // Move slower when getting closer
            else if (localPos.x > closedPos - (moveDistance * 0.8f))
            {
                settingsUI.transform.localPosition -= new Vector3(moveSpeed / 2 * Time.deltaTime, 0, 0);
            }
            // Move slower when getting closer
            else if (localPos.x > closedPos - (moveDistance * 0.9f))
            {
                settingsUI.transform.localPosition -= new Vector3(moveSpeed / 4 * Time.deltaTime, 0, 0);
            }
            // Move slower when getting closer
            else
            {
                settingsUI.transform.localPosition -= new Vector3(moveSpeed / 8 * Time.deltaTime, 0, 0);
            }
        }
        if (closeStart)
        {
            localPos = settingsUI.transform.localPosition;

            // Stop when arrive
            if (localPos.x >= closedPos)
            {
                closeStart = false;
                settingsOpen = false;
                settingsUI.transform.localPosition = new Vector3(closedPos + 1f, localPos.y, localPos.z);
                return;
            }

            // Move left with half of the normal speed
            settingsUI.transform.localPosition += new Vector3(moveSpeed / 2 * Time.deltaTime, 0, 0);
        }
    }

    public void StartMenu()
    {
        matchManager = FindObjectOfType<MatchManager>();
        roomManager = FindObjectOfType<RoomManager>();
        dm = FindObjectOfType<FirebaseDataManager>();

        // if a new version is available, Display update UI
        if (dm.gameInfo != null && dm.gameInfo.appVersion != Application.version)
            FindObjectOfType<FirebaseAuthManager>().DisplayAppUpdateUI();
    }

    public void Btn_SetNickname()
    {
        if (Nickname_Input.text == null || Nickname_Input.text == "")
            messageUI.Display("Nickname can not be empty!", 2f);
        else
            FindAnyObjectByType<FirebaseDataManager>().SetNickname(Nickname_Input.text);
    }

    public void Btn_Quit() { Application.Quit(); }

    public void Btn_OpenSettingsUI() 
    {
        if (settingsOpen)
        {
            closeStart = true; openStart = false;
        }
        else
        {
            closeStart = false; openStart = true;
        }
    }

    public void Btn_SaveSettings()
    {
        // Apply settings to the audio manager

        // Records settings to the player prefs
        PlayerPrefs.SetFloat("MusicVol", musicVolSlider.value);
        PlayerPrefs.SetFloat("SFXVol", SFXVolSlider.value);

        // Close settings UI
        openStart = false;
        closeStart = true;
    }

    public void DisplayInfo()
    {
        string nickname = dm.playerInfo.nickname;
        string walletAddress = dm.playerInfo.walletAddress;
        int currentWeek = dm.gameInfo.currentWeek;
        var eggCount = dm.playerInfo.eggs[currentWeek.ToString()];

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

        foreach (TextMeshProUGUI text in currentWeekTexts) { text.text = currentWeek.ToString(); }
        foreach (TextMeshProUGUI text in eggBalanceTexts) { text.text = "x " + eggCount; }
    }

    public void FindMatchButton(){
        bool hasWeapons = false;

        foreach(int balance in dm.weaponBalance)
        {
            if (balance > 0) { hasWeapons = true; break; }
        }

        if (!hasWeapons) { messageUI.Display("You don't have any weapons to play with!!", 5f); return; }

        if (PlayerPrefs.HasKey("Nickname"))
        {
            roomManager.FindMatch();
            MenuCanvas.enabled = false;
        }
        else { messageUI.Display("Error: Set a username first!", 3f); }
    }

    public void UpdateOnlineCounter(int count)
    {
        //Debug.LogWarning("dm Seed: " + dm.gameInfo.onlineSeed);
        count = (int)(dm.gameInfo.onlineSeed * count);
        onlinePlayerCounter.text = "Online: " + count;
    }

    // ------ PROFILE FUNCTIONS ------ //

    // ------ INVENTORY FUNCTIONS ------ //
    public void DisplayInventory()
    {
        for (int i = 0; i < dm.weaponBalance.Length; i++)
        {
            if (dm.weaponBalance[i] > 0)
            {
                weapons[i].SetActive(true);
                weaponBalanceText[i].text = "x" + dm.weaponBalance[i].ToString();
            }

            if (i == 0) continue; // skip knife
            if (dm.ammoBalance[i] > 0)
            {
                ammo[i].SetActive(true); 
                ammoBalanceText[i].text = "x" + dm.ammoBalance[i].ToString();
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
