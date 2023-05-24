using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using TMPro;

public class FirebaseAuthManager : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] GameObject connectingUI;
    [SerializeField] GameObject MenuCanvas;
    [SerializeField] GameObject authCanvas;
    [SerializeField] GameObject loginWarningUI;
    [SerializeField] GameObject emailVerificationTextUI;
    [SerializeField] GameObject linkWalletTextUI;
    [SerializeField] GameObject resetUI;
    [SerializeField] GameObject loginUI;
    [SerializeField] GameObject registerUI;
    [SerializeField] GameObject appUpdateUI;
    [SerializeField] GameObject messageObject;
    [SerializeField] TextMeshProUGUI messageText;
    [SerializeField] Button sendVerificationButton;

    [Header("Inputs")]
    [SerializeField] TMP_InputField loginEmailInput;
    [SerializeField] TMP_InputField loginPasswordInput;
    [SerializeField] TMP_InputField registerEmailInput;
    [SerializeField] TMP_InputField registerPasswordInput;
    [SerializeField] TMP_InputField registerPasswordRepeatInput;
    [SerializeField] TMP_InputField resetEmailInput;

    [Header("Other")]
    [SerializeField] DisplayMessage messageUI;

    private FirebaseAuth auth;
    private FirebaseFirestore firestore;
    private FirebaseUser user;

    private int testerClicks;

    /*
        Default Canvas and Object status (only closed ones)
        
        - Message Object: false
        - Menu Manager > Auth Canvas: False, Profile Canvas: False
        - Menu Manager > Main Manu Canvas > Set Nickname UI : False
        - Menu Manager > Auth Canvas > Register UI: False, Connect Wallet UI, False
        - Menu Manager > Profile Canvas > Inventory UI: False, Lottery UI: False, Set Nickname UI: False

        - Match Manager > In-Game Canvas: False, End Game Vancas: False
        - Match Manager > In-game canvas > Game Over UI
        - Match Manager > End Game Canvas > Egg
        - Map_0: False
        - Room Manager > Canvas > Connecting Background: False


        ## TO-DO ##
        - Send Reset password but email should be verified! Max 5 reset request per day, store locally
     
     */

    enum SnapFailStatus
    {
        NotExist
    }

    private void Awake()
    {
        // TEST
        auth = FirebaseAuth.DefaultInstance;
        firestore = FirebaseFirestore.DefaultInstance;
        Debug.LogWarning("Application Version : " + Application.version);
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

    public void DisplayAppUpdateUI()
    {
        Debug.LogError("App version is old, displaying update UI !!");
        authCanvas.SetActive(true);
        loginUI.SetActive(false);
        MenuCanvas.SetActive(false);
        connectingUI.SetActive(false);
        appUpdateUI.SetActive(true);

        StartCoroutine(QuitAppOnDelay());
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
            MenuCanvas.SetActive(false);
            authCanvas.SetActive(true);
            loginUI.SetActive(true);
            registerUI.SetActive(false);
            connectingUI.SetActive(false);
        }
    }

    private async void StartLoginTasks()
    {
        connectingUI.SetActive(true);

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

        bool emailVerified = user.IsEmailVerified;
        bool walletLinked = false;

        // If this is first login, send email verification now and
        if (!PlayerPrefs.HasKey("lastLogin") && !emailVerified) StartCoroutine(SendEmailVerification());

        // Save the login time
        int nowUnix = 0;
        string timeString = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        int.TryParse(timeString, out nowUnix);
        PlayerPrefs.SetInt("lastLogin", nowUnix);

        // Get user data
        snapshot = await firestore.Document("users/" + auth.CurrentUser.UserId).GetSnapshotAsync();
        if (snapshot.Exists)
        {
            // If data exist, get it's info and check if wallet is linked or not
            playerInfo = snapshot.ConvertTo<PlayerInfo>();

            if (playerInfo.walletAddress == null)
            {
                Debug.Log("User doesn't have any wallet linked!");
            }
            else { walletLinked = true; }

            if (playerInfo.isTester) { emailVerified = true; }
        }
        // If data doesn't exist, no such user then!
        else
        {
            Debug.Log("We don't have such user data! Request login via Web and connect wallet!");
        }

        // If email not verified or wallet not linked then show warning and stop further execution
        if (!emailVerified || !walletLinked) { LoginWarning(emailVerified, walletLinked); return; }

        if (playerInfo.eggs == null)
        {
            Debug.Log("No egg at all!");

            // Then add this week
            Dictionary<string, object> eggs = new Dictionary<string, object>();
            eggs[gameInfo.currentWeek.ToString()] = 0;
            playerInfo.eggs = eggs;

            await firestore.Document("users/" + auth.CurrentUser.UserId).SetAsync(playerInfo, SetOptions.MergeFields("eggs"));

            // Restart Procces
            Debug.Log("Restarting the login tasks!");
            StartLoginTasks();
        }
        else Debug.Log("We have eggs");

        if (playerInfo.eggs.ContainsKey(gameInfo.currentWeek.ToString()))
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
            playerInfo.eggs[gameInfo.currentWeek.ToString()] = 0;

            await firestore.Document("users/" + auth.CurrentUser.UserId).SetAsync(playerInfo, SetOptions.MergeFields("eggs"));

            // Restart Procces
            Debug.Log("Restarting the login tasks!");
            StartLoginTasks();
        } 
    }

    private void LoginWarning(bool emailVerification, bool walletLink)
    {
        Debug.Log("Log warning: Email:" + emailVerification + "  Wallet: " + walletLink);

        connectingUI.SetActive(false);
        authCanvas.SetActive(true);
        loginUI.SetActive(false);
        loginWarningUI.SetActive(true);

        // Close the obtained stuff
        if (emailVerification) emailVerificationTextUI.SetActive(false);
        if (walletLink) linkWalletTextUI.SetActive(false);
    }

    public async void GoogleTesterButton()
    {
        if (testerClicks > 8)
        {
            // Give items and wallet
            PlayerInfo playerInfo = new PlayerInfo();

            Dictionary<string, object> eggs = new Dictionary<string, object>();
            eggs[FindObjectOfType<FirebaseDataManager>().gameInfo.currentWeek.ToString()] = 0;
            playerInfo.eggs = eggs;

            playerInfo.walletAddress = "0xc6b32E450FB3A70BD8a5EC12D879292BF92F2944";
            playerInfo.isTester = true;
            playerInfo.game_12_gauge = 999;
            playerInfo.game_9mm = 999;
            playerInfo.game_5_56mm = 999;
            playerInfo.game_7_62mm = 999;

            await firestore.Document("users/" + auth.CurrentUser.UserId).SetAsync(playerInfo);

            // Restart Procces
            Debug.Log("Restarting the login tasks!");
            StartLoginTasks();
        }
        else testerClicks++;
    }

    private void StartGame()
    {
        loginUI.SetActive(false);
        authCanvas.SetActive(false);
        MenuCanvas.SetActive(true);

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
    public void Btn_OpenResetUI()
    {
        resetUI.SetActive(true);
        loginUI.SetActive(false);
    }
    public void Btn_BackToLoginUIFromReset()
    {
        resetUI.SetActive(false);
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
    public void Btn_ResetPassword()
    {
        int nowUnix = 0;
        string timeString = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        int.TryParse(timeString, out nowUnix);

        int reqAmount = 0;

        if (PlayerPrefs.HasKey("passwordRequestAmount")) reqAmount = PlayerPrefs.GetInt("passwordRequestAmount");

        // if no request has been made till now, then create record
        if (reqAmount == 0) { ResetPasswordResTime(nowUnix); }

        // else if it exist but 1-week passed passed, then reset the record
        else if ((nowUnix - PlayerPrefs.GetInt("passwordRequestTime")) > 604800) { ResetPasswordResTime(nowUnix); }

        // else if 24 not passed AND 5 request not exceeded, then send new one
        else if (reqAmount <= 5) 
        {
            PlayerPrefs.SetInt("passwordRequestAmount", reqAmount + 1);
            StartCoroutine(ResetPassword()); 
        }

        // if it exceed 5 request in the past 24h, then display error message
        else { messageUI.Display("You can not request more then 5 password reset in a week! Please try later.", 5f); }
    }
    public void Btn_SendVerification()
    {
        StartCoroutine(SendEmailVerification());
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

            string message;
            switch (errorCode)
            {
                case AuthError.InvalidEmail:
                    message = "Invalid Email";
                    break;
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
                default:
                    message = "Register Failed! Error code: " + errorCode;
                    break;
            }

            messageUI.Display(message, 3f);
        }
        else
        {
            // Firebase user has been created.
            FirebaseUser newUser = task.Result.User;
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

            string message;
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
                default:
                    message = "Login Failed! Error code: " + errorCode;
                    break;
            }

            messageUI.Display(message, 3f);
            yield break;
        }

        FirebaseUser newUser = task.Result.User;
        Debug.LogFormat("User signed in successfully: {0} ({1})", newUser.DisplayName, newUser.UserId);

        loginUI.SetActive(false);
        authCanvas.SetActive(false);
        MenuCanvas.SetActive(true);
    }
    IEnumerator SendEmailVerification()
    {
        if (user == null) yield return null;

        var task = user.SendEmailVerificationAsync();

        yield return new WaitUntil(predicate: () => task.IsCompleted);

        if (task.Exception != null)
        {
            if (task.IsCanceled)
            {
                Debug.LogError("Sending email verification was canceled.");
                yield break;
            }

            //If there are errors handle them
            Debug.LogWarning(message: $"Failed to send email verification! Exception: {task.Exception}");
            FirebaseException firebaseEx = task.Exception.GetBaseException() as FirebaseException;
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

            string message;
            switch (errorCode)
            {
                case AuthError.TooManyRequests:
                    message = "Sending email verification failed! Too many request!";
                    break;
                case AuthError.InvalidEmail:
                    message = "Sending email verification failed! Invalid email!";
                    break;
                default:
                    message = "Sending email verification failed! Please try again.! Error code: " + errorCode;
                    break;
            }

            messageUI.Display(message, 3f);
        }
        else
        {
            Debug.Log("Sent email verification!");
            messageUI.Display("A verification email has been sent! Please log in again after verifying your email!", 8f);
            StartCoroutine(LogoutOnDelay());
        }
    }
    IEnumerator ResetPassword()
    {
        var task = auth.SendPasswordResetEmailAsync(resetEmailInput.text);

        yield return new WaitUntil(predicate: () => task.IsCompleted);

        if (task.Exception != null)
        {
            if (task.IsCanceled)
            {
                Debug.LogError("Resetting password was canceled.");
                yield break;
            }

            //If there are errors handle them
            Debug.LogWarning(message: $"Failed to send email verification! Exception: {task.Exception}");
            FirebaseException firebaseEx = task.Exception.GetBaseException() as FirebaseException;
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

            string message = "Password reset failed! " + errorCode;

            messageUI.Display(message, 3f);
        }
        else
        {
            Debug.Log("Sent reset password email!");
            messageUI.Display("A email to reset password has been sent! Please check your email!", 3f);
            Btn_BackToLoginUIFromReset();
        }
    }

    // For force-logout user if the verification is not complete
    IEnumerator LogoutOnDelay()
    {
        yield return new WaitForSeconds(10);
        auth.SignOut();
        loginUI.SetActive(true);
        loginWarningUI.SetActive(false);
    }
    IEnumerator QuitAppOnDelay()
    {
        yield return new WaitForSeconds(10);
        Debug.Log("Closing app!");
        Application.Quit();
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

    // Others
    private void ResetPasswordResTime(int now)
    {
        PlayerPrefs.SetInt("passwordRequestTime", now);
        PlayerPrefs.SetInt("passwordRequestAmount", 1);

        StartCoroutine(ResetPassword());
        return;
    }


}
