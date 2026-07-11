using UnityEngine;
using System;

// 게임의 메인 턴 흐름
public enum TurnPhase
{
    Mulligan, // 턴 시작, 랜덤 카드 뽑고 선택하기
    Standby,  // 주사위 굴리기 대기 (패시브 발동 구간)
    Move,     // 주사위 굴린 후 이동 및 액티브 사용 구간
    End       // 정지 발판 처리 완료, 카드 무덤으로 버리기
}

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    public TurnPhase CurrentPhase { get; private set; }
    
    // 페이즈가 변경될 때 다른 스크립트들에게 알려주는 이벤트(Action)
    public Action<TurnPhase> OnPhaseChanged; 

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // 게임 씬이 시작되면 멀리건 페이즈부터 시작
        SetPhase(TurnPhase.Mulligan);
    }

    // 페이즈 전환은 반드시 이 함수를 통해서만 진행
    public void SetPhase(TurnPhase newPhase)
    {
        CurrentPhase = newPhase;
        Debug.Log($"[턴 매니저] 페이즈 변경 -> {newPhase}");
        
        // 구독(Subscribe)중인 다른 매니저들에게 알림 발송
        OnPhaseChanged?.Invoke(newPhase);

        switch (newPhase)
        {
            case TurnPhase.Mulligan:
                // TODO: CardManager에게 멀리건 UI 띄우기 명령
                if (CardManager.Instance != null) 
                    CardManager.Instance.StartMulligan();
                break;

            case TurnPhase.Standby:
                // TODO: 패시브 카드 효과들을 읽어서 PlayerController에 적용
                // TODO: DiceManager의 주사위 굴리기 버튼 활성화
                break;

            case TurnPhase.Move:
                // (DiceManager에서 주사위를 굴릴 때 진입됨)
                // 액티브 카드 사용 잠금 등
                break;

            case TurnPhase.End:
                // 턴 정리 로직
                CleanUpTurn();
                break;
        }
    }

    private void CleanUpTurn()
    {
        // 8번 흐름: 소유한 카드를 모두 버리고(무덤으로) 다음 턴으로 진행
        // (GameManager.Instance.HasCard를 비우는 작업)
        Debug.Log("턴 종료! 소지한 카드를 모두 무덤으로 보냅니다.");
        
        // TODO: GameManager에 ClearCards() 같은 함수 추가 필요

        // 턴 정리가 끝나면 다시 새로운 턴(멀리건) 시작
        SetPhase(TurnPhase.Mulligan);
    }
}