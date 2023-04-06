using UnityEngine;
using UnityEditor;
using Firebase;
using Firebase.Analytics;

public class FirebaseInit : MonoBehaviour
{
    [SerializeField] FirebaseAuthManager authManager;

    void Start()
    {
        Debug.Log("Starting to initialize Firebase");
        // Initialize Firebase Analytics
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);

            Debug.Log("Dependencies are done!");
            authManager.StartAuthManager();
        });
    }    
}
