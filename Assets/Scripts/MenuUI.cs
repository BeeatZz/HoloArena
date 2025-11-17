using UnityEngine;
using TMPro;

public class MenuUI : MonoBehaviour
{
    public TMP_InputField usernameInput;
    public TMP_InputField joinCodeInput;

    public void OnCreateRoom()
    {
        RelayManager.Instance.CreateRoom();
    }

    public void OnJoinByCode()
    {
        RelayManager.Instance.JoinLobbyByCode(joinCodeInput.text);
    }
}
