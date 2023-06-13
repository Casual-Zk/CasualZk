using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TarodevController;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using TMPro;

public class TableItem
{
    public int score;
}

public class MatchManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public bool isGameOver { get; set; }

    AudioManager audioManager; 
    FirebaseDataManager dataManager;

    [SerializeField] float matchTime;
    [SerializeField] int DEBUG_extraPlayer;
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] public GameObject waitingPlayersCanvas;
    [SerializeField] TextMeshProUGUI playerCountText;

    [Header("Score")]
    [SerializeField] ScoreTable scorePrefab;
    [SerializeField] GameObject scorePanel;
    [SerializeField] ScoreTable miniScorePrefab;
    [SerializeField] GameObject miniScorePanel;
    [SerializeField] GameObject eggImage;

    [Header("Other")]
    [SerializeField] GameObject gameOverUI;
    [SerializeField] GameObject inGameUI;
    [SerializeField] GameObject endGameUI;
    [SerializeField] GameObject map_0;

    private float time;
    private Coroutine timerCoroutine;
    private bool isWaitingForPlayers;
    private int currentPlayerCount;
    private int neededPlayerCount;

    List<ScoreTable> scoreList = new List<ScoreTable>();

    public enum EventCodes : byte
    {
        RefreshTimer
    }
    public enum GameState
    {
        Waiting = 0,
        Starting = 1,
        Playing = 2,
        Ending = 3
    }

    private void Start()
    {
        isGameOver = true;
    }

    void Update()
    {
        if (isWaitingForPlayers)
        {
            currentPlayerCount = PhotonNetwork.CurrentRoom.Players.Count + DEBUG_extraPlayer;
            neededPlayerCount = dataManager.gameInfo.playerAmount;

            playerCountText.text = "(" + currentPlayerCount + "/" + neededPlayerCount + ")";

            // If enough player is connected to the room, then close the waiting UI and start the game
            if (currentPlayerCount >= neededPlayerCount)
            {
                isWaitingForPlayers = false;
                StartCoroutine(StartActualMatchOnDelay());
            }
        }
    }
    IEnumerator StartActualMatchOnDelay()
    {
        yield return new WaitForSeconds(2.5f);

        waitingPlayersCanvas.SetActive(false);
        isGameOver = false;

        if (PhotonNetwork.IsMasterClient)
        {
            InitializeTimer();
        }
    }

    public void StartMatch()
    {
        PhotonNetwork.AddCallbackTarget(this);
        audioManager = FindObjectOfType<AudioManager>();
        dataManager = FindObjectOfType<FirebaseDataManager>();

        inGameUI.SetActive(true);
        map_0.SetActive(true);
        timeText.text = matchTime.ToString();
        isWaitingForPlayers = true; // start waiting screen
    }

    void OnDestroy()
    {
        // Unsubscribe from the OnMasterClientSwitched callback
        PhotonNetwork.RemoveCallbackTarget(this);
    }
    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room CALLED");
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log("Master Switched");
        // Check if the current player is the new master client
        if (newMasterClient == PhotonNetwork.LocalPlayer)
        {
            Debug.Log("Local is the master");
            if (timerCoroutine != null) timerCoroutine = StartCoroutine(Timer());
        }
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code >= 200) return;

        EventCodes eventCode = (EventCodes) photonEvent.Code;
        object[] eventData = (object[])photonEvent.CustomData;

        switch (eventCode)
        {
            case EventCodes.RefreshTimer:
                RefreshTimer_Receive(eventData);
                break;
        }
    }

    private void EndGame()
    {
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        time = 0;
        RefreshTimerUI();

        isGameOver = true;
        gameOverUI.SetActive(true); 
        audioManager.Play("GameOver_SFX");

        // Save the balance to the database and get online player count
        FindObjectOfType<FirebaseDataManager>().UpdateAmmoBalance();

        StartCoroutine(DisplayMainScore());
    }

    private IEnumerator DisplayMainScore()
    {
        yield return new WaitForSeconds(3f);

        for (var i = miniScorePanel.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(miniScorePanel.transform.GetChild(i).gameObject);
        }

        gameOverUI.SetActive(false);
        inGameUI.SetActive(false);
        map_0.SetActive(false);
        endGameUI.SetActive(true);

        // Stop game music
        audioManager.Stop("Game_Music");

        ScoreTable winner = scoreList[0];

        // Add all players to the main score table
        for (int i = 0; i < scoreList.Count; i++)
        {
            ScoreTable player = Instantiate(scorePrefab, scorePanel.transform);
            player.playerName = scoreList[i].playerName;
            player.score = scoreList[i].score;
            player.kill = scoreList[i].kill;
            player.death = scoreList[i].death;
            player.suicide = scoreList[i].suicide;
            player.DisplayMainScoreTable();

            if (scoreList[i].score > winner.score) winner = scoreList[i];
        }

        // Succes/Fail SFX
        if (winner.playerName == PhotonNetwork.NickName)
        {
            audioManager.Play("Win_SFX");
            eggImage.SetActive(true);
            FindObjectOfType<FirebaseDataManager>().GiveEgg();
        }
        else audioManager.Play("Fail_SFX");

        yield return new WaitForSeconds(1f); // wait for other clients a bit?

        // Kick all players from room and close the room
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.DestroyAll();
            //PhotonNetwork.EnableCloseConnection = true;
        }

        // Leave the room and lobby
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.LeaveLobby();

        FindObjectOfType<RoomManager>().UpdateCounter();
    }

    public void EndGameButton()
    {
        // Clean the players data
        scoreList.Clear();

        for (var i = scorePanel.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(scorePanel.transform.GetChild(i).gameObject);
        }

        eggImage.SetActive(false);
        endGameUI.SetActive(false);
        FindObjectOfType<MenuManager>().StartMenu();

        audioManager.Play("Game_Music");
    }


    // ------------- TIMER ------------- //

    private void InitializeTimer()
    {
        time = matchTime;
        RefreshTimerUI();

        Debug.Log("initilizing Timer");

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("This is the master client");
            timerCoroutine = StartCoroutine(Timer());
        }
    }

    private void RefreshTimerUI()
    {
        timeText.text = ((int)time).ToString();
    }

    private IEnumerator Timer()
    {
        yield return new WaitForSeconds(1f);
        time--;

        if (time <= 0)
        {
            // Check whether we have winner or not
            ScoreTable winner = scoreList[0];

            for (int i = 0; i < scoreList.Count; i++)
            {
                if (scoreList[i].score > winner.score) winner = scoreList[i];
            }

            bool doubleWinner = false;
            foreach (ScoreTable player in scoreList)
            {
                if (player.playerName == winner.playerName) continue;
                if (player.score == winner.score) doubleWinner = true;
            }

            if (!doubleWinner) 
            {
                Debug.Log("Time is up");
                timerCoroutine = null;
                RefreshTimer_Send();
            }
            else 
            { 
                time += 10;
                RefreshTimer_Send();
                timerCoroutine = StartCoroutine(Timer()); 
            }
        }
        else
        {
            RefreshTimer_Send();
            timerCoroutine = StartCoroutine(Timer());
        }
    }

    private void RefreshTimer_Send()
    {
        object[] package = new object[] { time };

        PhotonNetwork.RaiseEvent(
            (byte)EventCodes.RefreshTimer,
            package,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            new SendOptions { Reliability = true }
        );
    }

    private void RefreshTimer_Receive(object[] data)
    {
        time = (float)data[0];

        if (time <= 0) EndGame();
        
        RefreshTimerUI();
        
    }


    // ------------- SCORE ------------- //
    [PunRPC]
    public void AddPlayer(string playerNickname)
    {
        Debug.LogError("New player: " + playerNickname);

        ScoreTable newPlayer = Instantiate(miniScorePrefab, miniScorePanel.transform);
        newPlayer.playerName = playerNickname;
        scoreList.Add(newPlayer);

        foreach (ScoreTable player in scoreList) 
        {
            photonView.RPC("UpdatePlayers", RpcTarget.Others, player.playerName);
        }

        //photonView.RPC("RefreshPlayerScoreUI", RpcTarget.Others);
    }

    [PunRPC]
    public void UpdatePlayers(string playerNickname)
    {
        bool inTheList = false;

        foreach (ScoreTable player in scoreList)
        {
            if (player.playerName == playerNickname) inTheList = true;
        }

        if (!inTheList)
        {
            //Debug.LogError("I haven't had " + playerNickname + " but added now!");
            AddPlayer(playerNickname);
        }
    }

    [PunRPC]
    public void ScoreEvent(string killerName, string deathPlayer)
    {
        Debug.LogError("Killer: " + killerName + " | | Dead: " + deathPlayer);

        if (killerName == "DeathWall")
        {
            foreach (ScoreTable playerScore in scoreList)
            {
                if (playerScore.playerName == deathPlayer)
                {
                    playerScore.AddSuicide();
                    break;
                }
            }
        }
        else
        {
            foreach (ScoreTable playerScore in scoreList)
            {
                if (playerScore.playerName == killerName) playerScore.AddKill();
                if (playerScore.playerName == deathPlayer) playerScore.AddDeath();
            }
        }

        //SortScore();
        RefreshPlayerScoreUI();
    }

    private void SortScore()
    {
        Debug.Log(" == Before Sort == ");
        foreach (ScoreTable playerScore in scoreList) { Debug.Log(playerScore.name + " - " + playerScore.score); }

        //scoreList.Sort((a, b) => a.score.CompareTo(b.score));
        //scoreList = scoreList.OrderBy(player => player.score).ToList();
        //scoreList.Sort(SortByScore);
        /*
        scoreList.Sort(delegate (ScoreTable x, ScoreTable y)
        {
            if (x.score == 0 && y.score == 0) return 0;
            else if (x.score == 0) return -1;
            else if (y.score == 0) return 1;
            else return x.score.CompareTo(y.score);
        });
        */
        /*
        List<TableItem> items = new List<TableItem>();
        for (int i = 0; i < items.Count; i++) { items.Add(new TableItem { score = scoreList[i].score }); }
        foreach (TableItem item in items) { Debug.Log(item.score); }


        int n = items.Count;
        for (int i = 0; i < n - 1; i++)
        {
            for (int j = 0; j < n - i - 1; j++)
            {
                if (items[j].score > items[j + 1].score)
                {
                    int temp = items[j].score;
                    items[j].score = items[j + 1].score;
                    items[j + 1].score = temp;
                }
            }
        }
        */
        //scoreList[0].score = 99;


        List<ScoreTable> newList = new List<ScoreTable>();

        for (int i = 0; i < scoreList.Count - 1; i++)
        {
            int biggest = scoreList[i].score;

            for (int j = 1; j <= scoreList.Count; j++)
            {
                if (scoreList[i + j].score > biggest) biggest = scoreList[i + j].score;
            }

            
        }


        Debug.Log(" == AFTER Sort == ");
        foreach (ScoreTable playerScore in scoreList) { Debug.Log(playerScore.name + " - " + playerScore.score); }

        //foreach (TableItem item in items) { Debug.Log(item.score); }
    }

    private void RefreshPlayerScoreUI()
    {
        //Debug.LogError("Refresing score UI");

        for (int i = 0; i < scoreList.Count; i++)
        {
            scoreList[i].transform.SetSiblingIndex(i);
        }
    }


}
