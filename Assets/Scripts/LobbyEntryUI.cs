using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;

public class LobbyEntryUI : MonoBehaviour
{
    public TMP_Text lobbyNameText;
    public Button joinButton;

    private Lobby lobby;

    public void Setup(Lobby lobby)
    {
        this.lobby = lobby;

        string hostName = "Unknown";
        if (lobby.Data != null && lobby.Data.ContainsKey("HostName"))
        {
            hostName = lobby.Data["HostName"].Value;
        }

        int currentPlayers = lobby.Players != null ? lobby.Players.Count : 0;
        int maxPlayers = lobby.MaxPlayers;

        lobbyNameText.text = $"{hostName}'s Lobby ({currentPlayers}/{maxPlayers})";

        joinButton.onClick.RemoveAllListeners();

        joinButton.onClick.AddListener(() =>
        {
            TMP_InputField usernameInput = GameObject.Find("UsernameInput")?.GetComponent<TMP_InputField>();

            if (usernameInput == null)
            {
                Debug.LogWarning("UsernameInput field not found!");
                return;
            }

            string username = usernameInput.text;

            if (string.IsNullOrEmpty(username))
            {
                Debug.LogWarning("Username is empty!");
                return;
            }

            RelayManager.Instance.localUsername = username;
            RelayManager.Instance.JoinRoom(username, lobby);
        });
    }
}
