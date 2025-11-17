using Unity.Netcode;
using UnityEngine;

public class LobbySceneController : MonoBehaviour
{
    public GameObject teamManagerPrefab;

    private void Start()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            var obj = Instantiate(teamManagerPrefab);
            obj.GetComponent<NetworkObject>().Spawn();
        }
    }
}
