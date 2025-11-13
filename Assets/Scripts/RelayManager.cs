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

        try
        {
            string lobbyName = userName + "'s Lobby";
            bool isPrivate = privateToggle != null && privateToggle.isOn;

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
            {
                {
                    "HostName", new DataObject(DataObject.VisibilityOptions.Public, userName) }
                }
            };


            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            bool started = NetworkManager.Singleton.StartHost();
            if (started)
            {
                //roomInfoText.text = $"Hosting {currentLobby.Name} ({currentLobby.Players.Count}/{currentLobby.MaxPlayers})";
                Debug.Log($"Lobby hosted: {currentLobby.Name}");
                UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");

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

    public async void JoinLobbyByCode()
    {
        string code = roomCodeInput.text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("Join code is empty");
            return;
        }

        try
        {
            Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code);

            if (!joinedLobby.Data.TryGetValue("JoinCode", out DataObject joinCodeObj))
            {
                Debug.LogError("JoinCode missing in lobby data");
                return;
            }
            string joinCode = joinCodeObj.Value;

            JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                joinAlloc.RelayServer.IpV4,
                (ushort)joinAlloc.RelayServer.Port,
                joinAlloc.AllocationIdBytes,
                joinAlloc.Key,
                joinAlloc.ConnectionData,
                joinAlloc.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();
            Debug.Log("Joined private lobby with code: " + code);
            UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");

        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("Failed to join lobby by code: " + e);
        }
    }
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

            foreach (Transform child in lobbyListParent)
                Destroy(child.gameObject);

            foreach (Lobby lobby in response.Results)
            {
                GameObject entryGO = Instantiate(lobbyEntryPrefab, lobbyListParent);
                LobbyEnter entryUI = entryGO.GetComponent<LobbyEnter>();
                if (entryUI != null)
                {
                    entryUI.Setup(lobby); // Pass the lobby to the prefab
                }
            }


            Debug.Log("Found " + response.Results.Count + " joinable lobbies");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError("LobbyServiceException: " + e);
        }
    }



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
            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyToJoin.Id);

            var updatePlayerOptions = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, userName) }
                }
            };
            await LobbyService.Instance.UpdatePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId, updatePlayerOptions);

            
            JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(currentLobby.Id); 

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                joinAlloc.RelayServer.IpV4,
                (ushort)joinAlloc.RelayServer.Port,
                joinAlloc.AllocationIdBytes,
                joinAlloc.Key,
                joinAlloc.ConnectionData,
                joinAlloc.HostConnectionData
            );

            if (!NetworkManager.Singleton.StartClient())
                Debug.LogError("Failed to start client");
            else
                Debug.Log($"Joined lobby: {currentLobby.Name}");
                UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");



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
