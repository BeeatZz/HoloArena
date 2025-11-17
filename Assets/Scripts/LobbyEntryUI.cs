using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;

public class LobbyEntryUI : MonoBehaviour
{
    public TMP_Text lobbyNameText; // assign the Text inside the prefab
    public Button joinButton;      // assign the Button inside the prefab

    private Lobby lobby;

    public void Setup(Lobby lobby, string username)
    {
        this.lobby = lobby;

        // Update the text inside the prefab
        lobbyNameText.text = lobby.Name;

        // Make sure previous listeners are cleared to avoid duplicates
        joinButton.onClick.RemoveAllListeners();

        // Assign the join action
        joinButton.onClick.AddListener(() =>
        {
            RelayManager.Instance.JoinRoom(lobby);
        });
    }
}
