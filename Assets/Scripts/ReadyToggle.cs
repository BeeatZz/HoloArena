using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReadyToggle : MonoBehaviour
{
    private Toggle toggle;
    private LobbyPlayer localPlayer;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        if (toggle == null)
        {
            Debug.LogError("ReadyToggle requires a Toggle component.");
        }
    }

    private void Start()
    {
        StartCoroutine(WaitForLocalPlayer());
    }

    private IEnumerator WaitForLocalPlayer()
    {
        while ((localPlayer = FindLocalLobbyPlayer()) == null)
            yield return null;

        // Set initial toggle state
        toggle.isOn = localPlayer.IsReady.Value;

        // Listen for toggle changes from UI
        toggle.onValueChanged.AddListener(OnToggleChanged);

        // Listen for changes from network
        localPlayer.IsReady.OnValueChanged += OnReadyChanged;
    }


    private void OnToggleChanged(bool isOn)
    {
        if (localPlayer != null)
        {
            localPlayer.SetReadyServerRpc(isOn);
        }
    }

    private void OnReadyChanged(bool oldValue, bool newValue)
    {
        // Update toggle to reflect network state
        toggle.isOn = newValue;

        // Optional: refresh the TeamSelectionUI immediately
        var teamUI = FindObjectOfType<TeamSelectionUI>();
        teamUI?.RefreshUI();
    }

    private LobbyPlayer FindLocalLobbyPlayer()
    {
        foreach (var player in FindObjectsOfType<LobbyPlayer>())
        {
            if (player.IsOwner) return player;
        }
        return null;
    }

    private void OnDestroy()
    {
        if (localPlayer != null)
        {
            localPlayer.IsReady.OnValueChanged -= OnReadyChanged;
        }

        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(OnToggleChanged);
        }
    }
}
