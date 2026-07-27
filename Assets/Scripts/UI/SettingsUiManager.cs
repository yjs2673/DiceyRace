using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class SettingsUiManager : MonoBehaviour
{
    private const string TitleSceneName = "TitleScene";

    [Header("Panel References")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject titleConfirmPanel;
    [SerializeField] private GameObject titleButton;
    [SerializeField] private bool hidePanelsOnStart = true;

    [Header("Slider References")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider brightnessSlider;

    [Header("Brightness Overlay")]
    [SerializeField] private Image brightnessOverlay;
    [SerializeField, Range(0f, 1f)] private float maxOverlayAlpha = 1f;

    private GameSettingsManager settingsManager;

    private void Awake()
    {
        settingsManager = GameSettingsManager.EnsureInstance();
        RefreshTitleButtonState();

        if (hidePanelsOnStart)
        {
            SetSettingsPanelVisible(false);
            SetTitleConfirmVisible(false);
        }
    }

    private void OnEnable()
    {
        settingsManager = GameSettingsManager.EnsureInstance();
        settingsManager.BgmVolumeChanged += HandleBgmVolumeChanged;
        settingsManager.SfxVolumeChanged += HandleSfxVolumeChanged;
        settingsManager.BrightnessChanged += HandleBrightnessChanged;

        RefreshFromSettings();
        RefreshTitleButtonState();
    }

    private void OnDisable()
    {
        if (settingsManager == null)
        {
            return;
        }

        settingsManager.BgmVolumeChanged -= HandleBgmVolumeChanged;
        settingsManager.SfxVolumeChanged -= HandleSfxVolumeChanged;
        settingsManager.BrightnessChanged -= HandleBrightnessChanged;
    }

    public void OpenSettings()
    {
        AudioManager.instance.PlaySfx(AudioManager.Sfx.ButtonClick); //***
        SetSettingsPanelVisible(true);
        SetTitleConfirmVisible(false);
        RefreshFromSettings();
    }

    public void CloseSettings()
    {
        AudioManager.instance.PlaySfx(AudioManager.Sfx.ButtonClick); //***
        SetSettingsPanelVisible(false);
        SetTitleConfirmVisible(false);
    }

    public void ToggleSettings()
    {
        bool shouldOpen = settingsPanel != null && !settingsPanel.activeSelf;

        if (shouldOpen)
        {
            OpenSettings();
            return;
        }

        CloseSettings();
    }

    public void OnMenu(InputValue value)
    {
        if (value == null || !value.isPressed)
        {
            return;
        }

        ToggleSettings();
    }

    public void OnEsc(InputValue value)
    {
        if (value == null || !value.isPressed)
        {
            return;
        }

        ToggleSettings();
    }

    public void OpenTitleConfirm()
    {
        if (SceneManager.GetActiveScene().name == TitleSceneName)
        {
            return;
        }

        AudioManager.instance.PlaySfx(AudioManager.Sfx.ButtonClick); //***
        SetTitleConfirmVisible(true);
    }

    public void CloseTitleConfirm()
    {
        AudioManager.instance.PlaySfx(AudioManager.Sfx.ButtonClick); //***
        SetTitleConfirmVisible(false);
    }

    public void ConfirmMoveToTitle()
    {
        CloseSettings();
        GameManager.Instance?.ResetRuntimeDataToDefaults();
        SceneTransitionFader.Instance.FadeToScene(TitleSceneName, 0.5f, 0.1f);
    }

    public void SetBgmVolume(float value)
    {
        GameSettingsManager.EnsureInstance().SetBgmVolume(value);
    }

    public void SetSfxVolume(float value)
    {
        GameSettingsManager.EnsureInstance().SetSfxVolume(value);
    }

    public void SetBrightness(float value)
    {
        GameSettingsManager.EnsureInstance().SetBrightness(value);
    }

    public void RefreshFromSettings()
    {
        GameSettingsManager currentSettings = GameSettingsManager.EnsureInstance();
        HandleBgmVolumeChanged(currentSettings.BgmVolume);
        HandleSfxVolumeChanged(currentSettings.SfxVolume);
        HandleBrightnessChanged(currentSettings.Brightness);
    }

    private void HandleBgmVolumeChanged(float value)
    {
        if (bgmSlider != null)
        {
            bgmSlider.SetValueWithoutNotify(value);
        }
    }

    private void HandleSfxVolumeChanged(float value)
    {
        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(value);
        }
    }

    private void HandleBrightnessChanged(float value)
    {
        if (brightnessSlider != null)
        {
            brightnessSlider.SetValueWithoutNotify(value);
        }

        ApplyBrightness(value);
    }

    private void ApplyBrightness(float brightness)
    {
        if (brightnessOverlay == null)
        {
            return;
        }

        Color color = brightnessOverlay.color;
        color.a = (1f - Mathf.Clamp01(brightness)) * maxOverlayAlpha;
        brightnessOverlay.color = color;
    }

    private void RefreshTitleButtonState()
    {
        if (titleButton == null)
        {
            return;
        }

        titleButton.SetActive(SceneManager.GetActiveScene().name != TitleSceneName);
    }

    private void SetSettingsPanelVisible(bool visible)
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(visible);
        }
    }

    private void SetTitleConfirmVisible(bool visible)
    {
        if (titleConfirmPanel != null)
        {
            titleConfirmPanel.SetActive(visible);
        }
    }
}
