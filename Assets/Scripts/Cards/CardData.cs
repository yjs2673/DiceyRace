using UnityEngine;

// 카드의 종류 (패시브 / 액티브)
public enum CardType
{
    Passive, // 들고만 있어도 자동 발동
    Active   // 숫자 키를 눌러 사용
}

// 기획서 기반 카드 효과 목록
public enum CardEffect
{
    None,
    Gambler,                // 5의 배수마다 도박 시작 (거리 -3 ~ +3)
    DashAfterEvade,         // 회피 성공시 짧게 무적 돌진
    Hedonism,               // 데미지 받을 시 거리 증가
    ParryGod,               // 막기 판정 삭제, 패링시 거리 추가
    Destroyer,              // 지형 지물이나 몹 제거마다 거리 추가
    JumpCrazy,              // 점프할 때마다 거리 증가
    ParryReflect,           // 패링 시 공격 반사
    AddParryToAttack,       // 다음 공격에 패링 판정 추가
    InvincibleDash,         // n거리 무적 돌진
    DestroyEnemy,           // n거리 적과 장애물 파괴
    NextBlockIsParry,       // 다음 방어가 반드시 패링으로 판정
    AddDestroyToAttack,     // 다음 공격에 장애물 파괴 판정 추가
    IgnoreMoveTileEffects,  // 이동 타일 효과 무시
    AddDistance,            // 거리 증가
    LongFast,               // 거리, 속도 증가
    StopMovement            // 즉시 이동 정지
}

[CreateAssetMenu(fileName = "New Card", menuName = "Game/Card")]
public class CardData : ScriptableObject
{
    public int cardID;
    public string cardName;
    public Sprite icon;
    public int price;
    [TextArea]
    public string description;

    [Header("Card Logic")]
    public CardType cardType;
    public CardEffect cardEffect;
    public int effectValue; 
}