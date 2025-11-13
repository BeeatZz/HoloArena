using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ChatManager : NetworkBehaviour
{

    [SerializeField]
    private TMP_Text chatText;
    [SerializeField]
    private TMP_InputField chatInput;

    public TMP_Text userListLog;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        UserListManager.Singleton.RefreshUserConnectedListClientRPC(UserListManager.Singleton.userConnectedList.ToArray());
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SendMessage()
    {
        MyServerRPC(chatInput.text, NetworkManager.Singleton.LocalClientId);
    }


    [ClientRpc]
    private void MyClientRPC(string message)
    {
        chatText.text += "\n" + message;

    }


    
    [ServerRpc(RequireOwnership = false)]
    private void MyServerRPC(string message, ulong senderId)
    {
        string username = UserListManager.Singleton.GetUserNameById(senderId);
        message = username + ": " + message;

        MyClientRPC(message);
    }

}
