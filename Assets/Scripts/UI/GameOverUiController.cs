using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GameOverUiController : MonoBehaviour
{
    private const string TitleSceneName = "TitleScene";

    public static GameOverUiController Instance { get; private set; }

    [Header("Panel References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button titleButton;
    [SerializeField] private bool hidePanelOnStart = true;

    [Header("Optional Runtime References")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private DiceManager diceManager;
    [SerializeField] private CardManager cardManager;
    [SerializeField] private SettingsInputRelay settingsInputRelay;

    public bool IsGameOver { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (gameOverPanel == null)
        {
            gameOverPanel = gameObject;
        }

        CacheReferences();
        RegisterButtons();

        if (hidePanelOnStart)
        {
            SetPanelVisible(false);
        }
    }

    private void OnDestroy()
    {
        UnregisterButtons();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ShowGameOver()
    {
        if (IsGameOver)
        {
            return;
        }

        CacheReferences();
        IsGameOver = true;
        BlockNonGameOverInputs();
        SetPanelVisible(true);

        if (gameOverPanel != null)
        {
            gameOverPanel.transform.SetAsLastSibling();
        }

        if (retryButton != null)
        {
            EventSystem.current?.SetSelectedGameObject(retryButton.gameObject);
        }
    }

    public void RetryCurrentScene()
    {
        AudioManager.instance.PlaySfx(AudioManager.Sfx.ButtonClick); //***
        GameManager.Instance?.PrepareForFieldSceneRetry();
        SceneTransitionFader.Instance.FadeToScene(SceneManager.GetActiveScene().name, 0.5f, 0.1f);
    }

    public void MoveToTitle()
    {
        AudioManager.instance.PlaySfx(AudioManager.Sfx.ButtonClick); //***
        GameManager.Instance?.ResetRuntimeDataToDefaults();
        SceneTransitionFader.Instance.FadeToScene(TitleSceneName, 0.5f, 0.1f);
    }

    private void CacheReferences()
    {
        if (gameOverPanel == null)
        {
            gameOverPanel = gameObject;
        }

        if (retryButton == null && gameOverPanel != null)
        {
            Transform retryTransform = gameOverPanel.transform.Find("Btn_Retry");
            if (retryTransform != null)
            {
                retryButton = retryTransform.GetComponent<Button>();
            }
        }

        if (titleButton == null && gameOverPanel != null)
        {
            Transform titleTransform = gameOverPanel.transform.Find("Btn_Title");
            if (titleTransform != null)
            {
                titleButton = titleTransform.GetComponent<Button>();
            }
        }

        if (playerInput == null)
        {
            playerInput = FindFirstObjectByType<PlayerInput>();
        }

        if (diceManager == null)
        {
            diceManager = FindFirstObjectByType<DiceManager>();
        }

        if (cardManager == null)
        {
            cardManager = FindFirstObjectByType<CardManager>();
        }

        if (settingsInputRelay == null)
        {
            settingsInputRelay = FindFirstObjectByType<SettingsInputRelay>();
        }
    }

    private void RegisterButtons()
    {
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(RetryCurrentScene);
        }

        if (titleButton != null)
        {
            titleButton.onClick.AddListener(MoveToTitle);
        }
    }

    private void UnregisterButtons()
    {
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(RetryCurrentScene);
        }

        if (titleButton != null)
        {
            titleButton.onClick.RemoveListener(MoveToTitle);
        }
    }

    private void BlockNonGameOverInputs()
    {
        Selectable[] allSelectables = FindObjectsByType<Selectable>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < allSelectables.Length; i++)
        {
            Selectable selectable = allSelectables[i];
            if (selectable == null)
            {
                continue;
            }

            if (selectable == retryButton || selectable == titleButton)
            {
                continue;
            }
            selectable.interactable = false;
        }

        if (playerInput != null)
        {
            playerInput.enabled = false;
        }

        if (diceManager != null)
        {
            diceManager.SetPrimaryActionLocked(true);
            diceManager.enabled = false;
        }

        if (cardManager != null)
        {
            cardManager.enabled = false;
        }

        if (settingsInputRelay != null)
        {
            settingsInputRelay.enabled = false;
        }
    }

    private void SetPanelVisible(bool visible)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(visible);
        }
    }
}
