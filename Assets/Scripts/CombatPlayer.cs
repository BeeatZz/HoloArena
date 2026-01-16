using Unity.Netcode;
using UnityEngine;

public class CombatPlayer : NetworkBehaviour
{
    public NetworkVariable<int> CharacterIndex = new(0);
    public SpriteRenderer spriteRenderer;
    public Sprite[] characterSprites; 

    public override void OnNetworkSpawn()
    {
        UpdateVisuals(0, CharacterIndex.Value);
        CharacterIndex.OnValueChanged += UpdateVisuals;
    }

    private void UpdateVisuals(int oldV, int newV)
    {
        if (newV < characterSprites.Length)
            spriteRenderer.sprite = characterSprites[newV];
    }
}