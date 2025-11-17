using UnityEngine;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;

public class LobbyUI : MonoBehaviour
{
    public Transform lobbyListParent;
    public GameObject lobbyEntryPrefab;
    public TMP_InputField playerUsername;

    public async void RefreshList()
    {
        string userName = playerUsername.text;

        if (!await RelayManager.Instance.EnsureAuthentication())
        {
            Debug.LogWarning("Cannot refresh lobbies: not authenticated.");
            return;
        }

        List<Lobby> lobbies = await RelayManager.Instance.GetJoinableLobbies();

        // Clear old list
        foreach (Transform t in lobbyListParent)
            Destroy(t.gameObject);

        // Populate UI
        foreach (Lobby lobby in lobbies)
        {
            GameObject entryGO = Instantiate(lobbyEntryPrefab, lobbyListParent);
            LobbyEntryUI entryUI = entryGO.GetComponent<LobbyEntryUI>();
            if (entryUI != null)
            {
                entryUI.Setup(lobby, userName);
            }
        }
    }
}
