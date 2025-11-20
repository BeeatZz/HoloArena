using System.Globalization;
using Unity.Collections;
using Unity.Netcode;
using UnityEditor.PackageManager;

public class LobbyPlayer : NetworkBehaviour
{
    public NetworkVariable<bool> IsReady = new NetworkVariable<bool>(false);
    public NetworkVariable<int> CharacterIndex = new NetworkVariable<int>(0);
    public NetworkVariable<FixedString64Bytes> Username = new NetworkVariable<FixedString64Bytes>();

    private TeamSelectionUI teamUI;

    public override void OnNetworkSpawn()
    {
        // Find the TeamSelectionUI in the scene
        teamUI = FindObjectOfType<TeamSelectionUI>();

        if (IsOwner)
        {
            Username.Value = RelayManager.Instance.localUsername;
        }

        if (IsClient)
        {
            IsReady.OnValueChanged += OnReadyChanged;
            CharacterIndex.OnValueChanged += OnCharacterChanged;
            Username.OnValueChanged += (oldValue, newValue) => teamUI?.RefreshUI();
        }
    }

    private void OnReadyChanged(bool oldValue, bool newValue)
    {
        teamUI?.RefreshUI();
    }

    private void OnCharacterChanged(int oldValue, int newValue)
    {
        // Optional: highlight selected character
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetReadyServerRpc(bool ready)
    {
        IsReady.Value = ready;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetCharacterServerRpc(int index)
    {
        CharacterIndex.Value = index;
    }
}
