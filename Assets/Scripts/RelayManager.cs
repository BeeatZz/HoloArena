using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

public class RelayManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField usernameInput;
    public TMP_Text roomInfoText;
    public Transform lobbyListParent;
    public GameObject lobbyEntryPrefab;

    private static RelayManager instance;
    public static RelayManager Instance => instance;

    private Lobby currentLobby;

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

    private async void Start()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        roomInfoText.text = "- -";
    }

    /// <summary>
    /// Host a new lobby (Max 2 players by default)
    /// </summary>
    public async void CreateRoom(int maxPlayers = 2)
    {
        string userName = usernameInput.text;
        if (string.IsNullOrWhiteSpace(userName))
        {
            Debug.LogWarning("Username is empty");
            return;
        }

        try
        {
            // 1. Create lobby in Multiplayer Services
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false, // public lobby
                Data = new Dictionary<string, DataObject>
                {
                    { "HostName", new DataObject(DataObject.VisibilityOptions.Public, userName) }
                }
            };
            currentLobby = await LobbyService.Instance.CreateLobbyAsync("Lobby_" + Random.Range(1000, 9999), maxPlayers, options);

            // 2. Create Relay allocation for host
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);

            // 3. Provide Relay server data to Netcode Transport
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            // 4. Start Netcode host
            bool started = NetworkManager.Singleton.StartHost();
            if (started)
            {
                roomInfoText.text = $"Hosting {currentLobby.Name} ({currentLobby.Players.Count}/{currentLobby.MaxPlayers})";
                Debug.Log($"Lobby hosted: {currentLobby.Name}");
            }
            else
            {
                Debug.LogError("Failed to start host");
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("LobbyServiceException: " + e);
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("RelayServiceException: " + e);
        }
    }

    /// <summary>
    /// Refresh the lobby browser list
    /// </summary>
    public async void RefreshLobbies()
    {
        try
        {
            QueryLobbiesOptions queryOptions = new QueryLobbiesOptions
            {
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                }
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(queryOptions);

            // Clear previous list
            foreach (Transform child in lobbyListParent)
                Destroy(child.gameObject);

            // Populate UI
            foreach (Lobby lobby in response.Results)
            {
                GameObject entry = Instantiate(lobbyEntryPrefab, lobbyListParent);
                TMP_Text txt = entry.GetComponentInChildren<TMP_Text>();
                if (txt != null)
                    txt.text = $"{lobby.Name} ({lobby.Players.Count}/{lobby.MaxPlayers})";

                Button btn = entry.GetComponentInChildren<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => JoinRoom(lobby));
            }

            Debug.Log($"Found {response.Results.Count} joinable lobbies");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("LobbyServiceException: " + e);
        }
    }

    /// <summary>
    /// Join a selected lobby
    /// </summary>
    public async void JoinRoom(Lobby lobbyToJoin)
    {
        string userName = usernameInput.text;
        if (string.IsNullOrWhiteSpace(userName))
        {
            Debug.LogWarning("Username is empty");
            return;
        }

        try
        {
            // 1. Join the lobby
            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyToJoin.Id);

            // 2. Update player data (username)
            var updatePlayerOptions = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, userName) }
                }
            };
            await LobbyService.Instance.UpdatePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId, updatePlayerOptions);

            // 3. Retrieve Relay allocation info from host
            // Here we assume the host has already created a relay allocation and published it via Lobby data
            // For now, in simple 2-player setup, you can create a new relay join allocation by lobby ID
            JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(currentLobby.Id); // adjust if needed

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                joinAlloc.RelayServer.IpV4,
                (ushort)joinAlloc.RelayServer.Port,
                joinAlloc.AllocationIdBytes,
                joinAlloc.Key,
                joinAlloc.ConnectionData,
                joinAlloc.HostConnectionData
            );

            // 4. Start Netcode client
            if (!NetworkManager.Singleton.StartClient())
                Debug.LogError("Failed to start client");
            else
                Debug.Log($"Joined lobby: {currentLobby.Name}");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("LobbyServiceException: " + e);
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("RelayServiceException: " + e);
        }
    }
}
