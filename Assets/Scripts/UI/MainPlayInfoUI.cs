using TMPro;
using UnityEngine;

public class MainPlayInfoUI : MonoBehaviour
{
    [SerializeField] private TMP_Text remainingDistanceText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text pirateGoldText;

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
}
