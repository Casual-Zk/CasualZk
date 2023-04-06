using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using TMPro;

public class FirebaseAuthManager : MonoBehaviour
{
    [SerializeField] Canvas connectingUI;
    [SerializeField] Canvas MenuCanvas;
    [SerializeField] Canvas authCanvas;
    [SerializeField] GameObject loginUI;
    [SerializeField] GameObject registerUI;
    [SerializeField] TMP_InputField loginEmailInput;
    [SerializeField] TMP_InputField loginPasswordInput;
    [SerializeField] TMP_InputField registerEmailInput;
    [SerializeField] TMP_InputField registerPasswordInput;

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
        // Check if the inputs are valid

        RegisterUser();
    }

    void RegisterUser()
    {
        auth.CreateUserWithEmailAndPasswordAsync(registerEmailInput.text, registerPasswordInput.text).
            ContinueWith(task => {
                if (task.IsCanceled)
                {
                    Debug.LogError("CreateUserWithEmailAndPasswordAsync was canceled.");
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError("CreateUserWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                    return;
                }

                // Firebase user has been created.
                FirebaseUser newUser = task.Result;
                Debug.LogFormat("Firebase user created successfully: {0} ({1})",
                    newUser.DisplayName, newUser.UserId);

                registerUI.SetActive(false);
                loginUI.SetActive(true);
            });
    }

    public void LoginUser()
    {
        auth.SignInWithEmailAndPasswordAsync(loginEmailInput.text, loginPasswordInput.text).
            ContinueWith(task => {
                if (task.IsCanceled)
                {
                    Debug.LogError("SignInWithEmailAndPasswordAsync was canceled.");
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError("SignInWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                    return;
                }

                FirebaseUser newUser = task.Result;
                Debug.LogFormat("User signed in successfully: {0} ({1})",
                    newUser.DisplayName, newUser.UserId);
                                
                loginUI.SetActive(false);
                authCanvas.enabled = false;
                MenuCanvas.enabled = true;
            });
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
