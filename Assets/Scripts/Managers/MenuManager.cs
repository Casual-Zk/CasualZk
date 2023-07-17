using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Newtonsoft.Json;
using Photon.Pun;

public class MenuManager : MonoBehaviour, IPointerDownHandler
{
    MatchManager matchManager;
    RoomManager roomManager;
    FirebaseDataManager dm;

    [SerializeField] TextMeshProUGUI onlinePlayerCounter;

    [Header("Objects")]
    [SerializeField] DisplayMessage messageUI;
    [SerializeField] GameObject MenuCanvas;
    [SerializeField] GameObject ProfileCanvas;
    [SerializeField] GameObject MainMenuLoadingCanvas;
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
    [SerializeField] GameObject usernameBackButton;
    [SerializeField] TextMeshProUGUI matchCountText;
    [SerializeField] TextMeshProUGUI winRateText;
    [SerializeField] TextMeshProUGUI versionText;

    [Header("Inventory UI")]
    [SerializeField] GameObject[] weapons;
    [SerializeField] GameObject[] ammo;
    [SerializeField] TextMeshProUGUI[] weaponBalanceText;
    [SerializeField] TextMeshProUGUI[] ammoBalanceText;

    [Header("Lottery UI")]
    [SerializeField] TMP_InputField weekCounterInput;
    [SerializeField] TextMeshProUGUI playerCountText;
    [SerializeField] TextMeshProUGUI eggCountText;
    [SerializeField] TextMeshProUGUI playerEggText;
    [SerializeField] GameObject topUsersPanel;
    [SerializeField] TopUser topUserPrefab;
    int weekCounter = 1;
    int weekRecordDiff = 0;
    Dictionary<string, object>[] allTopUsers;

    [Header("Settings UI")]
    [SerializeField] GameObject settingsUI;
    [SerializeField] float moveSpeed;
    [SerializeField] Slider musicVolSlider;
    [SerializeField] Slider SFXVolSlider;
    [SerializeField] Toggle fpsToggle;
    [SerializeField] GameObject fpsSettingContainer;
    [SerializeField] GameObject fpsUI;
    [SerializeField] TextMeshProUGUI fpsText;
    bool openStart;
    bool closeStart;
    bool settingsOpen;
    float closedPos = 1600f;
    float openedPos = 600f;
    float moveDistance;
    Vector3 localPos;       // same but for localPos as a whole

    AudioManager audioManager;
    Coroutine fpsCoroutine = null;


    private void Start()
    {
        matchManager = FindObjectOfType<MatchManager>();
        roomManager = FindObjectOfType<RoomManager>();
        dm = FindObjectOfType<FirebaseDataManager>();

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        versionText.text = "v" + Application.version;

        StartMenu(false);
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

    public void OnPointerDown(PointerEventData eventData)
    {
        // Get the GameObject that the user touched
        GameObject touchedObject = eventData.pointerCurrentRaycast.gameObject;

        // Perform the desired action based on the touched object
        if (touchedObject != null)
        {
            //Debug.Log("User touched: " + touchedObject.name);
            // If settings open and toched object is not a setttings UI element, then close settings UI
            if (settingsOpen && !touchedObject.name.Contains("Sett"))
            {
                Btn_OpenSettingsUI();
            }
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

    public void StartMenu(bool checkConnection)
    {
        if (checkConnection) StartCoroutine(CheckNetworkConnectionOnDelay());

        MenuCanvas.SetActive(true);

        // if a new version is available, Display update UI
        if (dm.gameInfo != null && UpdateNeeded(dm.gameInfo.appVersion))
            FindObjectOfType<FirebaseAuthManager>().DisplayAppUpdateOrPauseUI(true);

        // if the game is in maintanence, Display maintenance UI
        if (dm.gameInfo != null && dm.gameInfo.appPaused)
            FindObjectOfType<FirebaseAuthManager>().DisplayAppUpdateOrPauseUI(false);
    }

    IEnumerator CheckNetworkConnectionOnDelay()
    {
        Debug.Log("Checking network connection...");
        MainMenuLoadingCanvas.SetActive(true);

        yield return new WaitForSeconds(4f);
        MainMenuLoadingCanvas.SetActive(false);

        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("Re-Connecting to server...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    private bool UpdateNeeded(string databaseVersion)
    {
        if (databaseVersion == Application.version) return false;

        string[] dbVersionNumbers = databaseVersion.Split(".");
        string[] localVersionNumbers = Application.version.Split(".");

        for (int i = 0; i < dbVersionNumbers.Length; i++)
        {
            //Debug.Log("Local: " + localVersionNumbers[i] + " - DB: " + dbVersionNumbers[i]);
            if (int.Parse(dbVersionNumbers[i]) > int.Parse(localVersionNumbers[i])) return true;
        }

        return false;
    }

    public void Btn_SetNickname()
    {
        if (Nickname_Input.text == null || Nickname_Input.text == "")
            messageUI.Display("Nickname can not be empty!", 2f);
        else
            FindObjectOfType<FirebaseDataManager>().SetNickname(Nickname_Input.text);
    }
    
    public void Btn_ShowBackButtonOnUsername()
    {
        if (PlayerPrefs.HasKey("Nickname"))
            usernameBackButton.SetActive(true);
        else 
            usernameBackButton.SetActive(false);
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
        
        if (fpsToggle.isOn)
        {
            PlayerPrefs.SetInt("OpenFPS", 1);
            ApplyFPSSettings(true);
        }
        else
        {
            PlayerPrefs.SetInt("OpenFPS", 0);
            ApplyFPSSettings(false);
        }

        // Apply settings to the audio manager
        audioManager.ApplyVolumeSettings();

        // Close settings UI
        openStart = false;
        closeStart = true;
    }

    private void ApplyFPSSettings(bool isActive)
    {
        if (dm.gameInfo == null) return;

        // If we allow the FPS settings, apply the preferences
        if (dm.gameInfo.openFPS)
        {
            fpsUI.SetActive(isActive);
            if (isActive) fpsCoroutine = StartCoroutine(FpsCoroutine());
            else if (fpsCoroutine != null) { StopCoroutine(fpsCoroutine); fpsCoroutine = null; }
        }
        
        // If we turn off it, no matter what preferences is, close the FPS UI
        else
        {
            fpsUI.SetActive(false);
            
            // Stop if there is coroutine
            if (fpsCoroutine != null) { StopCoroutine(fpsCoroutine); fpsCoroutine = null; }
        }
    }

    IEnumerator FpsCoroutine()
    {
        int fps = (int)(1f / Time.deltaTime);
        fpsText.text = fps.ToString();
        //Debug.Log("FPS: " + fps);

        yield return new WaitForSeconds(1f);
        fpsCoroutine = StartCoroutine(FpsCoroutine());
    }

    public void DisplayInfo()
    {
        string nickname = dm.playerInfo.nickname;
        string walletAddress = dm.playerInfo.walletAddress;
        int currentWeek = dm.gameInfo.currentWeek;
        var eggCount = dm.playerInfo.eggs[currentWeek.ToString()];
        float matches = dm.playerInfo.matchCount;
        float wins = dm.playerInfo.winCount;

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
            MenuCanvas.SetActive(false);
            ProfileCanvas.SetActive(true);
            PlayerPrefs.DeleteKey("Nickname");
        }

        walletAdddressText.text = walletAddress.Substring(0, 6) + "...." + walletAddress.Substring(37, 4);

        foreach (TextMeshProUGUI text in currentWeekTexts) { text.text = currentWeek.ToString(); }
        foreach (TextMeshProUGUI text in eggBalanceTexts) { text.text = "x " + eggCount; }

        if (matches <= 0) return;
        float winRate = Mathf.Ceil(wins / matches * 100f);
        matchCountText.text = matches.ToString();
        winRateText.text = winRate.ToString() + "%";
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
        }
        else { messageUI.Display("Error: Set a username from profile!", 3f); }
    }

    public void SetMenuCanvas(bool isActive, bool checkConnection)
    {
        if (checkConnection) StartCoroutine(CheckNetworkConnectionOnDelay());

        MenuCanvas.SetActive(isActive);
    }

    public void UpdateOnlineCounter(int count)
    {
        if (dm.gameInfo == null) return;

        //Debug.LogWarning("dm Seed: " + dm.gameInfo.onlineSeed);
        count = (int)(dm.gameInfo.onlineSeed * count);
        onlinePlayerCounter.text = "Online: " + count;
    }

    public void OnGameInfoReceived()
    {
        // Show/Hide it from settings
        fpsSettingContainer.SetActive(dm.gameInfo.openFPS);

        // Show/Hide UI
        fpsUI.SetActive(dm.gameInfo.openFPS);

        if (dm.gameInfo.openFPS)
        {
            if (!PlayerPrefs.HasKey("OpenFPS")) return;

            if (PlayerPrefs.GetInt("OpenFPS") == 1)
            {
                fpsToggle.isOn = true;
                ApplyFPSSettings(true);
            }
            else
            {
                fpsToggle.isOn = false;
                ApplyFPSSettings(false);
            }
        }
        else
        {
            // Stop if there is coroutine
            if (fpsCoroutine != null) { StopCoroutine(fpsCoroutine); fpsCoroutine = null; }            
        }
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
            if (weekCounter >= dm.gameInfo.currentWeek) return; 

            weekCounter++;
            DisplayTopUsers(allTopUsers[weekCounter - weekRecordDiff], weekCounter);
        }
        else
        {
            if (weekCounter <= dm.gameInfo.currentWeek - dm.gameInfo.topUserRecordAmount + 1) return;

            weekCounter--;
            DisplayTopUsers(allTopUsers[weekCounter - weekRecordDiff], weekCounter);
        }
    }

    public void OnReturnAllTopUsers(Dictionary<string, object>[] allTopUsers)
    {
        //Debug.Log("-- OnReturnAll --");
        this.allTopUsers = new Dictionary<string, object>[allTopUsers.Length];
        this.allTopUsers = allTopUsers;
        /*
        Debug.Log("All top count: " + allTopUsers.Length);
        for (int i = 0; i <= allTopUsers.Length; i++)
        {
            Dictionary<string, object> _user = (Dictionary<string, object>)allTopUsers[i]["1"];
            Debug.Log(_user["eggs"]);
        }
        */
    }

    public void OnCurrentWeekTopUserUpdate(Dictionary<string, object> topUsers)
    {
        //Debug.Log("here in on cureent week top user");
        //Dictionary<string, object> user = (Dictionary<string, object>)topUsers["1"];
        //Debug.Log(user);
        //Debug.Log(user["userID"]);
        if (allTopUsers == null) allTopUsers = new Dictionary<string, object>[dm.gameInfo.topUserRecordAmount];

        allTopUsers[dm.gameInfo.topUserRecordAmount - 1] = topUsers;
        DisplayTopUsers(topUsers, dm.gameInfo.currentWeek);

        weekRecordDiff = (dm.gameInfo.currentWeek - dm.gameInfo.topUserRecordAmount) + 1;
    }
    
    private void DisplayTopUsers(Dictionary<string, object> topUsers, int weekNumber)
    {
        //Debug.Log("On Display Top Users - Week: " + weekNumber);
        //Dictionary<string, object> _user = (Dictionary<string, object>)topUsers["0"];
        //Debug.Log(_user);
        //Debug.Log(_user["playerCount"]);

        // Clear the panel first
        TopUser[] currentTopList = topUsersPanel.GetComponentsInChildren<TopUser>();
        foreach (TopUser listUser in currentTopList) { Destroy(listUser.gameObject); }

        // Instantiate top users
        for (int i = 0; i < topUsers.Count; i++)
        {
            Dictionary<string, object> user = (Dictionary<string, object>)topUsers[i.ToString()];
            
            if (i == 0) // Which is the week's soft info (egg and player count)
            {
                try
                {
                    var eggCount = dm.playerInfo.eggs[weekNumber.ToString()];
                    playerEggText.text = "Your Eggs: " + eggCount;
                }
                catch { playerEggText.text = "Your Eggs: 0"; }

                playerCountText.text = "Total Player Count: " + JsonConvert.SerializeObject(user["playerCount"]);
                eggCountText.text = "Total Egg Count: " + JsonConvert.SerializeObject(user["eggCount"]);

                continue;
            }

            string nickname = JsonConvert.SerializeObject(user["nickname"]).Trim('"');
            string eggs = JsonConvert.SerializeObject(user["eggs"]);
            string walletAddress = JsonConvert.SerializeObject(user["walletAddress"]).Trim('"');
            walletAddress = walletAddress.Substring(0, 6) + "...." + walletAddress.Substring(37, 4);    // Shorten

            //Debug.Log("nickname: " + nickname);
            //Debug.Log("eggs: " + eggs);

            TopUser newUser = Instantiate(topUserPrefab, topUsersPanel.transform);
            newUser.AssignValues(i, walletAddress, nickname, "-", eggs);
            //newUser.AssignValues(pair.Key, user["walletAddress"], user["nickname"], user["matches"], user["eggs"]);
        }
        // adjust the week input to the current week
        weekCounter = weekNumber;
        weekCounterInput.text = weekNumber.ToString();
    }
}
