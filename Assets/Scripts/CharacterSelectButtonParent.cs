using UnityEngine;

public class CharacterSelectButtonParent : MonoBehaviour
{
    public CharacterSelectButton[] buttons;

    private void Awake()
    {
        buttons = GetComponentsInChildren<CharacterSelectButton>();
    }
}
