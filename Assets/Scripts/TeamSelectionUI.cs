using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TeamSelectionUI : MonoBehaviour
{
    [Header("Team Text Displays")]
    public TMP_Text teamAText;
    public TMP_Text teamBText;

    [Header("Buttons")]
    public Button joinTeamAButton;
    public Button joinTeamBButton;

    private string username;

    private void Start()
    {
        username = RelayManager.Instance.localUsername;

        joinTeamAButton.interactable = false;
        joinTeamBButton.interactable = false;

        joinTeamAButton.onClick.AddListener(() => JoinTeam("A"));
        joinTeamBButton.onClick.AddListener(() => JoinTeam("B"));

        StartCoroutine(WaitForTeamManager());
    }

    private System.Collections.IEnumerator WaitForTeamManager()
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


    public void RefreshUI()
    {
        teamAText.text = "Team A:\n";
        foreach (var name in TeamManager.Instance.TeamA)
        {
            LobbyPlayer player = FindLobbyPlayerByUsername(name.ToString());
            string color = (player != null && player.IsReady.Value) ? "<color=green>" : "<color=white>";
            teamAText.text += $"{color}{name}</color>\n";
        }

        teamBText.text = "Team B:\n";
        foreach (var name in TeamManager.Instance.TeamB)
        {
            LobbyPlayer player = FindLobbyPlayerByUsername(name.ToString());
            string color = (player != null && player.IsReady.Value) ? "<color=green>" : "<color=white>";
            teamBText.text += $"{color}{name}</color>\n";
        }
    }

    private LobbyPlayer FindLobbyPlayerByUsername(string username)
    {
        foreach (var player in FindObjectsOfType<LobbyPlayer>())
        {
            if (player.Username.Value.ToString() == username)
                return player;
        }
        return null;
    }

}
