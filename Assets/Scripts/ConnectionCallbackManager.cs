using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            NetworkManager.Singleton.OnClientStarted += OnClientStartedMethod;
            NetworkManager.Singleton.OnClientStopped += OnClientStoppedMethod;
        }
        else
        {
            StartCoroutine(WaitForNetworkManager());
        }
    }
    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientStarted -= OnClientStartedMethod;
            NetworkManager.Singleton.OnClientStopped -= OnClientStoppedMethod;
        }
    }

    private IEnumerator WaitForNetworkManager()
    {
        while (NetworkManager.Singleton == null)
            yield return null;

        NetworkManager.Singleton.OnClientStarted += OnClientStartedMethod;
        NetworkManager.Singleton.OnClientStopped += OnClientStoppedMethod;
    }


    private void OnClientStoppedMethod(bool obj)
    {
        informationalText.text = "Disconnected";
        SceneManager.LoadScene("MainMenu");
    }

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

}
