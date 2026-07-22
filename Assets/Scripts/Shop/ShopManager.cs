using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [Header("상점에 등장 가능한 카드")]
    [SerializeField] private List<CardData> cardPool;
    [Header("상점에 등장 가능한 주사위")]
    [SerializeField] private List<Dice> dicePool;

    [Header("Shopslot 리스트")]
    [SerializeField] private List<ShopSlot> shopSlots;

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
        foreach (ShopSlot slot in shopSlots)
        {
            if (slot != null)
                slot.SetEmpty();
        }
        // ���� ī�� ����� �������� �ʱ� ���� ����
        List<ScriptableObject> remainingItems = new List<ScriptableObject>();

        if (cardPool != null)
        {
            foreach (CardData card in cardPool)
            {
                if (card != null)
                    remainingItems.Add(card);
            }
        }

        if (dicePool != null)
        {
            foreach (Dice dice in dicePool)
            {
                if (dice != null)
                    remainingItems.Add(dice);
            }
        }

        if (remainingItems.Count == 0)
        {
            Debug.LogError("상점에 표시할 카드와 주사위가 없습니다.");
            return;
        }

        for (int i = 0; i < shopSlots.Count; i++)
        {
            if (remainingItems.Count == 0)
                break;

            int randomIndex = Random.Range(0, remainingItems.Count);
            ScriptableObject selectedItem = remainingItems[randomIndex];

            if (selectedItem is CardData card)
                shopSlots[i].SetCard(card);
            else if (selectedItem is Dice dice)
                shopSlots[i].SetDice(dice);

            remainingItems.RemoveAt(randomIndex);
        }
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
