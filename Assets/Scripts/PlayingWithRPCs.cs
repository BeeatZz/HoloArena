using Unity.Netcode;
using UnityEngine;

public class PlayingWithRPCs : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ExecuteClient_RPC()
    {
        MyClientRPC();
    }
    public void ExecuteServer_RPC()
    {
        MyServerRPC();
    }

    [ClientRpc]
    private void MyClientRPC()
    {
        Debug.Log("im a clientrpc fromt the" + (NetworkManager.Singleton.IsHost ? "Host" : "Client"));
    }


    [ServerRpc(RequireOwnership = false)]
    private void MyServerRPC()
    {
        Debug.Log("im a Serverrpc fromt the" + (NetworkManager.Singleton.IsHost ? "Host" : "Client"));
    }
}
