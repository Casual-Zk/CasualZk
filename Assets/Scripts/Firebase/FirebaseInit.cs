using UnityEngine;
using UnityEditor;
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;
using Firebase.Firestore;

public class FirebaseInit : MonoBehaviour
{
    [SerializeField] FirebaseAuthManager authManager;

    private FirebaseFirestore firestore;

    private void Awake()
    {
        firestore = FirebaseFirestore.DefaultInstance;
    }

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
                StartLoginTasks();
            }
            else
            {
                Debug.LogError(
                  "Could not resolve all Firebase dependencies: " + task.Result);
            }
        });
    }

    private async void StartLoginTasks()
    {
        var snapshot = await firestore.Document("gameInfo/basicInfo").GetSnapshotAsync();
        if (snapshot.Exists)
        {
            BasicGameInfo gameInfo = snapshot.ConvertTo<BasicGameInfo>();

            if (gameInfo.appVersion != Application.version)
                authManager.DisplayAppUpdateUI();
            else
                authManager.StartAuthManager();
        }
        else { Debug.LogError("Error while getting basic Info from DB within FirebaseInit.cs !!"); }
    }
}
