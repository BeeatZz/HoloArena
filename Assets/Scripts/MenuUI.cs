using UnityEngine;
using TMPro;

public class MenuUI : MonoBehaviour
{
    public TMP_InputField usernameInput;
    public TMP_InputField joinCodeInput;

    public void OnCreateRoom(int players)
    {
        RelayManager.Instance.CreateRoom(usernameInput.text,players);
    }

    public void OnJoinByCode()
    {
        RelayManager.Instance.JoinLobbyByCode(usernameInput.text, joinCodeInput.text);
    }
}
