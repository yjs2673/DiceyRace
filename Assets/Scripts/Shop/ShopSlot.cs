using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShopSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Shopslot Text UI")]
    [SerializeField] private TMP_Text soldoutText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Image coinImage;
    [SerializeField] private Image pirateCoinImage;
    [SerializeField] private Image itemImage;

    private Button buyButton;

    private CardData currentCard;
    private Dice currentDice;

    private void Awake()
    {
        ResolveItemImage();

        buyButton = GetComponent<Button>();
        if (itemImage != null)
            buyButton.targetGraphic = itemImage;

        buyButton.onClick.AddListener(BuyItem);
    }

    private void ResolveItemImage()
    {
        if (itemImage != null)
            return;

        Transform itemImageTransform = transform.Find("ItemImage");
        if (itemImageTransform != null)
            itemImage = itemImageTransform.GetComponent<Image>();

        if (itemImage == null)
            Debug.LogError($"{name}의 ItemImage 참조를 찾을 수 없습니다.", this);
    }

    private void BuyItem()
    {
        if (currentCard == null && currentDice == null)
            return;

        AudioManager.instance.PlaySfx(AudioManager.Sfx.ButtonClick); //***

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

        if (currentCard != null)
        {
            if (!GameManager.Instance.SpendCoin(price))
            {
                Debug.Log("골드가 부족합니다.");
                return;
            }
        }
        else
        {
            if (!GameManager.Instance.SpendPirateCoin(price))
            {
                Debug.Log("해적주화가 부족합니다.");
                return;
            }
        }

        if (currentCard != null)
            GameManager.Instance.AddOwnedCard(currentCard);
        else
            GameManager.Instance.AddDice(currentDice);
        Debug.Log($"{itemName}을 구매했습니다.");
        AudioManager.instance.PlaySfx(AudioManager.Sfx.ShopBuy); //***

        currentCard = null;
        currentDice = null;

        priceText.gameObject.SetActive(false);
        coinImage.gameObject.SetActive(false);
        pirateCoinImage.gameObject.SetActive(false);
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

        if (itemImage != null)
        {
            itemImage.sprite = card.icon;
            itemImage.preserveAspect = true;
        }

        priceText.text = $"{card.price}";
        priceText.gameObject.SetActive(true);
        coinImage.gameObject.SetActive(true);
        soldoutText.gameObject.SetActive(false);
        buyButton.interactable = true;
    }

    public void SetDice(Dice dice)
    {
        if (dice == null)
            return;

        currentDice = dice;
        currentCard = null;
        if (itemImage != null)
        {
            itemImage.sprite = dice.icon;
            itemImage.preserveAspect = true;
        }

        priceText.text = $"{dice.price}";
        priceText.gameObject.SetActive(true);
        pirateCoinImage.gameObject.SetActive(true);
        soldoutText.gameObject.SetActive(false);
        buyButton.interactable = true;
    }

    public void SetEmpty()
    {
        currentCard = null;
        currentDice = null;
        if (itemImage != null)
            itemImage.sprite = null;
        priceText.gameObject.SetActive(false);
        coinImage.gameObject.SetActive(false);
        pirateCoinImage.gameObject.SetActive(false);
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
