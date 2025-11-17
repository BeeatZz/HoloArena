using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
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
    private static RelayManager instance;
    public static RelayManager Instance => instance;

    private Lobby currentLobby;
    private bool isInitialized = false;
    private string playerProfileId;

    private async void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            await InitializeServicesAndAuth();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Initialize Unity Services and automatically sign in with a random profile.
    /// </summary>
    private async Task InitializeServicesAndAuth()
    {
        if (!isInitialized)
        {
            await UnityServices.InitializeAsync();
            isInitialized = true;
        }

        await SignInWithRandomProfile();
    }

    /// <summary>
    /// Signs in with a random profile ID. Safe for multiple local editor instances.
    /// </summary>
    public async Task SignInWithRandomProfile()
    {
        // Generate a random profile
        playerProfileId = "Player_" + Random.Range(1000, 9999);
        AuthenticationService.Instance.SwitchProfile(playerProfileId);

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Signed in as: " + playerProfileId);
        }
        else
        {
            Debug.Log("Already signed in as: " + playerProfileId);
        }
    }

    /// <summary>
    /// Ensure the player is authenticated. If not, automatically signs in.
    /// </summary>
    public async Task<bool> EnsureAuthentication()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await SignInWithRandomProfile();
        }

        return AuthenticationService.Instance.IsSignedIn;
    }

    #region Room Creation & Joining

    public async Task CreateRoom(int maxPlayers = 4)
    {
        if (!await EnsureAuthentication())
            return;

        // Create Relay Allocation
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        Debug.Log("Relay Join Code: " + joinCode);

        // Setup Unity Transport for Relay
        SetupRelayTransport(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData
        );

        if (!NetworkManager.Singleton.StartHost())
        {
            Debug.LogError("Failed to start host");
            return;
        }

        // Create Lobby
        var options = new CreateLobbyOptions()
        {
            IsPrivate = false,
            Data = new Dictionary<string, DataObject>()
            {
                { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, joinCode) },
                { "HostName", new DataObject(DataObject.VisibilityOptions.Public, playerProfileId) }
            }
        };

        currentLobby = await LobbyService.Instance.CreateLobbyAsync(playerProfileId + "'s Lobby", maxPlayers, options);

        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    }

    public async Task JoinLobbyByCode(string roomCode)
    {
        if (!await EnsureAuthentication())
            return;

        Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(roomCode);

        if (!joinedLobby.Data.TryGetValue("RelayJoinCode", out var joinCodeObj))
        {
            Debug.LogError("RelayJoinCode missing!");
            return;
        }

        await JoinRelay(joinCodeObj.Value);
        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    }

    public async Task JoinRoom(Lobby targetLobby)
    {
        if (!await EnsureAuthentication())
            return;

        Lobby joined = await LobbyService.Instance.JoinLobbyByIdAsync(targetLobby.Id);

        if (!joined.Data.TryGetValue("RelayJoinCode", out var joinCodeObj))
        {
            Debug.LogError("RelayJoinCode missing!");
            return;
        }

        await JoinRelay(joinCodeObj.Value);
        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    }

    private async Task JoinRelay(string relayJoinCode)
    {
        JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);

        SetupRelayTransport(
            joinAlloc.RelayServer.IpV4,
            (ushort)joinAlloc.RelayServer.Port,
            joinAlloc.AllocationIdBytes,
            joinAlloc.Key,
            joinAlloc.ConnectionData,
            joinAlloc.HostConnectionData
        );

        if (!NetworkManager.Singleton.StartClient())
        {
            Debug.LogError("Failed to start client");
        }
    }

    #endregion

    #region Lobby Query

    public async Task<List<Lobby>> GetJoinableLobbies()
    {
        if (!await EnsureAuthentication())
        {
            Debug.LogWarning("Cannot query lobbies: not authenticated.");
            return new List<Lobby>();
        }

        var query = new QueryLobbiesOptions()
        {
            Filters = new List<QueryFilter>()
            {
                new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
            }
        };

        QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(query);
        return response.Results;
    }

    #endregion

    #region Transport Setup

    private void SetupRelayTransport(string ip, ushort port, byte[] allocationIdBytes, byte[] key, byte[] connectionData, byte[] hostConnectionData = null)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(
            ip,
            port,
            allocationIdBytes,
            key,
            connectionData,
            hostConnectionData
        );
    }

    #endregion
}
