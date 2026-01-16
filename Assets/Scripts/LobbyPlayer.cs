using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using System.Collections;

public class LobbyPlayer : NetworkBehaviour
{
    public NetworkVariable<bool> IsReady = new(false);
    public NetworkVariable<int> CharacterIndex = new(0);
    public NetworkVariable<FixedString64Bytes> Username = new();

    // Store the actual client ID that this lobby player represents
    public NetworkVariable<ulong> ClientId = new(0);

    private TeamSelectionUI teamUI;

    public override void OnNetworkSpawn()
    {
        DontDestroyOnLoad(gameObject);

        // Store the owner's client ID
        if (IsServer)
        {
            ClientId.Value = OwnerClientId;
        }

        teamUI = FindObjectOfType<TeamSelectionUI>();

        if (IsOwner)
        {
            SetUsernameServerRpc(RelayManager.Instance.localUsername);
        }

        IsReady.OnValueChanged += OnReadyChanged;
        CharacterIndex.OnValueChanged += OnCharacterChanged;
        Username.OnValueChanged += (oldV, newV) => teamUI?.RefreshUI();

        if (TeamSelectionUI.Instance != null)
        {
            TeamSelectionUI.Instance.RegisterPlayer(this);
        }
        else
        {
            StartCoroutine(RegisterWithUIWhenReady());
        }

        teamUI?.RefreshUI();
    }

    [ServerRpc]
    public void SetUsernameServerRpc(FixedString64Bytes name)
    {
        Username.Value = name;
    }

    private IEnumerator RegisterWithUIWhenReady()
    {
        TeamSelectionUI ui = null;
        while (ui == null)
        {
            ui = TeamSelectionUI.Instance;
            yield return null;
        }
        ui.RegisterPlayer(this);
        ui.RefreshUI();
    }

    private void OnReadyChanged(bool oldValue, bool newValue)
    {
        if (TeamSelectionUI.Instance != null)
        {
            TeamSelectionUI.Instance.RefreshUI();
        }
        if (IsServer)
        {
            FindObjectOfType<LobbyChatManager>()?.CheckReadyStatus();
        }
    }

    private void OnCharacterChanged(int oldValue, int newValue)
    {
        teamUI?.RefreshUI();
    }

    public override void OnNetworkDespawn()
    {
        var ui = FindObjectOfType<TeamSelectionUI>();
        ui?.UnregisterPlayer(this);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetReadyServerRpc(bool ready) => IsReady.Value = ready;

    [ServerRpc(RequireOwnership = false)]
    public void SetCharacterServerRpc(int index) => CharacterIndex.Value = index;
}