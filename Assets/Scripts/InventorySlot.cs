using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private Image iconImage;

    private CardData currentCard;
    private Dice currentDice;

    public void SetCard(CardData card)
    {
        if (card == null)
            return;

        currentCard = card;
        currentDice = null;

        iconImage.sprite = card.icon;
        iconImage.preserveAspect = true;
    }

    public void SetDice(Dice dice)
    {
        if (dice == null)
            return;

        currentDice = dice;
        currentCard = null;

        iconImage.sprite = dice.icon;
        iconImage.preserveAspect = true;
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
        HideTooltip();
    }

    private void OnDisable()
    {
        HideTooltip();
    }

    private void HideTooltip()
    {
        if (ShopTooltipUI.Instance != null)
            ShopTooltipUI.Instance.Hide();
    }
}