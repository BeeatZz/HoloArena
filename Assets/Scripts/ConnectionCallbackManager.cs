using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Lobbies; 

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

  

    private void OnClientStartedMethod()
    {
        //informationalText.text = "Connected as " + (NetworkManager.Singleton.IsHost ? "Host" : "Client");

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


    private async void OnClientDisconnectCallback(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {

            if (RelayManager.Instance != null)
                await RelayManager.Instance.LeaveLobby();

            CleanupLobbyPlayerUI();

            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.Shutdown();

            SceneManager.LoadScene("MainMenu");
        }
    }

    private async void OnClientStoppedMethod(bool wasHost)
    {

        if (RelayManager.Instance != null)
            await RelayManager.Instance.LeaveLobby();

        CleanupLobbyPlayerUI();

        SceneManager.LoadScene("MainMenu");
    }

   
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
