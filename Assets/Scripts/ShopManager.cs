using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    [Header("상점에 등장 가능한 카드")]
    [SerializeField] private List<CardData> cardPool;

    [Header("상점 슬롯 5개")]
    [SerializeField] private List<ShopSlot> shopSlots;

    private void Start()
    {
        CreateShop();
    }

    public void CreateShop()
    {
        if (cardPool == null || cardPool.Count == 0)
        {
            Debug.LogError("Card Pool이 비어 있습니다.");
            return;
        }

        if (shopSlots == null || shopSlots.Count == 0)
        {
            Debug.LogError("Shop Slot이 연결되지 않았습니다.");
            return;
        }

        // 원본 카드 목록을 변경하지 않기 위해 복사
        List<CardData> remainingCards =
            new List<CardData>(cardPool);

        for (int i = 0; i < shopSlots.Count; i++)
        {
            if (remainingCards.Count == 0)
            {
                Debug.LogWarning("슬롯 수보다 카드 수가 부족합니다.");
                break;
            }

            int randomIndex =
                Random.Range(0, remainingCards.Count);

            CardData selectedCard =
                remainingCards[randomIndex];

            // 뽑은 카드를 해당 슬롯에 전달
            shopSlots[i].SetCard(selectedCard);

            // 같은 카드가 중복으로 나오지 않게 제거
            remainingCards.RemoveAt(randomIndex);
        }
    }
}