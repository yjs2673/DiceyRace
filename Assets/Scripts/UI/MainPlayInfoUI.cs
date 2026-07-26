using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainPlayInfoUI : MonoBehaviour
{
    [SerializeField] private TMP_Text remainingDistanceText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text pirateGoldText;
    [SerializeField] private Button infoButton;
    [SerializeField] private TutorialInfoUI tutorialInfoPrefab;

    private TutorialInfoUI tutorialInfo;

    private void Awake()
    {
        if (infoButton != null)
        {
            infoButton.onClick.AddListener(ShowTutorial);
        }
    }

    private void Start()
    {
        RefreshUI();
    }

    private void Update()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (remainingDistanceText != null)
        {
            int remainingDistance = StageManager.Instance != null
                ? StageManager.Instance.RemainingDistanceToGoal
                : 0;
            remainingDistanceText.text = $"도착지까지\\n{remainingDistance}칸";
        }

        if (GameManager.Instance == null)
        {
            return;
        }

        if (goldText != null)
        {
            goldText.text = $"{GameManager.Instance.Coin}";
        }

        if (pirateGoldText != null)
        {
            pirateGoldText.text = $"{GameManager.Instance.PirateCoin}";
        }
    }

    private void OnDestroy()
    {
        if (infoButton != null)
        {
            infoButton.onClick.RemoveListener(ShowTutorial);
        }
    }

    public void ShowTutorial()
    {
        if (tutorialInfo == null)
        {
            tutorialInfo = FindFirstObjectByType<TutorialInfoUI>(FindObjectsInactive.Include);
        }

        if (tutorialInfo == null && tutorialInfoPrefab != null)
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            Transform parent = parentCanvas != null ? parentCanvas.transform : transform.parent;
            tutorialInfo = Instantiate(tutorialInfoPrefab, parent, false);
        }

        if (tutorialInfo == null)
        {
            Debug.LogWarning("TutorialInfo prefab is not assigned.", this);
            return;
        }

        tutorialInfo.Show();
    }
}
