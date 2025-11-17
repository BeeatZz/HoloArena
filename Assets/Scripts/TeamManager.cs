using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEditor.PackageManager;

using UnityEngine;

public class TeamManager : NetworkBehaviour
{
    public static TeamManager Instance;

    public NetworkList<FixedString64Bytes> TeamA;
    public NetworkList<FixedString64Bytes> TeamB;

    private const int MaxPlayersPerTeam = 2;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        TeamA = new NetworkList<FixedString64Bytes>();
        TeamB = new NetworkList<FixedString64Bytes>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            TeamA.OnListChanged += (_) => OnTeamsUpdated?.Invoke();
            TeamB.OnListChanged += (_) => OnTeamsUpdated?.Invoke();
        }
    }

    public event Action OnTeamsUpdated;

    public void RequestJoinTeam(string username, string teamName)
    {
        RequestJoinTeamServerRpc(username, teamName);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestJoinTeamServerRpc(string username, string teamName)
    {
        NetworkList<FixedString64Bytes> team = teamName == "A" ? TeamA : TeamB;

        if (team.Count >= MaxPlayersPerTeam)
            return;

        TeamA.Remove(username);
        TeamB.Remove(username);

        team.Add(username);
    }
}
