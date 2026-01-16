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

        toggle.isOn = localPlayer.IsReady.Value;

        toggle.onValueChanged.AddListener(OnToggleChanged);

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
        toggle.isOn = newValue;

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
