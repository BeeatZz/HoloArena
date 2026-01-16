using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    public TMP_InputField usernameInput;
    public TMP_InputField joinCodeInput;
    public Toggle lobbyType;

    public void OnCreateRoom(int players)
    {
        bool isPrivate = lobbyType.isOn;
        RelayManager.Instance.CreateRoom(usernameInput.text, isPrivate, players);
    }

    public void OnJoinByCode()
    {
        RelayManager.Instance.JoinLobbyByCode(usernameInput.text, joinCodeInput.text);
    }
}
