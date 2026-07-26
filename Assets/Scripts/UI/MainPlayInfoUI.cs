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
            remainingDistanceText.text = $"남은 거리: {remainingDistance}";
        }

        if (GameManager.Instance == null)
        {
            return;
        }

        if (goldText != null)
        {
            goldText.text = $"골드: {GameManager.Instance.Coin}";
        }

        if (pirateGoldText != null)
        {
            pirateGoldText.text = $"해적 골드: {GameManager.Instance.PirateCoin}";
        }
    }
}
