 using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;

public class FirebaseAuthManager : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] Canvas connectingUI;
    [SerializeField] Canvas MenuCanvas;
    [SerializeField] Canvas authCanvas;
    [SerializeField] GameObject loginUI;
    [SerializeField] GameObject registerUI;
    [SerializeField] GameObject messageObject;
    [SerializeField] TextMeshProUGUI messageText;

    [Header("Inputs")]
    [SerializeField] TMP_InputField loginEmailInput;
    [SerializeField] TMP_InputField loginPasswordInput;
    [SerializeField] TMP_InputField registerEmailInput;
    [SerializeField] TMP_InputField registerPasswordInput;
    [SerializeField] TMP_InputField registerPasswordRepeatInput;

    [Header("Other")]
    [SerializeField] DisplayMessage messageUI;

    private FirebaseAuth auth;
    private FirebaseFirestore firestore;
    private FirebaseUser user;

    enum SnapFailStatus
    {
        NotExist
    }

    private void Awake()
    {
        // TEST
        auth = FirebaseAuth.DefaultInstance;
        firestore = FirebaseFirestore.DefaultInstance;
    }

    public void StartAuthManager()
    {
        Debug.Log("Auth Manager has started");
        InitializeFirebase();
    }

    void InitializeFirebase()
    {
        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);
    }

    void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        Debug.Log("Auth State Changed!");
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;
            if (!signedIn && user != null)
            {
                Debug.Log("Signed out " + user.UserId);
            }
            user = auth.CurrentUser;
            if (signedIn)
            {
                Debug.Log("Signed in " + user.UserId);

                StartLoginTasks();
            }
        }
        else if (auth.CurrentUser == null)
        {
            Debug.Log("No user found! Opening the login UI");
            MenuCanvas.enabled = false;
            authCanvas.enabled = true;
            loginUI.SetActive(true);
            registerUI.SetActive(false);
            connectingUI.enabled = false;
        }
    }

    private async void StartLoginTasks()
    {
        connectingUI.enabled = true;

        BasicGameInfo gameInfo = null;
        PlayerInfo playerInfo = null;

        // Get game info
        var snapshot = await firestore.Document("gameInfo/basicInfo").GetSnapshotAsync();
        if (snapshot.Exists)
        {
            gameInfo = snapshot.ConvertTo<BasicGameInfo>();
        }
        else { SnapFail(snapshot, SnapFailStatus.NotExist); return; }

        Debug.Log("Current Week: " + gameInfo.currentWeek);
        FindObjectOfType<FirebaseDataManager>().gameInfo = gameInfo;    // Send game info to the dm


        snapshot = await firestore.Document("users/" + auth.CurrentUser.UserId).GetSnapshotAsync();
        if (snapshot.Exists)
        {
            playerInfo = snapshot.ConvertTo<PlayerInfo>();
        }
        else 
        { 
            Debug.Log("We don't have such user! But now, adding!");

            // Create player
            playerInfo = new PlayerInfo();

            // Create egg record for this week
            Dictionary<string, object> eggs = new Dictionary<string, object>();
            eggs[gameInfo.currentWeek] = 0;
            playerInfo.eggs = eggs;

            await firestore.Document("users/" + auth.CurrentUser.UserId).SetAsync(playerInfo);

            // Restart Procces
            Debug.Log("Restarting the login tasks!");
            StartLoginTasks();
        }



        if (playerInfo.eggs == null)
        {
            Debug.Log("No egg at all!");

            // Then add this week
            Dictionary<string, object> eggs = new Dictionary<string, object>();
            eggs[gameInfo.currentWeek] = 0;
            playerInfo.eggs = eggs;

            await firestore.Document("users/" + auth.CurrentUser.UserId).SetAsync(playerInfo, SetOptions.MergeFields("eggs"));

            // Restart Procces
            Debug.Log("Restarting the login tasks!");
            StartLoginTasks();
        }
        else Debug.Log("We have eggs");

        if (playerInfo.eggs.ContainsKey(gameInfo.currentWeek))
        {
            Debug.Log("Current week exist!");
            // Start game
            Debug.Log("AWESOME, Starting the game!!!");
            StartGame();
        }
        else
        {
            Debug.Log("This week is not here!");

            // Then add this week
            playerInfo.eggs[gameInfo.currentWeek] = 0;

            await firestore.Document("users/" + auth.CurrentUser.UserId).SetAsync(playerInfo, SetOptions.MergeFields("eggs"));

            // Restart Procces
            Debug.Log("Restarting the login tasks!");
            StartLoginTasks();
        } 
    }

    private void StartGame()
    {
        loginUI.SetActive(false);
        authCanvas.enabled = false;
        MenuCanvas.enabled = true;

        FindObjectOfType<FirebaseDataManager>().OnLogin();
    }

    private void SnapFail(DocumentSnapshot snap, SnapFailStatus status)
    {
        switch (status) {
            case SnapFailStatus.NotExist:
                Debug.LogError(String.Format("Document {0} does not exist!", snap.Id));
                break;
        }
    }

    void OnDestroy()
    {
        auth.StateChanged -= AuthStateChanged;
        auth = null;
    }

    // Navigation Buttons
    public void Btn_OpenRegisterUI()
    {
        registerUI.SetActive(true);
        loginUI.SetActive(false);
    }
    public void Btn_BackToLoginUI()
    {
        registerUI.SetActive(false);
        loginUI.SetActive(true);
    }

    // Execution Buttons
    public void Btn_Register()
    {
        if (registerPasswordInput.text != registerPasswordRepeatInput.text)
            messageUI.Display("Passwords are not same!", 3f);
        else
            StartCoroutine(RegisterUser());
    }
    public void Btn_Login()
    {
        StartCoroutine(LoginUser());
    }

    IEnumerator RegisterUser()
    {
        var task = auth.CreateUserWithEmailAndPasswordAsync(registerEmailInput.text, registerPasswordInput.text);

        yield return new WaitUntil(predicate: () => task.IsCompleted);

        if (task.Exception != null)
        {
            if (task.IsCanceled)
            {
                Debug.LogError("CreateUserWithEmailAndPasswordAsync was canceled.");
                yield break;
            }

            //If there are errors handle them
            Debug.LogWarning(message: $"Failed to register task with {task.Exception}");
            FirebaseException firebaseEx = task.Exception.GetBaseException() as FirebaseException;
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

            string message = "Register Failed!";
            switch (errorCode)
            {
                case AuthError.MissingEmail:
                    message = "Missing Email";
                    break;
                case AuthError.MissingPassword:
                    message = "Missing Password";
                    break;
                case AuthError.WeakPassword:
                    message = "Weak Password";
                    break;
                case AuthError.EmailAlreadyInUse:
                    message = "Email Already In Use";
                    break;
            }

            messageUI.Display(message, 3f);
        }
        else
        {
            // Firebase user has been created.
            FirebaseUser newUser = task.Result;
            Debug.LogFormat("Firebase user created successfully: {0} ({1})",
                newUser.DisplayName, newUser.UserId);

            registerUI.SetActive(false);
            loginUI.SetActive(true);
        }
                
    }

    IEnumerator LoginUser()
    {
        var task = auth.SignInWithEmailAndPasswordAsync(loginEmailInput.text, loginPasswordInput.text);

        yield return new WaitUntil(predicate: () => task.IsCompleted);

        if (task.IsCanceled)
        {
            Debug.LogError("SignInWithEmailAndPasswordAsync was canceled.");
            yield break;
        }

        if (task.IsFaulted)
        {
            Debug.LogError("SignInWithEmailAndPasswordAsync encountered an error: " + task.Exception);

            FirebaseException firebaseEx = task.Exception.GetBaseException() as FirebaseException;
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

            string message = "Login Failed!";
            switch (errorCode)
            {
                case AuthError.MissingEmail:
                    message = "Missing Email";
                    break;
                case AuthError.MissingPassword:
                    message = "Missing Password";
                    break;
                case AuthError.WrongPassword:
                    message = "Wrong Password";
                    break;
                case AuthError.InvalidEmail:
                    message = "Invalid Email";
                    break;
                case AuthError.UserNotFound:
                    message = "Account does not exist";
                    break;
            }

            messageUI.Display(message, 3f);
            yield break;
        }

        FirebaseUser newUser = task.Result;
        Debug.LogFormat("User signed in successfully: {0} ({1})", newUser.DisplayName, newUser.UserId);

        loginUI.SetActive(false);
        authCanvas.enabled = false;
        MenuCanvas.enabled = true;
    }

    public void LogoutUser()
    {
        if (auth.CurrentUser != null)
        {
            auth.SignOut();
            Debug.Log("User logged out !");
            Application.Quit();
        }
    }


}
