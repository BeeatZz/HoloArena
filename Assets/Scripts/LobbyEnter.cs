using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Lobbies.Models;

public class LobbyEnter : MonoBehaviour
{
    public TMP_Text lobbyNameText;
    public Button joinButton;

    private Lobby lobby;

    // Called by RelayManager when creating the entry
    public void Setup(Lobby lobbyToDisplay)
    {
        lobby = lobbyToDisplay;
        if (lobbyNameText != null)
            lobbyNameText.text = $"{lobby.Name} ({lobby.Players.Count}/{lobby.MaxPlayers})";

        if (joinButton != null)
        {
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(OnJoinClicked);
        }
    }

    private void OnJoinClicked()
    {
        // Call RelayManager to join this lobby
        RelayManager.Instance.JoinRoom(lobby);
    }
}
