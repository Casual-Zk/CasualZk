using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class MenuManager : MonoBehaviour
{
    MatchManager matchManager;
    RoomManager roomManager;

    [Header("Objects")]
    [SerializeField] DisplayMessage messageUI;
    [SerializeField] Canvas MenuCanvas;
    [SerializeField] Canvas profileCanvas;

    [Header("Nickname")]
    [SerializeField] TMP_InputField Nickname_Input;
    [SerializeField] GameObject[] setNicknameUIs;
    [SerializeField] TextMeshProUGUI[] nicknameTexts;

    [Header("Info UI")]
    [SerializeField] TextMeshProUGUI walletAdddressText;
    [SerializeField] TextMeshProUGUI tokenBalanceText;
    [SerializeField] TextMeshProUGUI eggBalanceText;

    [Header("Inventory UI")]
    [SerializeField] TextMeshProUGUI dassd;

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
    }

    public void Btn_SetNickname()
    {
        if (Nickname_Input.text == null || Nickname_Input.text == "")
            messageUI.Display("Nickname can not be empty!", 2f);
        else
            FindAnyObjectByType<FirebaseDataManager>().SetNickname(Nickname_Input.text);
    }

    public void DisplayNickname(string nickname)
    {
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
    }

    public void FindMatchButton(){
        if (PlayerPrefs.HasKey("Nickname"))
        {
            roomManager.FindMatch();
            MenuCanvas.enabled = false;
        }        
    }

    // ------ PROFILE FUNCTIONS ------ //

    // ------ INVENTORY FUNCTIONS ------ //

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
