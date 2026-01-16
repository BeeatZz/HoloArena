using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Lobbies;  // Needed for lobby cleanup

public class ConnectionCallbackManager : MonoBehaviour
{
    public TMP_Text informationalText;

    private static ConnectionCallbackManager instance;
    public static ConnectionCallbackManager Instance => instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            SubscribeCallbacks();
        }
        else
        {
            StartCoroutine(WaitForNetworkManager());
        }
    }

    private void OnDisable()
    {
        UnsubscribeCallbacks();
    }

    private void SubscribeCallbacks()
    {
        NetworkManager.Singleton.OnClientStarted += OnClientStartedMethod;
        NetworkManager.Singleton.OnClientStopped += OnClientStoppedMethod;
        //NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
    }

    private void UnsubscribeCallbacks()
    {
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientStarted -= OnClientStartedMethod;
        NetworkManager.Singleton.OnClientStopped -= OnClientStoppedMethod;
        //NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
    }

    private IEnumerator WaitForNetworkManager()
    {
        while (NetworkManager.Singleton == null)
            yield return null;

        SubscribeCallbacks();
    }

    // ---------------------------------------------------------------------
    //  On Client Start (host or client)
    // ---------------------------------------------------------------------

    private void OnClientStartedMethod()
    {
        informationalText.text = "Connected as " + (NetworkManager.Singleton.IsHost ? "Host" : "Client");

        if (NetworkManager.Singleton.IsHost)
        {
            StartCoroutine(LoadLobbySceneNextFrame());
        }
    }

    private IEnumerator LoadLobbySceneNextFrame()
    {
        yield return null;

        Debug.Log("Loading LobbyScene via NetworkManager.SceneManager");
        NetworkManager.Singleton.SceneManager.LoadScene("LobbyScene", LoadSceneMode.Single);
    }

    // ---------------------------------------------------------------------
    //  Spawn Lobby Player for clients entering lobby
    // ---------------------------------------------------------------------

    

   

    // ---------------------------------------------------------------------
    //  Cleanup on Disconnect (client or host)
    // ---------------------------------------------------------------------

    private async void OnClientDisconnectCallback(ulong clientId)
    {
        // If *this* client disconnected, perform cleanup
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            informationalText.text = "Disconnected";

            // Clean up Lobby (Unity Lobby API)
            if (RelayManager.Instance != null)
                await RelayManager.Instance.LeaveLobby();

            // Destroy all LobbyPlayer objects left over
            CleanupLobbyPlayerUI();

            // Shutdown netcode safely
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.Shutdown();

            // Return to main menu
            SceneManager.LoadScene("MainMenu");
        }
    }

    // This is called by NetworkManager when host shuts down
    private async void OnClientStoppedMethod(bool wasHost)
    {
        informationalText.text = "Disconnected";

        // Lobby cleanup
        if (RelayManager.Instance != null)
            await RelayManager.Instance.LeaveLobby();

        CleanupLobbyPlayerUI();

        SceneManager.LoadScene("MainMenu");
    }

    // ---------------------------------------------------------------------
    //  UI Cleanup: removes leftover lobby UI elements
    // ---------------------------------------------------------------------

    private void CleanupLobbyPlayerUI()
    {
        var players = GameObject.FindObjectsOfType<LobbyPlayer>();
        foreach (var p in players)
        {
            Destroy(p.gameObject);
        }

        var teamUI = FindObjectOfType<TeamSelectionUI>();
        if (teamUI != null)
            teamUI.RefreshUI();
    }
}
