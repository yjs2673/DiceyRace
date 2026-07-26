using UnityEngine;
using UnityEngine.InputSystem;

public sealed class SettingsInputRelay : MonoBehaviour
{
    [SerializeField] private SettingsUiManager settingsUiManager;

    public void OnMenu(InputValue value)
    {
        if (value == null || !value.isPressed || settingsUiManager == null)
        {
            return;
        }

        settingsUiManager.ToggleSettings();
    }

    public void OnEsc(InputValue value)
    {
        if (value == null || !value.isPressed || settingsUiManager == null)
        {
            return;
        }

        settingsUiManager.ToggleSettings();
    }
}
