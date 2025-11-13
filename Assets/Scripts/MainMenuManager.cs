using System;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    public TMP_Text informationText;
    public TMP_InputField ipAddress_Input;
    public TMP_InputField portAddress_Input;
    public TMP_InputField playerName_Input;
    void Start()
    {
        informationText.text = "Disconnect";
        NetworkManager.Singleton.OnClientStarted += OnClientConnectedMethod;
        NetworkManager.Singleton.OnClientStopped += OnClientDisconnectedMethod;
    }

    private void OnClientDisconnectedMethod(bool isHost)
    {
        informationText.text = "Disconnected";
    }

    private void OnClientConnectedMethod()
    {
        informationText.text = "Connected as" + (NetworkManager.Singleton.IsHost ? "Host" : "Client");
    }

    public void ConnectAsHost()
    {
        UserListManager.Singleton.localUserName = playerName_Input.text;
        if (SetConnectionData())
        {
            NetworkManager.Singleton.StartHost();
        }

    }

    public void ConnectAsClient()
    {
        if (SetConnectionData())
        {
            NetworkManager.Singleton.StartClient();
        }
    }

    public bool SetConnectionData()
    {
        ushort portNumber = 7777;
        if (ushort.TryParse(portAddress_Input.text, out portNumber))
        {
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ipAddress_Input.text, portNumber);
            return true;
        }
        else
        {
            Debug.Log("Error while paring port number, must a be a value between 0 and 65535");
            return false;
        }
    }

    public void Disconnect()
    {
        NetworkManager.Singleton.Shutdown();
    }

    public void CloseApp()
    {
        Application.Quit();
    }
}