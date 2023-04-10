using UnityEngine;
using UnityEditor;
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;

public class FirebaseInit : MonoBehaviour
{
    [SerializeField] FirebaseAuthManager authManager;

    void Start()
    {
        Debug.Log("Starting to initialize Firebase");
        // Initialize Firebase Analytics
        /*
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);

            Debug.Log("Dependencies are done!");
            authManager.StartAuthManager();
        });
        */

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                Debug.Log($"Dependency Status: {task.Result}");
                authManager.StartAuthManager();
            }
            else
            {
                Debug.LogError(
                  "Could not resolve all Firebase dependencies: " + task.Result);
            }
        });
    }    
}
