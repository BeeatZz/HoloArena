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
using UnityEngine.SceneManagement;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance;

    Lobby currentLobby;
    bool initialized = false;
    string profileId;
    public string localUsername;
    public string currentLobbyCode; 

    async void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            await Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    async Task Initialize()
    {
        if (!initialized)
        {
            await UnityServices.InitializeAsync();
            initialized = true;
        }

        await SignInRandom();
    }

    public async Task SignInRandom()
    {
        profileId = "Player_" + Random.Range(1000, 9999);
        AuthenticationService.Instance.SwitchProfile(profileId);

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Signed in " + profileId);
        }
    }

    public async Task<bool> CheckAuth()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await SignInRandom();
        }

        return AuthenticationService.Instance.IsSignedIn;
    }

    public async Task CreateRoom(string username, int maxPlayers = 4)
    {
        if (!await CheckAuth()) return;
        if (string.IsNullOrEmpty(username)) return;

        localUsername = username;

        var alloc = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
        var joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(
            alloc.RelayServer.IpV4,
            (ushort)alloc.RelayServer.Port,
            alloc.AllocationIdBytes,
            alloc.Key,
            alloc.ConnectionData
        );

        NetworkManager.Singleton.StartHost();

        var options = new CreateLobbyOptions()
        {
            IsPrivate = false,
            Data = new Dictionary<string, DataObject>()
        {
            { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, joinCode) },
            { "HostName", new DataObject(DataObject.VisibilityOptions.Public, username) }
        }
        };

        string lobbyName = username + "'s Lobby (" + "1/" + maxPlayers + ")";
        currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
        currentLobbyCode = joinCode; 


        if (ConnectionCallbackManager.Instance != null)
        {
            ConnectionCallbackManager.Instance.informationalText.text = "Connected as Host";
        }
    }

    public async Task JoinRoom(string username, Lobby lobby)
    {
        if (!await CheckAuth()) return;
        if (string.IsNullOrEmpty(username)) return;

        localUsername = username;

        var joined = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);

        if (!joined.Data.TryGetValue("RelayJoinCode", out var joinObj)) return;

        await JoinRelay(joinObj.Value);

        if (ConnectionCallbackManager.Instance != null)
        {
            ConnectionCallbackManager.Instance.informationalText.text = "Connected as Client";
        }
    }

    public async Task JoinLobbyByCode(string username, string roomCode)
    {
        if (!await CheckAuth()) return;
        if (string.IsNullOrEmpty(username)) return;

        localUsername = username;

        var joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(roomCode);

        if (!joinedLobby.Data.TryGetValue("RelayJoinCode", out var joinObj)) return;

        await JoinRelay(joinObj.Value);

        if (ConnectionCallbackManager.Instance != null)
        {
            ConnectionCallbackManager.Instance.informationalText.text = "Connected as Client";
        }
    }

    async Task JoinRelay(string code)
    {
        var join = await RelayService.Instance.JoinAllocationAsync(code);

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(
            join.RelayServer.IpV4,
            (ushort)join.RelayServer.Port,
            join.AllocationIdBytes,
            join.Key,
            join.ConnectionData,
            join.HostConnectionData
        );

        NetworkManager.Singleton.StartClient();
    }

    public async Task<List<Lobby>> GetJoinableLobbies()
    {
        if (!await CheckAuth()) return new List<Lobby>();

        var query = new QueryLobbiesOptions()
        {
            Filters = new List<QueryFilter>()
            {
                new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
            }
        };

        var result = await LobbyService.Instance.QueryLobbiesAsync(query);
        return result.Results;
    }
}
