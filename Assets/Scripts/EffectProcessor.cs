using UnityEngine;

// 타일의 효과를 처리하는 클래스
public static class EffectProcessor
{
    // 타일의 효과와 수치를 받아서 매니저에게 명령
    public static void ApplyTileEffect(Tile tile, DiceManager diceManager, PlayerController player)
    {
        if (tile == null || tile.tileEffect == TileEffect.None) return;

        int value = tile.effectValue;

        switch (tile.tileEffect)
        {
            case TileEffect.ChangeMoveDistance:
                if (diceManager != null)
                {
                    diceManager.ModifyMoves(value);
                }
                Debug.Log($"발판 효과: 이동 거리 {value} 증감");
                break;

            case TileEffect.Heal:
                GameManager.Instance.Heal(value);
                Debug.Log($"발판 효과: 체력 {value} 회복 -> 현재 체력: {GameManager.Instance.PlayerHP}");
                break;

            case TileEffect.MultiplyMoney:
                // value가 2라면, 현재 돈만큼 한 번 더 더해줘서 2배로 만듦
                int currentCoin = GameManager.Instance.Coin;
                GameManager.Instance.AddCoin(currentCoin * (value - 1));
                Debug.Log($"발판 효과: 골드 {value}배 -> 현재 골드: {GameManager.Instance.Coin}");
                break;

            case TileEffect.AddReroll:
                diceManager.AddReroll(value);
                Debug.Log($"발판 효과: 리롤 {value}회 증가 -> 남은 리롤: {diceManager.remainingRerolls}");
                break;

            // TODO: 무적, 상점, 보스 데미지 등은 관련 시스템이 만들어진 후 연결
            case TileEffect.SetInvincible:
                player.AddInvincibleMove(value);
                Debug.Log($"발판 효과: {value}회 이동 무적 적용 (구현 예정)");
                break;

            case TileEffect.OpenShop:
                player.AddIgnoreHit(value);
                Debug.Log("발판 효과: 상점 씬으로 이동 (구현 예정)");
                break;

            // TODO: 보스 데미지 효과는 보스 관련 시스템이 만들어진 후 연결
            case TileEffect.DamageToBoss:
                Debug.Log($"발판 효과: 보스에게 {value} 데미지 (구현 예정)");
                break;
        }
    }
}