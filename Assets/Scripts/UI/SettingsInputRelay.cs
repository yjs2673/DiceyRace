using UnityEngine;
using UnityEngine.InputSystem;

public sealed class SettingsInputRelay : MonoBehaviour
{
    [SerializeField] private SettingsUiManager settingsUiManager;
    private int lastToggleFrame = -1;

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        ToggleSettings();
    }

    // public void OnMenu(InputValue value)
    // {
    //     if (value == null || !value.isPressed)
    //     {
    //         return;
    //     }

    //     ToggleSettings();
    // }

    // public void OnEsc(InputValue value)
    // {
    //     if (value == null || !value.isPressed)
    //     {
    //         return;
    //     }

    //     ToggleSettings();
    // }

    private void ToggleSettings()
    {
        if (settingsUiManager == null || lastToggleFrame == Time.frameCount)
        {
            return;
        }

        lastToggleFrame = Time.frameCount;
        settingsUiManager.ToggleSettings();
    }
}
