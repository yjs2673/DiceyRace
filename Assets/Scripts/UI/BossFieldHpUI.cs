using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossFieldHpUI : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Slider playerHpSlider;
    [SerializeField] private Text playerHpText;

    [Header("Boss")]
    [SerializeField] private BossController bossController;
    [SerializeField] private Slider bossHpSlider;
    [SerializeField] private Text bossHpText;

    private int playerMaxHp = 100;

    private void Awake()
    {
        if (playerHpSlider != null)
        {
            playerMaxHp = Mathf.Max(1, Mathf.RoundToInt(playerHpSlider.maxValue));
        }
    }

    private void Update()
    {
        RefreshPlayerUi();
        RefreshBossUi();
    }

    private void RefreshPlayerUi()
    {
        if (GameManager.Instance == null || playerHpSlider == null) return;

        int currentHp = Mathf.Clamp(GameManager.Instance.PlayerHP, 0, playerMaxHp);

        playerHpSlider.minValue = 0f;
        playerHpSlider.maxValue = playerMaxHp;
        playerHpSlider.value = currentHp;
        SetText(playerHpText, $"{currentHp} / {playerMaxHp}");
    }

    private void RefreshBossUi()
    {
        if (bossHpSlider == null || bossController == null) return;

        int maxHp = Mathf.Max(1, bossController.maxHP);
        int currentHp = Mathf.Clamp(bossController.CurrentHP, 0, maxHp);

        bossHpSlider.minValue = 0f;
        bossHpSlider.maxValue = maxHp;
        bossHpSlider.value = currentHp;
        SetText(bossHpText, $"{currentHp} / {maxHp}");
    }

    private void SetText(Text legacyText, string value)
    {
        if (legacyText != null)
        {
            legacyText.text = value;
        }
    }
}
