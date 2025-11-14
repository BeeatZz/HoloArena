using UnityEngine;
using UnityEngine.UI;

public class ToggleColorSwap : MonoBehaviour
{
    public Toggle toggle;
    public Image backgroundImage;

    public Color offColor = Color.red;
    public Color onColor = Color.green;

    void Start()
    {
        UpdateColor(toggle.isOn);

        toggle.onValueChanged.AddListener(UpdateColor);
    }

    void UpdateColor(bool isOn)
    {
        backgroundImage.color = isOn ? onColor : offColor;
    }
}
