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

            if (UpdateNeeded(gameInfo.appVersion))
                authManager.DisplayAppUpdateUI();
            else
                authManager.StartAuthManager();
        }
        else { Debug.LogError("Error while getting basic Info from DB within FirebaseInit.cs !!"); }
    }
    private bool UpdateNeeded(string databaseVersion)
    {
        if (databaseVersion == Application.version) return false;

        string[] dbVersionNumbers = databaseVersion.Split(".");
        string[] localVersionNumbers = Application.version.Split(".");

        for (int i = 0; i < dbVersionNumbers.Length; i++)
        {
            if (int.Parse(localVersionNumbers[i]) > int.Parse(dbVersionNumbers[i])) return false;
        }

        return true;
    }
}
