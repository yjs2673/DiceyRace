using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardSlot : MonoBehaviour
{
    public CardData currentCard;
    public bool isSelected = false;

    [Header("UI References")]
    public Image cardIcon;
    [SerializeField] private Image typeImage;
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI descriptionText;
    public GameObject highlightObj; // 선택 시 켜질 테두리 이미지

    private Sprite passiveTypeSprite;
    private Sprite activeTypeSprite;

    private void Awake()
    {
        if (typeImage == null)
        {
            typeImage = FindChildImageByName(transform, "TypeImage");
        }
    }

    public void SetTypeSprites(Sprite passiveSprite, Sprite activeSprite)
    {
        passiveTypeSprite = passiveSprite;
        activeTypeSprite = activeSprite;
        RefreshTypeImage();
    }

    // 카드를 슬롯에 세팅하는 함수
    public void SetCard(CardData card)
    {
        currentCard = card;
        
        if (card != null)
        {
            if (cardIcon != null) cardIcon.sprite = card.icon;
            if (cardNameText != null) cardNameText.text = card.cardName;
            if (descriptionText != null) descriptionText.text = card.description;
        }

        RefreshTypeImage();
    }

    // 초기화 (리롤될 때 선택 상태 해제)
    public void ResetSlot()
    {
        SetSelected(false);
    }

    // UI 버튼의 OnClick 이벤트에 연결할 함수
    public void ToggleSelect()
    {
        SetSelected(!isSelected);
        CardManager.Instance?.HandleMulliganSelectionChanged();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (highlightObj != null) highlightObj.SetActive(isSelected);
    }

    private void RefreshTypeImage()
    {
        if (typeImage == null)
        {
            return;
        }

        if (currentCard == null)
        {
            typeImage.gameObject.SetActive(false);
            return;
        }

        Sprite spriteToUse = currentCard.cardType == CardType.Active
            ? activeTypeSprite
            : passiveTypeSprite;

        typeImage.gameObject.SetActive(spriteToUse != null);
        if (spriteToUse != null)
        {
            typeImage.sprite = spriteToUse;
        }
    }

    private static Image FindChildImageByName(Transform root, string childName)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
            {
                return child.GetComponent<Image>();
            }

            Image nestedImage = FindChildImageByName(child, childName);
            if (nestedImage != null)
            {
                return nestedImage;
            }
        }

        return null;
    }
}
