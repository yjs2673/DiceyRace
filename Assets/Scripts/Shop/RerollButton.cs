using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class RerollButton : MonoBehaviour
{
   private const float UnavailableAlpha = 0.5f;

   [SerializeField] private ShopManager shopManager;
   [SerializeField] private InfoPanelUI infoPanelUI;

   [Header("Reroll Price")]
   [SerializeField] private TMP_Text rerollPriceText;
   [SerializeField] private int initialRerollPrice = 5;
   [SerializeField] private int rerollPriceIncrease = 2;

   private Button button;
   private int currentRerollPrice;
   private Color availablePriceColor = Color.white;
   private ColorBlock defaultButtonColors;
   private CanvasGroup buttonCanvasGroup;

    private void Awake()
    {
        button = GetComponent<Button>();
        buttonCanvasGroup = GetComponent<CanvasGroup>();
        if (buttonCanvasGroup == null)
            buttonCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        defaultButtonColors = button.colors;
        currentRerollPrice = initialRerollPrice;

        if (rerollPriceText == null)
        {
            Transform rerollPriceTransform = transform.Find("RerollPrice");
            if (rerollPriceTransform != null)
                rerollPriceText = rerollPriceTransform.GetComponent<TMP_Text>();
        }

        if (rerollPriceText != null)
            availablePriceColor = rerollPriceText.color;

        button.onClick.AddListener(RerollShop);
    }

    private void OnEnable()
    {
        if (shopManager != null)
            shopManager.AvailabilityChanged += RefreshButton;
    }

    private void OnDisable()
    {
        if (shopManager != null)
            shopManager.AvailabilityChanged -= RefreshButton;
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

        bool hasShopManager = shopManager != null;
        bool allSlotsSoldOut =
            hasShopManager &&
            shopManager.AreAllSlotsSoldOut;
        bool canReroll =
            hasShopManager &&
            !allSlotsSoldOut &&
            GameManager.Instance.Coin >= currentRerollPrice;

        if (rerollPriceText != null)
            rerollPriceText.color = availablePriceColor;

        ColorBlock buttonColors = defaultButtonColors;
        buttonColors.disabledColor = defaultButtonColors.normalColor;

        button.colors = buttonColors;
        button.interactable = canReroll;
        buttonCanvasGroup.alpha = canReroll ? 1f : UnavailableAlpha;
    }
    private void OnDestroy()
    {
        if(button != null)
        {
            button.onClick.RemoveListener(RerollShop);
        }
    }
}
