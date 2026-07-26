using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class RerollButton : MonoBehaviour
{
   [SerializeField] private ShopManager shopManager;
   [SerializeField] private InfoPanelUI infoPanelUI;

   [Header("Reroll Price")]
   [SerializeField] private TMP_Text rerollPriceText;
   [SerializeField] private int initialRerollPrice = 5;
   [SerializeField] private int rerollPriceIncrease = 2;

   private Button button;
   private int currentRerollPrice;

    private void Awake()
    {
        button = GetComponent<Button>();
        currentRerollPrice = initialRerollPrice;

        if (rerollPriceText == null)
        {
            Transform rerollPriceTransform = transform.Find("RerollPrice");
            if (rerollPriceTransform != null)
                rerollPriceText = rerollPriceTransform.GetComponent<TMP_Text>();
        }

        button.onClick.AddListener(RerollShop);
    }
    private void Start()
    {
        RefreshButton();
    }
    private void RerollShop()
    {
        AudioManager.instance.PlaySfx(AudioManager.Sfx.ButtonClick); //***
        if(shopManager == null)
        {
            Debug.LogError("ShopManager가 존재하지 않습니다.");
            return;
        }
        if(GameManager.Instance == null)
        {
            Debug.LogError("GameManager가 존재하지 않습니다.");
            return;
        }
        if (GameManager.Instance.Coin < currentRerollPrice)
        {
            Debug.Log("골드가 부족합니다.");
            RefreshButton();
            return;
        }
        if (!GameManager.Instance.SpendCoin(currentRerollPrice))
        {
            RefreshButton();
            return;
        }
        if (!GameManager.Instance.UseReroll())
        {
            GameManager.Instance.AddCoin(currentRerollPrice);
            Debug.Log("남은 리롤 횟수가 없습니다.");
            RefreshButton();
            return;
        }

        shopManager.CreateShop();
        currentRerollPrice += rerollPriceIncrease;

        if(infoPanelUI != null)
            infoPanelUI.GetCoinData();
        RefreshButton();
    }
    private void RefreshButton()
    {
        if(GameManager.Instance == null)
        {
            Debug.LogError("GameManager가 존재하지 않습니다.");
            return;
        }

        if (rerollPriceText != null)
            rerollPriceText.text = $"{currentRerollPrice}";

        button.interactable =
            GameManager.Instance.Reroll > 0 &&
            GameManager.Instance.Coin >= currentRerollPrice;
    }
    private void OnDestroy()
    {
        if(button != null)
        {
            button.onClick.RemoveListener(RerollShop);
        }
    }
}
