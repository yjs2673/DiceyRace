using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CardManager : MonoBehaviour
{
    public static CardManager Instance { get; private set; }

    [Header("Card Database")]
    [Tooltip("게임에 존재하는 모든 카드 에셋을 여기에 넣어주세요.")]
    public List<CardData> allAvailableCards;

    [Header("Mulligan UI")]
    public GameObject mulliganPanel; // 멀리건 화면 전체 패널
    public CardSlot[] cardSlots;     // 화면에 배치된 4개의 카드 슬롯
    public Button nextButton;        // 다음/완료 버튼

    [Header("Deck UI (In-Game)")]
    public GameObject deckPanel;     // 화면 하단에 띄울 내 카드 목록 패널
    public CardSlot[] deckSlots;     // 하단에 배치된 4개의 카드 슬롯 (프리팹 재사용)

    [Header("Managers Reference")]
    public DiceManager diceManager;
    public PlayerController playerController;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (mulliganPanel != null) mulliganPanel.SetActive(false);
        if (deckPanel != null) deckPanel.SetActive(false);
    }

    private void Start()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextButtonClicked);
    }

    private void Update()
    {
        // Standby 또는 Move 페이즈일 때만 숫자 1~4번 키로 액티브 카드 사용 가능
        TurnPhase phase = TurnManager.Instance.CurrentPhase;
        if (phase == TurnPhase.Standby || phase == TurnPhase.Move)
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame) UseActiveCard(0);
                if (Keyboard.current.digit2Key.wasPressedThisFrame) UseActiveCard(1);
                if (Keyboard.current.digit3Key.wasPressedThisFrame) UseActiveCard(2);
                if (Keyboard.current.digit4Key.wasPressedThisFrame) UseActiveCard(3);
            }
        }
    }

    // TurnManager에서 TurnPhase.Mulligan이 될 때 호출할 함수
    public void StartMulligan()
    {
        Debug.Log("멀리건 페이즈 시작!");
        mulliganPanel.SetActive(true);
        deckPanel.SetActive(false);

        // 최초 4장 랜덤 배치 및 초기화
        foreach (var slot in cardSlots)
        {
            slot.ResetSlot();
            slot.SetCard(GetRandomCard());
        }
    }

    private void OnNextButtonClicked()
    {
        int selectedCount = 0;

        // 몇 장이 선택되었는지 체크
        foreach (var slot in cardSlots)
        {
            if (slot.isSelected) selectedCount++;
        }

        // 4장 모두 선택되었다면 멀리건 완료 (게임 진행)
        if (selectedCount == 4)
        {
            FinishMulligan();
        }
        else
        {
            // 4장이 아니라면 선택되지 않은 카드만 리롤
            Debug.Log($"현재 {selectedCount}장 선택됨. 나머지를 리롤합니다.");
            foreach (var slot in cardSlots)
            {
                if (!slot.isSelected)
                {
                    slot.ResetSlot(); // 하이라이트 끄기 방어코드
                    slot.SetCard(GetRandomCard());
                }
            }
        }
    }

    // 멀리건 종료 및 다음 페이즈 이동
    private void FinishMulligan()
    {
        mulliganPanel.SetActive(false);

        List<CardData> selected = new List<CardData>();
        foreach (var slot in cardSlots)
        {
            if (slot.isSelected) selected.Add(slot.currentCard);
        }

        // 액티브 카드가 무조건 먼저 오고, 그 다음 ID 오름차순 정렬 -> 나중에 그냥 id순으로 정렬하면 될듯
        selected.Sort((a, b) =>
        {
            if (a.cardType != b.cardType)
            {
                return b.cardType.CompareTo(a.cardType); // Enum 구조상 Active(1)가 Passive(0)보다 큼
            }
            return a.cardID.CompareTo(b.cardID);
        });

        // 하단 덱 UI 켜기 및 세팅
        deckPanel.SetActive(true);
        for (int i = 0; i < deckSlots.Length; i++)
        {
            if (i < selected.Count)
            {
                GameManager.Instance.AddCard(selected[i]);
                deckSlots[i].gameObject.SetActive(true);
                deckSlots[i].SetCard(selected[i]);
            }
            else
            {
                deckSlots[i].gameObject.SetActive(false);
            }
        }

        TurnManager.Instance.SetPhase(TurnPhase.Standby);
    }

    // 패시브 발동: Standby 진입 시 TurnManager가 호출
    public void ActivateAndRemovePassives()
    {
        // 배열을 역순으로 돌면서 패시브만 찾아 발동 후 제거
        for (int i = deckSlots.Length - 1; i >= 0; i--)
        {
            CardSlot slot = deckSlots[i];
            if (slot.gameObject.activeSelf && slot.currentCard != null)
            {
                if (slot.currentCard.cardType == CardType.Passive)
                {
                    Debug.Log($"패시브 자동 발동: {slot.currentCard.cardName}");
                    EffectProcessor.ApplyCardEffect(slot.currentCard, diceManager, playerController);
                    
                    GameManager.Instance.RemoveCard(slot.currentCard);
                    slot.gameObject.SetActive(false);
                    slot.currentCard = null;
                }
            }
        }
    }

    // 액티브 사용: 1~4 숫자키 누를 때 발동
    private void UseActiveCard(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= deckSlots.Length) return;
        
        CardSlot slot = deckSlots[slotIndex];
        if (!slot.gameObject.activeSelf || slot.currentCard == null) return;
        
        if (slot.currentCard.cardType != CardType.Active)
        {
            Debug.Log("이 카드는 액티브 카드가 아니거나 이미 사용되었습니다.");
            return;
        }

        Debug.Log($"액티브 수동 발동: {slot.currentCard.cardName}");
        EffectProcessor.ApplyCardEffect(slot.currentCard, diceManager, playerController);
        
        // 사용한 카드 버리기
        GameManager.Instance.RemoveCard(slot.currentCard);
        slot.gameObject.SetActive(false);
        slot.currentCard = null;
    }

    // 턴 종료: End 진입 시 남은 카드 싹 다 치우기
    public void ClearDeckUI()
    {
        deckPanel.SetActive(false);
        foreach (var slot in deckSlots)
        {
            slot.gameObject.SetActive(false);
            slot.currentCard = null;
        }
    }

    // 풀에서 랜덤한 카드 한 장 가져오기 (중복 처리 로직은 필요에 따라 추가 가능)
    private CardData GetRandomCard()
    {
        if (allAvailableCards == null || allAvailableCards.Count == 0)
        {
            Debug.LogError("CardManager에 등록된 카드가 없습니다!");
            return null;
        }

        int randomIndex = Random.Range(0, allAvailableCards.Count);
        return allAvailableCards[randomIndex];
    }
}