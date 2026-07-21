using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUIController : MonoBehaviour
{
    [Header("Panel Switching")]
    [SerializeField] private GameObject shopRoot;
    [SerializeField] private GameObject inventoryRoot;
    [SerializeField] private Button inventoryButton;
    [SerializeField] private Button returnButton;

    [Header("Inventory Items")]
    [SerializeField] private Transform content;
    [SerializeField] private InventorySlot inventorySlotPrefab;
    [SerializeField] private Button cardButton;
    [SerializeField] private Button diceButton;

    private void Awake()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        inventoryButton.onClick.AddListener(ShowInventory);
        returnButton.onClick.AddListener(ShowShop);
        cardButton.onClick.AddListener(ShowCards);
        diceButton.onClick.AddListener(ShowDice);
    }

    private void Start()
    {
        ShowShop();
    }

    public void ShowInventory()
    {
        shopRoot.SetActive(true);
        inventoryRoot.SetActive(true);
        inventoryButton.gameObject.SetActive(false);
        ShowCards();
    }

    public void ShowShop()
    {
        inventoryRoot.SetActive(false);
        shopRoot.SetActive(true);
        inventoryButton.gameObject.SetActive(true);

        if (ShopTooltipUI.Instance != null)
            ShopTooltipUI.Instance.Hide();
    }

    public void ShowCards()
    {
        ClearSlots();

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager가 존재하지 않습니다.", this);
            return;
        }

        CreateCardSlots(GameManager.Instance.HasCard);
    }

    public void ShowDice()
    {
        ClearSlots();

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager가 존재하지 않습니다.", this);
            return;
        }

        CreateDiceSlots(GameManager.Instance.HasDice);
    }

    private void CreateCardSlots(IReadOnlyList<CardData> cards)
    {
        HashSet<CardData> displayedCards = new HashSet<CardData>();

        foreach (CardData card in cards)
        {
            if (card == null || !displayedCards.Add(card))
                continue;

            foreach (CardData ownedCard in cards)
            {
                if (ownedCard != card)
                    continue;

                InventorySlot slot = Instantiate(inventorySlotPrefab, content);
                slot.SetCard(ownedCard);
            }
        }
    }

    private void CreateDiceSlots(IReadOnlyList<Dice> diceList)
    {
        HashSet<Dice> displayedDice = new HashSet<Dice>();

        foreach (Dice dice in diceList)
        {
            if (dice == null || !displayedDice.Add(dice))
                continue;

            foreach (Dice ownedDice in diceList)
            {
                if (ownedDice != dice)
                    continue;

                InventorySlot slot = Instantiate(inventorySlotPrefab, content);
                slot.SetDice(ownedDice);
            }
        }
    }

    private void ClearSlots()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            GameObject slotObject = content.GetChild(i).gameObject;
            slotObject.SetActive(false);
            Destroy(slotObject);
        }
    }

    private bool ValidateReferences()
    {
        if (shopRoot != null && inventoryRoot != null &&
            inventoryButton != null && returnButton != null &&
            content != null && inventorySlotPrefab != null &&
            cardButton != null && diceButton != null)
        {
            return true;
        }

        Debug.LogError("InventoryUIController의 UI 참조가 연결되지 않았습니다.", this);
        return false;
    }

    private void OnDestroy()
    {
        if (inventoryButton != null)
            inventoryButton.onClick.RemoveListener(ShowInventory);

        if (returnButton != null)
            returnButton.onClick.RemoveListener(ShowShop);

        if (cardButton != null)
            cardButton.onClick.RemoveListener(ShowCards);

        if (diceButton != null)
            diceButton.onClick.RemoveListener(ShowDice);
    }
}
