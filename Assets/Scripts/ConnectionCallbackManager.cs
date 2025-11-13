using Unity.Netcode;
using UnityEngine;
using TMPro;
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
        NetworkManager.Singleton.OnClientStarted += OnClientStartedMethod;
        NetworkManager.Singleton.OnClientStopped += OnClientStoppedMethod;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientStarted -= OnClientStartedMethod;
            NetworkManager.Singleton.OnClientStopped -= OnClientStoppedMethod;
        }
    }

    private void OnClientStoppedMethod(bool obj)
    {
        informationalText.text = "Disconnected";
        SceneManager.LoadScene("MainMenu");
    }

    private void OnClientStartedMethod()
    {
        informationalText.text = "Connected as " + (NetworkManager.Singleton.IsHost ? "Host" : "Client");

        SceneManager.LoadScene("LobbyScene", LoadSceneMode.Single);
    }
}
