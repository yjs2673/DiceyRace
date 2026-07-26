using UnityEngine;

// 발판 유형
public enum TileType
{
    None,   // 효과 없음
    Moving, // 이동 발판
    Stop    // 정지 발판
}

// 발판 효과
public enum TileEffect
{
    None,
    ChangeMoveDistance, // 이동거리 증감 (음수면 감소, 양수면 증가)
    SetInvincible,      // N회 무적
    IgnoreNextHit,      // 다음 피격 무시 (수치 1)
    AddReroll,          // 리롤 횟수 증가
    IncreaseSpeed,      // 이동속도 증가
    Heal,               // 체력 회복
    MultiplyMoney,      // 소지 골드 배수 증가
    DamageToBoss,       // 보스에게 데미지
    DamageToBossByCrashingObstacle, // 장애물 파괴 시 보스에게 데미지
    DamageToBossByRemainDistance,   // 남은 이동거리 비례 보스에게 데미지
    OpenShop            // 상점 호출
}

public class Tile : MonoBehaviour
{
    [Header("Tile Settings")]
    public TileType tileType;
    public TileEffect tileEffect;
    
    [Tooltip("효과 수치")]
    public int effectValue;

    public bool HasInspectableEffect => tileEffect != TileEffect.None;

    public string GetTooltipTypeText()
    {
        return tileType.ToString();
    }

    public string GetTooltipEffectText()
    {
        if (tileEffect == TileEffect.None)
        {
            return tileEffect.ToString();
        }

        string valueText = effectValue > 0 ? $"+{effectValue}" : effectValue.ToString();
        return $"{tileEffect} {valueText}";
    }
}
