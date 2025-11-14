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
    public TMP_InputField usernameInput;
    public TMP_InputField searchInput;
    public TMP_InputField roomCodeInput;
    public TMP_Text roomInfoText;
    public Transform lobbyListParent;
    public GameObject lobbyEntryPrefab;
    public Toggle privateToggle;

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

    public async void CreateRoom(int maxPlayers = 2)
    {
        string userName = usernameInput.text;
        if (string.IsNullOrWhiteSpace(userName))
        {
            Debug.LogWarning("Username is empty");
            return;
        }
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        SetupRelayTransport(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData
        );
        bool startedHost = NetworkManager.Singleton.StartHost();
        if (!startedHost)
        {
            Debug.LogError("Failed to start host");
            return;
        }
        string lobbyName = userName + "'s Lobby";
        var options = new CreateLobbyOptions();
        options.IsPrivate = false;
        options.Data = new Dictionary<string, DataObject>();
        options.Data.Add("RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, joinCode));
        options.Data.Add("HostName", new DataObject(DataObject.VisibilityOptions.Public, userName));
        currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
        Debug.Log("Lobby hosted: " + currentLobby.Name + " | Join Code: " + joinCode);
        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    }

    public async void JoinLobbyByCode()
    {
        string code = roomCodeInput.text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("Join code is empty");
            return;
        }
        Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code);
        DataObject joinCodeObj;
        bool hasJoinCode = joinedLobby.Data.TryGetValue("JoinCode", out joinCodeObj);
        if (!hasJoinCode)
        {
            Debug.LogError("JoinCode missing in lobby data");
            return;
        }
        string joinCode = joinCodeObj.Value;
        JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);
        SetupRelayTransport(
            joinAlloc.RelayServer.IpV4,
            (ushort)joinAlloc.RelayServer.Port,
            joinAlloc.AllocationIdBytes,
            joinAlloc.Key,
            joinAlloc.ConnectionData,
            joinAlloc.HostConnectionData
        );
        bool startedClient = NetworkManager.Singleton.StartClient();
        if (!startedClient)
        {
            Debug.LogError("Failed to start client");
            return;
        }
        Debug.Log("Joined private lobby with code: " + code);
        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    }

    public async void RefreshLobbies()
    {
        var queryOptions = new QueryLobbiesOptions();
        queryOptions.Filters = new List<QueryFilter>();
        QueryFilter filter = new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT);
        queryOptions.Filters.Add(filter);
        QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(queryOptions);
        foreach (Transform child in lobbyListParent)
        {
            Destroy(child.gameObject);
        }
        foreach (Lobby lobby in response.Results)
        {
            GameObject entryGO = Instantiate(lobbyEntryPrefab, lobbyListParent);
            LobbyEnter entryUI = entryGO.GetComponent<LobbyEnter>();
            if (entryUI != null)
            {
                entryUI.Setup(lobby);
            }
        }
        Debug.Log("Found " + response.Results.Count + " joinable lobbies");
    }

    public async void JoinRoom(Lobby lobbyToJoin)
    {
        string userName = usernameInput.text;
        if (string.IsNullOrWhiteSpace(userName))
        {
            Debug.LogWarning("Username is empty");
            return;
        }
        Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyToJoin.Id);

        DataObject joinCodeObj;
        bool hasJoinCode = joinedLobby.Data.TryGetValue("RelayJoinCode", out joinCodeObj);
        if (!hasJoinCode)
        {
            Debug.LogError("Relay join code missing in lobby data");
            return;
        }
        string joinCode = joinCodeObj.Value;
        JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);
        SetupRelayTransport(
            joinAlloc.RelayServer.IpV4,
            (ushort)joinAlloc.RelayServer.Port,
            joinAlloc.AllocationIdBytes,
            joinAlloc.Key,
            joinAlloc.ConnectionData,
            joinAlloc.HostConnectionData
        );
        bool startedClient = NetworkManager.Singleton.StartClient();
        if (!startedClient)
        {
            Debug.LogError("Failed to start client");
            return;
        }
        Debug.Log("Joined lobby: " + joinedLobby.Name);
        UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
    }

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

}
