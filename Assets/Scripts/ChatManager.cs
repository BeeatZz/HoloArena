using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyChatManager : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject chatMessagePrefab; 
    [SerializeField] private Transform chatContent;       

    [SerializeField] private TMP_InputField chatInput;  
    [SerializeField] private ScrollRect scrollRect;

    private void Start()
    {
        if (chatInput != null)
            chatInput.onSubmit.AddListener(OnInputSubmit);

        if (NetworkManager.Singleton.IsHost)
        {
            StartCoroutine(SendLobbyCodeWhenReady());
        }
    }

    private System.Collections.IEnumerator SendLobbyCodeWhenReady()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton.IsServer && !string.IsNullOrEmpty(RelayManager.Instance.currentLobbyCode));

        string message = $"LOBBY CODE: {RelayManager.Instance.currentLobbyCode}";
        SendChatServerRpc("", message);
    }


    private void OnDestroy()
    {
        if (chatInput != null)
        {
            chatInput.onSubmit.RemoveListener(OnInputSubmit);
        }
    }

    private void OnInputSubmit(string input)
    {
        SendMessage();
    }

  
    public void SendMessage()
    {
        string message = chatInput.text.Trim();
        if (string.IsNullOrEmpty(message)) return;

        string username = RelayManager.Instance.localUsername;

        SendChatServerRpc(username, message);

        chatInput.text = "";
        chatInput.ActivateInputField();
    }
    private string GetTeamColorHex(string username)
    {
        if (TeamManager.Instance.TeamA.Contains(username))
            return "#FF0000"; 
        if (TeamManager.Instance.TeamB.Contains(username))
            return "#0000FF"; 

        return "#FFFFFF"; 
    }


    [ServerRpc(RequireOwnership = false)]
    private void SendChatServerRpc(string username, string message)
    {
        BroadcastChatClientRpc(username, message);
    }




    [ClientRpc]
    public void BroadcastChatClientRpc(string username, string message)
    {
        string colorHex;
        if(username != "")
        {
            colorHex = GetTeamColorHex(username);

        }
        else
        {
            colorHex = "#3BB143";
        }
        string finalMessage = $"<color={colorHex}>{username}: {message}</color>";

        GameObject newMessage = Instantiate(chatMessagePrefab, chatContent);
        TMP_Text messageText = newMessage.GetComponent<TMP_Text>();
        messageText.text = finalMessage;

        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();
    }


}
