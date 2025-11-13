using UnityEngine;
using Unity.Netcode;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System;
using Unity.VisualScripting;
using TMPro;
public class UserConnectedData : INetworkSerializable
{
    public ulong userId;
    public string userName;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref userId);
        serializer.SerializeValue(ref userName);
    }
}

public class UserListManager : NetworkBehaviour
{
    private static UserListManager singleton;
    public static UserListManager Singleton => singleton;

    public List<UserConnectedData> userConnectedList;

    public string localUserName;

    private void Awake()
    {
        if (singleton == null)
        {
            singleton = this;
        }

        else
        {
            Destroy(this.gameObject);
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        userConnectedList = new List<UserConnectedData>();
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedMethod;
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        AddNewUserServerRPC(NetworkManager.Singleton.LocalClientId, localUserName);
    }
    private void OnClientDisconnectedMethod(ulong userId)
    {
        for (int i = 0; i < userConnectedList.Count; i++)
        {
            if (userConnectedList[i].userId == userId)
            {
                userConnectedList.Remove(userConnectedList[i]);
            }

        }
        RefreshUserConnectedListClientRPC(userConnectedList.ToArray());
    }

    [ServerRpc(RequireOwnership = false)]

    public void AddNewUserServerRPC(ulong newUserId, string newUserName)
    {
        UserConnectedData newUserConnectedData = new UserConnectedData();

        newUserConnectedData.userId = newUserId;
        newUserConnectedData.userName = newUserName;

        userConnectedList.Add(newUserConnectedData);

        RefreshUserConnectedListClientRPC(userConnectedList.ToArray());
    }

    [ClientRpc]
    public void RefreshUserConnectedListClientRPC(UserConnectedData[] userConnectedArray)
    {
        userConnectedList = userConnectedArray.ToList();
        // Aqui accedemos al objeto de texto, que contiene la lista de usuarios en pantalla
        TMP_Text userListLog = FindAnyObjectByType<ChatManager>().userListLog;
        userListLog.text = "";
        for (int i = 0; i < userConnectedList.Count; i++)

        {
            userListLog.text += userConnectedList[i].userName + "\n";
        }
    }

    public string GetUserNameById(ulong clientId)
    {
        foreach (var user in userConnectedList)
        {
            if (user.userId == clientId)
                return user.userName;
        }
        return $"User{clientId}";
    }

}