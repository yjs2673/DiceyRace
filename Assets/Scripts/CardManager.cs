using System.Collections.Generic;
using UnityEngine;
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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        mulliganPanel.SetActive(false);
    }

    private void Start()
    {
        // 넥스트 버튼 클릭 이벤트 연결
        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextButtonClicked);
            
        // mulliganPanel.SetActive(false);
    }

    // TurnManager에서 TurnPhase.Mulligan이 될 때 호출할 함수
    public void StartMulligan()
    {
        Debug.Log("멀리건 페이즈 시작!");
        mulliganPanel.SetActive(true);

        // 최초 4장 랜덤 배치 및 초기화
        foreach (var slot in cardSlots)
        {
            slot.ResetSlot();
            slot.SetCard(GetRandomCard());
        }
    }

    // '다음' 버튼 클릭 시 기획된 로직 실행
    private void OnNextButtonClicked()
    {
        int selectedCount = 0;

        // 1. 몇 장이 선택되었는지 체크
        foreach (var slot in cardSlots)
        {
            if (slot.isSelected) selectedCount++;
        }

        // 2. 4장 모두 선택되었다면 멀리건 완료 (게임 진행)
        if (selectedCount == 4)
        {
            FinishMulligan();
        }
        else
        {
            // 3. 4장이 아니라면 선택되지 않은 카드만 리롤
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
        Debug.Log("멀리건 완료! 덱에 카드를 추가합니다.");
        
        mulliganPanel.SetActive(false); // UI 숨기기

        // GameManager의 덱 리스트에 선택된 4장 추가
        foreach (var slot in cardSlots)
        {
            GameManager.Instance.AddCard(slot.currentCard);
        }

        // TODO: 향후 인게임 UI 하단에 내 덱(GameManager.Instance.HasCard) 표시 기능 추가

        // 턴 매니저에게 다음 페이즈(Standby)로 넘어가라고 신호 발송
        TurnManager.Instance.SetPhase(TurnPhase.Standby);
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