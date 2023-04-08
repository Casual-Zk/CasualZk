using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
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
    private FirebaseUser user;

    public void StartAuthManager()
    {
        Debug.Log("Auth Manager has started");
        InitializeFirebase();
    }

    void InitializeFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);
    }

    void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        connectingUI.enabled = false;
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

                loginUI.SetActive(false);
                authCanvas.enabled = false;
                MenuCanvas.enabled = true;
                FindObjectOfType<FirebaseDataManager>().OnLogin();
            }
        }
        else if (auth.CurrentUser == null)
        {
            Debug.Log("No user found! Opening the login UI");
            MenuCanvas.enabled = false;
            authCanvas.enabled = true;
            loginUI.SetActive(true);
            registerUI.SetActive(false);
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
