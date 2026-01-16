using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class TeamSelectionUI : MonoBehaviour
{
    public static TeamSelectionUI Instance;

    [Header("Team Text Displays")]
    public TMP_Text teamAText;
    public TMP_Text teamBText;

    [Header("Buttons")]
    public Button joinTeamAButton;
    public Button joinTeamBButton;

    private string username;

    private readonly List<LobbyPlayer> trackedPlayers = new List<LobbyPlayer>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        username = RelayManager.Instance.localUsername;

        joinTeamAButton.interactable = false;
        joinTeamBButton.interactable = false;

        joinTeamAButton.onClick.AddListener(() => JoinTeam("A"));
        joinTeamBButton.onClick.AddListener(() => JoinTeam("B"));

        StartCoroutine(WaitForTeamManager());

        // 🔥 Ensure UIs always re-scan for player objects
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        // 🔥 Also scan on start because players can already exist
        StartCoroutine(ScanAfterFrame());
    }
    private void OnClientConnected(ulong clientId)
    {
        // Scan for players every time anyone connects
        ScanForPlayers();
    }
    private IEnumerator ScanAfterFrame()
    {
        yield return null;
        ScanForPlayers();
    }

    private IEnumerator WaitForTeamManager()
    {
        while (TeamManager.Instance == null)
            yield return null;

        joinTeamAButton.interactable = true;
        joinTeamBButton.interactable = true;

        TeamManager.Instance.OnTeamsUpdated += RefreshUI;

        RefreshUI();
    }

    private void JoinTeam(string team)
    {
        if (TeamManager.Instance == null)
        {
            Debug.LogWarning("Cannot join team yet. TeamManager not initialized.");
            return;
        }

        TeamManager.Instance.RequestJoinTeam(username, team);
    }

    // 🔥 FIX: This finds and registers ALL LobbyPlayers
    public void ScanForPlayers()
    {
        foreach (var p in FindObjectsOfType<LobbyPlayer>())
            RegisterPlayer(p);
    }

    // 🔥 FIX: This ensures clients subscribe to IsReady updates
    public void RegisterPlayer(LobbyPlayer player)
    {
        if (trackedPlayers.Contains(player)) return;

        trackedPlayers.Add(player);

        // Subscribe to ready changes
        player.IsReady.OnValueChanged += (_, _) => RefreshUI();

        // Force update immediately
        RefreshUI();
    }
    private IEnumerator DelayedScan()
    {
        yield return new WaitForSeconds(0.1f); // ensure LobbyPlayers spawned
        ScanForPlayers();
    }


    public void RefreshUI()
    {
        if (TeamManager.Instance == null)
            return; // or show "Loading..." text

        if (TeamManager.Instance.TeamA == null || TeamManager.Instance.TeamB == null)
            return;

        // === Team A ===
        teamAText.text = "Team A:\n";
        foreach (var name in TeamManager.Instance.TeamA)
        {
            LobbyPlayer player = FindLobbyPlayerByUsername(name.ToString());
            bool ready = player != null && player.IsReady.Value;
            string color = ready ? "<color=green>" : "<color=white>";
            teamAText.text += $"{color}{name}</color>\n";
        }

        // === Team B ===
        teamBText.text = "Team B:\n";
        foreach (var name in TeamManager.Instance.TeamB)
        {
            LobbyPlayer player = FindLobbyPlayerByUsername(name.ToString());
            bool ready = player != null && player.IsReady.Value;
            string color = ready ? "<color=green>" : "<color=white>";
            teamBText.text += $"{color}{name}</color>\n";
        }
    }

    public void UnregisterPlayer(LobbyPlayer player)
    {
        if (player == null) return;

        if (trackedPlayers.Contains(player))
            trackedPlayers.Remove(player);

        RefreshUI();
    }

    private LobbyPlayer FindLobbyPlayerByUsername(string username)
    {
        foreach (var p in trackedPlayers)
        {
            if (p.Username.Value == username)
                return p;
        }
        return null;
    }
}
