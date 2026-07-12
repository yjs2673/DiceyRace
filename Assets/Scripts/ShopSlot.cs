using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopSlot : MonoBehaviour
{
    [Header("슬롯 UI")]
    [SerializeField] private TMP_Text soldoutText;
    [SerializeField] private TMP_Text priceText;

    private Image cardImage;
    private Button buyButton;

    // 현재 슬롯에 들어 있는 카드
    private CardData currentCard;

    private void Awake()
    {
        cardImage = GetComponent<Image>();
        buyButton = GetComponent<Button>();
    }

    public void SetCard(CardData card)
    {
        if (card == null)
        {
            Debug.LogError("ShopSlot에 전달된 CardData가 없습니다.");
            return;
        }

        currentCard = card;

        // 부모 ShopSlot의 Image에 카드 이미지 넣기
        cardImage.sprite = card.icon;
        cardImage.preserveAspect = true;

        // 가격 표시
        priceText.text = $"{card.price}G";

        // 판매 전 상태
        priceText.gameObject.SetActive(true);
        soldoutText.gameObject.SetActive(false);

        // 클릭 가능
        buyButton.interactable = true;
    }

    public void SetEmpty()
    {
        currentCard = null;

        cardImage.sprite = null;
        priceText.gameObject.SetActive(false);
        soldoutText.gameObject.SetActive(false);

        buyButton.interactable = false;
    }
}