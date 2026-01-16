using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Unity.Netcode;

public class CharacterSelectButton : MonoBehaviour
{
    public int characterIndex;
    private Button button;
    private Image highlightImage;
    private LobbyPlayer localPlayer;

    private void Awake()
    {
        button = GetComponent<Button>();
        highlightImage = GetComponent<Image>();
    }

    private void Start()
    {
        button.onClick.AddListener(OnClick);
        StartCoroutine(WaitForLocalPlayerAndSubscribe());
    }

    private IEnumerator WaitForLocalPlayerAndSubscribe()
    {
        // Wait for NetworkManager and client
        while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
            yield return null;

        // Wait until the local LobbyPlayer exists
        while ((localPlayer = FindLocalLobbyPlayer()) == null)
            yield return null;

        // Set initial highlight
        UpdateHighlight();

        // When the local player's character index changes (from network), update highlights
        localPlayer.CharacterIndex.OnValueChanged += (oldV, newV) => UpdateHighlight();

        // Also refresh when players list changes in UI (optional)
        var teamUi = FindObjectOfType<TeamSelectionUI>();
        teamUi?.RefreshUI();
    }

    private void OnClick()
    {
        if (localPlayer == null)
        {
            localPlayer = FindLocalLobbyPlayer();
            if (localPlayer == null) return;
        }

        localPlayer.SetCharacterServerRpc(characterIndex);

        // instant local feedback - UpdateHighlight will also run once the network variable replicates
        UpdateHighlight();
    }

    private void UpdateHighlight()
    {
        // Local player might not be set yet
        if (localPlayer == null) localPlayer = FindLocalLobbyPlayer();
        if (localPlayer == null) return;

        var parent = GetComponentInParent<CharacterSelectButtonParent>();
        if (parent == null) return;

        var allButtons = parent.buttons;
        foreach (var btn in allButtons)
        {
            bool isSelected = (btn.characterIndex == localPlayer.CharacterIndex.Value);
            btn.highlightImage.color = isSelected ? Color.green : Color.white;
        }
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
            localPlayer.CharacterIndex.OnValueChanged -= (oldV, newV) => UpdateHighlight();
    }
}
