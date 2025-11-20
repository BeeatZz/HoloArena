using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectButton : MonoBehaviour
{
    public int characterIndex; 
    private Button button;

    private void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        LobbyPlayer localPlayer = FindLocalLobbyPlayer();
        if (localPlayer != null)
        {
            localPlayer.SetCharacterServerRpc(characterIndex);
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
}
