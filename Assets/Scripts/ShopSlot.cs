using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShopSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Shopslot Text UI")]
    [SerializeField] private TMP_Text soldoutText;
    [SerializeField] private TMP_Text priceText;

    private Image cardImage;
    private Button buyButton;

    private CardData currentCard;
    private Dice currentDice;

    private void Awake()
    {
        cardImage = GetComponent<Image>();
        buyButton = GetComponent<Button>();
        buyButton.onClick.AddListener(BuyItem);
    }

    private void BuyItem()
    {
        if (currentCard == null && currentDice == null)
            return;

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager가 존재하지 않습니다.");
            return;
        }

        int price; string itemName;

        if (currentCard != null)
        {
            price = currentCard.price;
            itemName = currentCard.cardName;
        }
        else
        {
            price = currentDice.price;
            itemName = currentDice.diceName;
        }
        if (!GameManager.Instance.SpendCoin(price))
        {
            Debug.Log("골드가 부족합니다.");
            return;
        }
        if (currentCard != null)
            GameManager.Instance.AddCard(currentCard);
        else
            GameManager.Instance.AddDice(currentDice);
        Debug.Log($"{itemName}을 구매했습니다.");

        currentCard = null;
        currentDice = null;

        priceText.gameObject.SetActive(false);
        soldoutText.gameObject.SetActive(true);
        buyButton.interactable = false;

        InfoPanelUI infoPanel = FindFirstObjectByType<InfoPanelUI>();
        if (infoPanel != null)
            infoPanel.GetCoinData();

        if (ShopTooltipUI.Instance != null)
            ShopTooltipUI.Instance.Hide();
    }
    public void SetCard(CardData card)
    {
        if (card == null)
        {
            return;
        }

        currentCard = card;
        currentDice = null;

        cardImage.sprite = card.icon;
        cardImage.preserveAspect = true;

        priceText.text = $"{card.price}G";
        priceText.gameObject.SetActive(true);
        soldoutText.gameObject.SetActive(false);
        buyButton.interactable = true;
    }

    public void SetDice(Dice dice)
    {
        if (dice == null)
            return;

        currentDice = dice;
        currentCard = null;
        cardImage.sprite = dice.icon;
        cardImage.preserveAspect = true;

        priceText.text = $"{dice.price}G";
        priceText.gameObject.SetActive(true);
        soldoutText.gameObject.SetActive(false);
        buyButton.interactable = true;
    }

    public void SetEmpty()
    {
        currentCard = null;
        currentDice = null;
        cardImage.sprite = null;
        priceText.gameObject.SetActive(false);
        soldoutText.gameObject.SetActive(false);
        buyButton.interactable = false;
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ShopTooltipUI.Instance == null)
            return;

        if (currentCard != null)
        {
            ShopTooltipUI.Instance.Show(
                currentCard.cardName,
                currentCard.description
            );
        }
        else if (currentDice != null)
        {
            ShopTooltipUI.Instance.Show(
                currentDice.diceName,
                currentDice.description
            );
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ShopTooltipUI.Instance != null)
            ShopTooltipUI.Instance.Hide();
    }

    private void OnDisable()
    {
        if (ShopTooltipUI.Instance != null)
            ShopTooltipUI.Instance.Hide();
    }
    private void OnDestroy()
    {
        if (buyButton != null)
            buyButton.onClick.RemoveListener(BuyItem);
    }
}