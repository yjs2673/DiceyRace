using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    [Header("상점에 등장 가능한 카드")]
    [SerializeField] private List<CardData> cardPool;
    [Header("상점에 등장 가능한 주사위")]
    [SerializeField] private List<Dice> dicePool;

    [Header("Shopslot 리스트")]
    [SerializeField] private List<ShopSlot> shopSlots;
    [SerializeField] private Button moveButton;

    public event System.Action AvailabilityChanged;

    public bool AreAllSlotsSoldOut
    {
        get
        {
            if (shopSlots == null || shopSlots.Count == 0)
                return false;

            int validSlotCount = 0;

            foreach (ShopSlot slot in shopSlots)
            {
                if (slot == null)
                    continue;

                validSlotCount++;
                if (!slot.IsSoldOut)
                    return false;
            }

            return validSlotCount > 0;
        }
    }

    private void Awake()
    {
        ResolveMoveButton();
        BindMoveButton();
    }

    private void OnEnable()
    {
        SetSlotEventSubscriptions(true);
    }

    private void OnDisable()
    {
        SetSlotEventSubscriptions(false);
    }

    private void OnDestroy()
    {
        if (moveButton != null)
        {
            moveButton.onClick.RemoveListener(ReturnToField);
        }
    }

    private void Start()
    {
        CreateShop();
    }

    public void CreateShop()
    {
        if (shopSlots == null || shopSlots.Count == 0)
        {
            Debug.LogError("Shop Slot을 불러올 수 없습니다.");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager가 존재하지 않습니다.", this);
            return;
        }

        foreach (ShopSlot slot in shopSlots)
        {
            if (slot == null || slot.IsSoldOut)
                continue;

            slot.SetEmpty();
        }

        List<ScriptableObject> remainingItems = new List<ScriptableObject>();
        HashSet<ScriptableObject> uniqueItems = new HashSet<ScriptableObject>();
        HashSet<CardData> ownedCards = new HashSet<CardData>(GameManager.Instance.HasCard);
        HashSet<Dice> ownedDice = new HashSet<Dice>(GameManager.Instance.HasDice);

        if (cardPool != null)
        {
            foreach (CardData card in cardPool)
            {
                if (card != null &&
                    !ownedCards.Contains(card) &&
                    uniqueItems.Add(card))
                {
                    remainingItems.Add(card);
                }
            }
        }

        if (dicePool != null)
        {
            foreach (Dice dice in dicePool)
            {
                if (dice != null &&
                    !ownedDice.Contains(dice) &&
                    uniqueItems.Add(dice))
                {
                    remainingItems.Add(dice);
                }
            }
        }

        for (int i = 0; i < shopSlots.Count; i++)
        {
            ShopSlot slot = shopSlots[i];
            if (slot == null || slot.IsSoldOut)
                continue;

            if (remainingItems.Count == 0)
            {
                slot.SetSoldOut();
                continue;
            }

            int randomIndex = Random.Range(0, remainingItems.Count);
            ScriptableObject selectedItem = remainingItems[randomIndex];

            if (selectedItem is CardData card)
                slot.SetCard(card);
            else if (selectedItem is Dice dice)
                slot.SetDice(dice);

            remainingItems.RemoveAt(randomIndex);
        }

        AvailabilityChanged?.Invoke();
    }

    private void HandleSlotSoldOut()
    {
        AvailabilityChanged?.Invoke();
    }

    private void SetSlotEventSubscriptions(bool subscribe)
    {
        if (shopSlots == null)
            return;

        foreach (ShopSlot slot in shopSlots)
        {
            if (slot == null)
                continue;

            slot.SoldOut -= HandleSlotSoldOut;
            if (subscribe)
                slot.SoldOut += HandleSlotSoldOut;
        }
    }

    private void ResolveMoveButton()
    {
        if (moveButton != null)
        {
            return;
        }

        GameObject moveButtonObject = GameObject.Find("MoveButton");
        if (moveButtonObject != null)
        {
            moveButton = moveButtonObject.GetComponent<Button>();
        }
    }

    private void BindMoveButton()
    {
        if (moveButton == null)
        {
            Debug.LogWarning("상점 나가기 버튼을 찾지 못했습니다.", this);
            return;
        }

        moveButton.onClick = new Button.ButtonClickedEvent();
        moveButton.onClick.AddListener(ReturnToField);
    }

    public void ReturnToField()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager가 존재하지 않습니다.");
            return;
        }

        GameManager.Instance.ReturnToSavedFieldScene();
    }
}
