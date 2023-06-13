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
    [SerializeField] GameObject MenuCanvas;
    [SerializeField] Canvas loopingCanvas;
    [SerializeField] public GameObject findMatchButton;
    [SerializeField] TextMeshProUGUI loopingText;
    [SerializeField] TextMeshProUGUI cloneText;
    [SerializeField] float loopingSpeed;
    RectTransform textRectTransform;
    float textWidth;
    Vector3 textStartPos;
    float scrollPos;

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

    AudioManager audioManager;


    private void Start()
    {
        StartMenu();
        weekCounterInput.text = weekCounter.ToString();

        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        audioManager = FindObjectOfType<AudioManager>();

        if (!PlayerPrefs.HasKey("MusicVol")) Btn_SaveSettings();
        else
        {
            musicVolSlider.value = PlayerPrefs.GetFloat("MusicVol");
            SFXVolSlider.value = PlayerPrefs.GetFloat("SFXVol");

            audioManager.ApplyVolumeSettings();
        }

        moveDistance = closedPos - openedPos;

        // Looping text vars
        textRectTransform = loopingText.GetComponent<RectTransform>();
        textWidth = loopingText.preferredWidth;
        textStartPos = textRectTransform.position;
        scrollPos = 0;
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

    private void FixedUpdate()
    {
        loopingCanvas.enabled = MenuCanvas.activeSelf;

        // Text looping
        if (dm.dv != null && loopingText.text != dm.dv.loopingText)
        {
            loopingText.text = dm.dv.loopingText;
            cloneText.text = dm.dv.loopingText;

            loopingSpeed = dm.dv.loopingTextSpeed;

            textWidth = loopingText.preferredWidth;
            scrollPos = 0;

            Debug.Log("New witdh: " + textWidth);
            Debug.Log("New X given: " + textWidth / 0.75f);

            cloneText.GetComponent<RectTransform>().localPosition = new Vector3(textWidth, 0, 0);
        }

        if (scrollPos < -textWidth) scrollPos = 0;
        textRectTransform.position = new Vector3(scrollPos / 0.75f, textStartPos.y, textStartPos.z);
        scrollPos -= loopingSpeed * Time.deltaTime;
    }

    public void PlayClickSound()
    {
        audioManager.Play("Click_SFX");
    }

    public void StartMenu()
    {
        MenuCanvas.SetActive(true);
        matchManager = FindObjectOfType<MatchManager>();
        roomManager = FindObjectOfType<RoomManager>();
        dm = FindObjectOfType<FirebaseDataManager>();

        // if a new version is available, Display update UI
        if (dm.gameInfo != null && UpdateNeeded(dm.gameInfo.appVersion))
            FindObjectOfType<FirebaseAuthManager>().DisplayAppUpdateUI();
    }

    private bool UpdateNeeded(string databaseVersion)
    {
        if (databaseVersion == Application.version) return false;

        string[] dbVersionNumbers = databaseVersion.Split(".");
        string[] localVersionNumbers = Application.version.Split(".");

        for (int i = 0; i < dbVersionNumbers.Length; i++)
        {
            if (int.Parse(localVersionNumbers[i]) > int.Parse(dbVersionNumbers[i])) return false;
        }

        return true;
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
        // Records settings to the player prefs
        PlayerPrefs.SetFloat("MusicVol", musicVolSlider.value);
        PlayerPrefs.SetFloat("SFXVol", SFXVolSlider.value);

        // Apply settings to the audio manager
        audioManager.ApplyVolumeSettings();

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

        walletAdddressText.text = walletAddress.Substring(0, 6) + "...." + walletAddress.Substring(37, 4);

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
            MenuCanvas.SetActive(false);
        }
        else { messageUI.Display("Error: Set a username from profile!", 3f); }
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
