using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class ArenaManager : NetworkBehaviour
{
    [SerializeField] private GameObject combatPlayerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            SpawnPlayers();
        }
    }

    private void SpawnPlayers()
    {
        LobbyPlayer[] lobbyPlayers = FindObjectsOfType<LobbyPlayer>();

        Debug.Log($"[ArenaManager] Found {lobbyPlayers.Length} lobby players to spawn");

        for (int i = 0; i < lobbyPlayers.Length; i++)
        {
            // Use the stored ClientId from the LobbyPlayer
            ulong clientId = lobbyPlayers[i].ClientId.Value;
            Debug.Log($"[ArenaManager] Spawning player {i} for ClientId: {clientId}");

            Transform spawnPoint = spawnPoints[i % spawnPoints.Length];
            GameObject playerObj = Instantiate(combatPlayerPrefab, spawnPoint.position, Quaternion.identity);

            var networkObj = playerObj.GetComponent<NetworkObject>();

            // Spawn with ownership
            networkObj.SpawnWithOwnership(clientId);

            Debug.Log($"[ArenaManager] Spawned {playerObj.name} with OwnerClientId: {networkObj.OwnerClientId}");

            // Set character index
            playerObj.GetComponent<CombatPlayer>().CharacterIndex.Value = lobbyPlayers[i].CharacterIndex.Value;
        }
    }
}