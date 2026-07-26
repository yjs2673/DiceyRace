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

        inventoryButton.onClick.AddListener(HandleInventoryButtonClick);
        returnButton.onClick.AddListener(HandleReturnButtonClick);
        cardButton.onClick.AddListener(HandleCardButtonClick);
        diceButton.onClick.AddListener(HandleDiceButtonClick);
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
            inventoryButton.onClick.RemoveListener(HandleInventoryButtonClick);

        if (returnButton != null)
            returnButton.onClick.RemoveListener(HandleReturnButtonClick);

        if (cardButton != null)
            cardButton.onClick.RemoveListener(HandleCardButtonClick);

        if (diceButton != null)
            diceButton.onClick.RemoveListener(HandleDiceButtonClick);
    }

    private void HandleInventoryButtonClick()
    {
        AudioManager.instance.PlaySfx(AudioManager.Sfx.ButtonClick); //***
        ShowInventory();
    }

    private void HandleReturnButtonClick()
    {
        AudioManager.instance.PlaySfx(AudioManager.Sfx.ButtonClick); //***
        ShowShop();
    }

    private void HandleCardButtonClick()
    {
        AudioManager.instance.PlaySfx(AudioManager.Sfx.ButtonClick); //***
        ShowCards();
    }

    private void HandleDiceButtonClick()
    {
        AudioManager.instance.PlaySfx(AudioManager.Sfx.ButtonClick); //***
        ShowDice();
    }
}
