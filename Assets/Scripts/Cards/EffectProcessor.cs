using System.Collections.Generic;
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

            case TileEffect.IgnoreNextHit:
                player.AddIgnoreHit(value);
                Debug.Log($"발판 효과: 다음 피격 {value}회 무시");
                break;

            case TileEffect.OpenShop:
                if (GameManager.Instance != null && player != null && diceManager != null && StageManager.Instance != null)
                {
                    GameManager.Instance.EnterShopFromField(player, diceManager, StageManager.Instance, TurnPhase.End);
                    Debug.Log("발판 효과: 상점 씬으로 이동");
                }
                else
                {
                    Debug.LogError("상점 이동에 필요한 필드 참조가 부족합니다.");
                }
                break;

            case TileEffect.DamageToBoss:
                StageManager.Instance?.DamageBoss(value);
                Debug.Log($"발판 효과: 보스에게 {value} 데미지");
                break;
        }
    }

    // 카드의 효과와 수치를 받아서 매니저에게 명령
    public static void ApplyCardEffect(CardData card, DiceManager diceManager, PlayerController player)
    {
        if (card == null || card.cardEffect == CardEffect.None)
        {
            return;
        }

        int value = card.effectValue;
        int normalizedValue = value > 0 ? value : 1;

        switch (card.cardEffect)
        {
            case CardEffect.Gambler:
                player?.ActivateGambler(value > 0 ? value : 3);
                break;

            case CardEffect.DashAfterEvade:
                player?.ActivateDashAfterEvade(normalizedValue);
                break;

            case CardEffect.Hedonism:
                player?.ActivateHedonism(normalizedValue);
                break;

            case CardEffect.ParryGod:
                player?.ActivateParryGod(normalizedValue);
                break;

            case CardEffect.Destroyer:
                player?.ActivateDestroyer(normalizedValue);
                break;

            case CardEffect.JumpCrazy:
                player?.ActivateJumpCrazy(normalizedValue);
                break;

            case CardEffect.ParryReflect:
                player?.ActivateParryReflect(normalizedValue);
                break;

            case CardEffect.AddParryToAttack:
                player?.EnableAttackParry(normalizedValue, card.cardType == CardType.Passive);
                break;

            case CardEffect.InvincibleDash:
                player?.TriggerInvincibleDash(value > 0 ? value : 2);
                break;

            case CardEffect.DestroyEnemy:
                DestroyTargetsAhead(player, diceManager, value > 0 ? value : 3);
                break;

            case CardEffect.NextBlockIsParry:
                player?.EnableBlockParry(normalizedValue, card.cardType == CardType.Passive);
                break;

            case CardEffect.AddDestroyToAttack:
                player?.EnableObstacleDestroyOnAttack(normalizedValue, card.cardType == CardType.Passive);
                break;

            case CardEffect.IgnoreMoveTileEffects:
                player?.EnableIgnoreMoveTileEffects(normalizedValue, card.cardType == CardType.Passive);
                break;

            case CardEffect.AddDistance:
                player?.AddCardDistanceBoost(value);
                break;

            case CardEffect.LongFast:
                player?.ActivateLongFast(normalizedValue, 2f);
                break;

            case CardEffect.StopMovement:
                player?.StopRemainingMovement();
                break;
        }
    }

    private static void DestroyTargetsAhead(PlayerController player, DiceManager diceManager, int distance)
    {
        if (player == null)
        {
            return;
        }

        float stepDistance = diceManager != null ? diceManager.StepDistance : 1f;
        float range = Mathf.Max(1, distance) * stepDistance;
        Vector3 center = player.transform.position + Vector3.right * (range * 0.5f);
        Vector3 halfExtents = new Vector3(range * 0.5f, 1.5f, 1.5f);
        Collider[] hits = Physics.OverlapBox(center, halfExtents);
        HashSet<GameObject> processedTargets = new HashSet<GameObject>();
        int destroyedCount = 0;

        foreach (Collider hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

            GameObject target = hit.attachedRigidbody != null
                ? hit.attachedRigidbody.gameObject
                : hit.gameObject;

            if (!processedTargets.Add(target))
            {
                continue;
            }

            if (!target.CompareTag("Enemy") && !target.CompareTag("Obstacle"))
            {
                continue;
            }

            if (target.transform.position.x < player.transform.position.x)
            {
                continue;
            }

            if (player.DestroyCardTarget(target, "DestroyEnemy3"))
            {
                destroyedCount++;
            }
        }

        Debug.Log($"카드 효과 적용: 전방 {distance}칸 파괴 -> {destroyedCount}개 제거");
    }
}
