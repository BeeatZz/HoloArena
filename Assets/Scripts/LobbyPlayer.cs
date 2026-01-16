using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using System.Collections;

public class LobbyPlayer : NetworkBehaviour
{
    public NetworkVariable<bool> IsReady = new(false);
    public NetworkVariable<int> CharacterIndex = new(0);
    public NetworkVariable<FixedString64Bytes> Username = new();

    private TeamSelectionUI teamUI;

    // inside LobbyPlayer.cs
    public override void OnNetworkSpawn()
    {
        teamUI = FindObjectOfType<TeamSelectionUI>();

        if (IsOwner)
        {
            Username.Value = RelayManager.Instance.localUsername;
        }

        // Subscribe to changes
        IsReady.OnValueChanged += OnReadyChanged;
        CharacterIndex.OnValueChanged += OnCharacterChanged;
        Username.OnValueChanged += (oldV, newV) => teamUI?.RefreshUI();

        // Immediately register if UI exists
        if (TeamSelectionUI.Instance != null)
        {
            TeamSelectionUI.Instance.RegisterPlayer(this);
        }
        else
        {
            StartCoroutine(RegisterWithUIWhenReady());
        }

        // Force refresh after spawn
        teamUI?.RefreshUI();
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

        // Force first refresh
        ui.RefreshUI();
    }

    private void OnReadyChanged(bool oldValue, bool newValue)
    {
        if (TeamSelectionUI.Instance != null && TeamManager.Instance != null)
        {
            TeamSelectionUI.Instance.RefreshUI();
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
