using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardSlot : MonoBehaviour
{
    public CardData currentCard;
    public bool isSelected = false;

    [Header("UI References")]
    public Image cardIcon;
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI descriptionText;
    public GameObject highlightObj; // 선택 시 켜질 테두리 이미지

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
}
