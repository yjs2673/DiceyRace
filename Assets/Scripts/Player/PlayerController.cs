using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private static readonly int IsMoveHash = Animator.StringToHash("isMove");
    private static readonly int DoJumpHash = Animator.StringToHash("doJump");
    private static readonly int DoSlideHash = Animator.StringToHash("doSlide");
    private static readonly int DoAttackHash = Animator.StringToHash("doAttack");
    private static readonly int DoShieldHash = Animator.StringToHash("doShield");
    private static readonly int DoHitHash = Animator.StringToHash("doHit");

    private Rigidbody rb;
    private Animator animator;

    [Header("Player Settings")]
    public float jumpForce = 5f;
    private bool isMoving = false;
    private bool isJumping = false;
    private bool isSliding = false;
    private bool isAttacking = false;
    private bool isParrying = false;

    [Header("State Times")]
    public float slideDuration = 1.0f;  // 슬라이딩 지속 시간
    public float attackDuration = 0.5f; // 공격 지속 시간
    public float parryDuration = 0.5f;  // 패링 지속 시간

    [Header("Buff States")]
    public int invincibleMoveCount = 0; // N번 이동 무적 (칸 이동 시 차감)
    public int ignoreHitCount = 0;      // N회 피격 무시 (맞을 때 차감)

    [Header("Card Effect Runtime")]
    [SerializeField] private bool gamblerActive;
    [SerializeField] private int gamblerSwing = 3;
    [SerializeField] private int nextGamblerThreshold = 5;
    [SerializeField] private bool dashAfterEvadeActive;
    [SerializeField] private int dashAfterEvadeDistance = 1;
    [SerializeField] private int hedonismBonusDistance;
    [SerializeField] private int parryGodBonusDistance;
    [SerializeField] private int destroyerBonusDistance;
    [SerializeField] private int jumpCrazyBonusDistance;
    [SerializeField] private bool parryReflectActive;
    [SerializeField] private int parryReflectDamage = 1;
    [SerializeField] private bool nextAttackHasParry;
    [SerializeField] private bool nextBlockIsParry;

    [Header("Managers")]
    public DiceManager diceManager;

    [Header("Animation")]
    public Animator playerAnimator;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = playerAnimator != null ? playerAnimator : GetComponentInChildren<Animator>();

        // 물리 충돌 시 멋대로 넘어지지 않도록 회전 고정
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public Vector3 PhysicsPosition => rb.position;

    #region 입력 처리
    // Input System: Jump (Q)
    public void OnJump(InputValue value)
    {
        if (value.isPressed && !isJumping)
        {            
            animator?.SetTrigger(DoJumpHash);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isJumping = true;
            if (jumpCrazyBonusDistance != 0)
            {
                AdjustCardDistance(jumpCrazyBonusDistance, "JumpCrazy");
            }
            Debug.Log("점프 (Q)");   
        }
    }

    // Input System: Slide (W)
    public void OnSlide(InputValue value)
    {
        if (value.isPressed && !isSliding)
        {
            animator?.SetTrigger(DoSlideHash);
            StartCoroutine(SlideRoutine());
            Debug.Log("슬라이딩 (W)");
        }
    }

    // Input System: Attack (E)
    public void OnAttack(InputValue value)
    {
        if (value.isPressed && !isAttacking)
        {
            animator?.SetTrigger(DoAttackHash);
            StartCoroutine(AttackRoutine());
            Debug.Log("공격 (E)");
        }
    }

    // Input System: Parry (R)
    public void OnParry(InputValue value)
    {
        if (value.isPressed && !isParrying)
        {
            animator?.SetTrigger(DoShieldHash);
            StartCoroutine(ParryRoutine());
            Debug.Log("패링 (R)");
        }
    }

    private IEnumerator AttackRoutine()
    {
        bool consumeParryAfterAttack = nextAttackHasParry;
        isAttacking = true;
        yield return new WaitForSeconds(attackDuration); // 공격 지속 시간
        isAttacking = false;

        if (consumeParryAfterAttack)
        {
            nextAttackHasParry = false;
        }
    }

    private IEnumerator ParryRoutine()
    {
        isParrying = true;
        yield return new WaitForSeconds(parryDuration); // 패링 지속 시간
        isParrying = false;
    }

    // 슬라이딩 코루틴: 총 slideDuration 동안 진행 (slideDuration/2초 눕기 -> slideDuration/2초 일어나기)
    private IEnumerator SlideRoutine()
    {
        isSliding = true;

        float duration = slideDuration;
        float halfDuration = duration / 2.0f;
        float elapsedTime = 0f;

        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(-90, 90, 0);

        while (elapsedTime < halfDuration)
        {
            transform.rotation = Quaternion.Lerp(startRotation, targetRotation, elapsedTime / halfDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.rotation = targetRotation; // 오차 보정

        elapsedTime = 0f;

        // 다시 원래 상태로 되돌리기
        while (elapsedTime < halfDuration)
        {
            transform.rotation = Quaternion.Lerp(targetRotation, startRotation, elapsedTime / halfDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.rotation = startRotation; // 오차 보정

        isSliding = false;
    }
    #endregion

    #region 충돌 처리
    public void OnCollisionEnter(Collision collision)
    {
        // 바닥에 닿으면 점프 상태 해제
        if (collision.gameObject.CompareTag("Tile"))
        {
            isJumping = false;
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        // 발판 충돌
        if (other.gameObject.CompareTag("Tile"))
        {
            Tile tile = other.GetComponent<Tile>();
            if (tile != null && tile.tileType == TileType.Moving)
            {
                EffectProcessor.ApplyTileEffect(tile, diceManager, this);
            }
        }
        // 장애물 충돌
        else if (other.gameObject.CompareTag("Obstacle"))
        {
            if (TryResolveThreat(other.gameObject, false, true, true))
            {
                return;
            }

            animator?.SetTrigger(DoHitHash);
            if (diceManager != null)
            {
                diceManager.ModifyMoves(-1);
            }
            Debug.Log("장애물 충돌 - 넉백 및 이동 수 1 감소");
            OnDamaged(1, other.gameObject);
            if (diceManager != null)
            {
                diceManager.ApplyPenaltyKnockback();
            }
        }
        // 적 충돌
        else if (other.gameObject.CompareTag("Enemy"))
        {
            if (TryResolveThreat(other.gameObject, true, true, true))
            {
                return;
            }

            animator?.SetTrigger(DoHitHash);
            if (diceManager != null)
            {
                diceManager.ModifyMoves(-1);
            }
            Debug.Log("적 충돌 (피격) - 넉백 및 이동 수 1 감소");
            OnDamaged(1, other.gameObject);
            if (diceManager != null)
            {
                diceManager.ApplyPenaltyKnockback();
            }
        }

    }
    #endregion

    #region 버프 처리
    // 외부(EffectProcessor 등)에서 버프를 부여할 때 부를 함수
    public void AddInvincibleMove(int count)
    {
        invincibleMoveCount += count;
        Debug.Log($"버프 획득: {count}번 이동까지 무적");
    }

    public void AddIgnoreHit(int count)
    {
        ignoreHitCount += count;
        Debug.Log($"버프 획득: 다음 피격 {count}회 무시");
    }

    public void ActivateGambler(int swing)
    {
        gamblerActive = true;
        gamblerSwing = Mathf.Max(1, swing);

        int currentDistance = StageManager.Instance != null ? StageManager.Instance.CurrentDistance : 0;
        nextGamblerThreshold = ((currentDistance / 5) + 1) * 5;
        Debug.Log($"카드 효과 적용: Gambler 활성화 (다음 발동 거리 {nextGamblerThreshold})");
    }

    public void ActivateDashAfterEvade(int dashDistance)
    {
        dashAfterEvadeActive = true;
        dashAfterEvadeDistance = Mathf.Max(1, dashDistance);
        Debug.Log($"카드 효과 적용: DashAfterEvade 활성화 ({dashAfterEvadeDistance}칸)");
    }

    public void ActivateHedonism(int bonusDistance)
    {
        hedonismBonusDistance = Mathf.Max(hedonismBonusDistance, bonusDistance);
        Debug.Log($"카드 효과 적용: Hedonism 활성화 (+{hedonismBonusDistance})");
    }

    public void ActivateParryGod(int bonusDistance)
    {
        parryGodBonusDistance = Mathf.Max(parryGodBonusDistance, bonusDistance);
        Debug.Log($"카드 효과 적용: ParryGod 활성화 (+{parryGodBonusDistance})");
    }

    public void ActivateDestroyer(int bonusDistance)
    {
        destroyerBonusDistance = Mathf.Max(destroyerBonusDistance, bonusDistance);
        Debug.Log($"카드 효과 적용: Destroyer 활성화 (+{destroyerBonusDistance})");
    }

    public void ActivateJumpCrazy(int bonusDistance)
    {
        jumpCrazyBonusDistance = Mathf.Max(jumpCrazyBonusDistance, bonusDistance);
        Debug.Log($"카드 효과 적용: JumpCrazy 활성화 (+{jumpCrazyBonusDistance})");
    }

    public void ActivateParryReflect(int reflectDamage)
    {
        parryReflectActive = true;
        parryReflectDamage = Mathf.Max(1, reflectDamage);
        Debug.Log($"카드 효과 적용: ParryReflect 활성화 ({parryReflectDamage} 반사 피해)");
    }

    public void EnableNextAttackParry()
    {
        nextAttackHasParry = true;
        Debug.Log("카드 효과 적용: 다음 공격에 패링 판정 추가");
    }

    public void EnableNextBlockParry()
    {
        nextBlockIsParry = true;
        Debug.Log("카드 효과 적용: 다음 방어가 패링으로 판정됩니다.");
    }

    public void TriggerInvincibleDash(int distance)
    {
        int dashDistance = Mathf.Max(1, distance);
        AddInvincibleMove(dashDistance);

        if (diceManager != null)
        {
            diceManager.QueueForcedMove(dashDistance);
        }
        else
        {
            AdjustCardDistance(dashDistance, "InvincibleDash");
        }

        Debug.Log($"카드 효과 적용: 무적 돌진 {dashDistance}칸");
    }

    public void OnMoveStepCompleted(int currentDistance)
    {
        ConsumeInvincibleMoveStep();

        if (!gamblerActive)
        {
            return;
        }

        while (currentDistance >= nextGamblerThreshold)
        {
            int delta = Random.Range(-gamblerSwing, gamblerSwing + 1);
            if (delta != 0)
            {
                AdjustCardDistance(delta, $"Gambler({nextGamblerThreshold})");
            }
            else
            {
                Debug.Log($"Gambler 발동 ({nextGamblerThreshold}) -> 변동 없음");
            }

            nextGamblerThreshold += 5;
        }
    }

    public bool DestroyCardTarget(GameObject target, string reason)
    {
        if (target == null)
        {
            return false;
        }

        if (target.TryGetComponent<BossController>(out BossController _))
        {
            int damage = Mathf.Max(1, GameManager.Instance != null ? GameManager.Instance.PlayerDamage : 1);
            StageManager.Instance?.DamageBoss(damage);
            Debug.Log($"{reason}: 보스에게 {damage} 피해");
            return true;
        }

        if (!target.CompareTag("Enemy") && !target.CompareTag("Obstacle"))
        {
            return false;
        }

        Destroy(target);
        Debug.Log($"{reason}: {target.name} 제거");
        OnTargetDestroyed(reason);
        return true;
    }

    public void HandleProjectileHit(RangeObject projectile)
    {
        if (projectile == null)
        {
            return;
        }

        GameObject projectileObject = projectile.gameObject;
        if (TryResolveThreat(projectileObject, false, true, true))
        {
            Destroy(projectileObject);
            return;
        }

        ReceiveDirectDamage(projectile.damage, projectileObject, true);
        Destroy(projectileObject);
    }

    public void ResetTurnCardEffects()
    {
        gamblerActive = false;
        gamblerSwing = 3;
        nextGamblerThreshold = 5;
        dashAfterEvadeActive = false;
        dashAfterEvadeDistance = 1;
        hedonismBonusDistance = 0;
        parryGodBonusDistance = 0;
        destroyerBonusDistance = 0;
        jumpCrazyBonusDistance = 0;
        parryReflectActive = false;
        parryReflectDamage = 1;
        nextAttackHasParry = false;
        nextBlockIsParry = false;
    }

    // 적이나 장애물에 닿았을 때 무적 상태인지 체크하고 차감하는 헬퍼 함수
    private bool CheckAndConsumeInvincibility()
    {
        // 이동 횟수 기반 무적이 켜져 있다면 피격 무시 (차감은 이동 로직에서 처리)
        if (invincibleMoveCount > 0)
        {
            Debug.Log("무적 상태! 피해를 무시합니다.");
            return true;
        }

        // 횟수제 방어막이 있다면 1회 깎고 피격 무시
        if (ignoreHitCount > 0)
        {
            ignoreHitCount--;
            Debug.Log($"방어막 발동! 피해 무시 (남은 방어막: {ignoreHitCount})");
            return true;
        }

        return false; // 방어 수단이 없으면 false 반환 (피해 입음)
    }

    private void ConsumeInvincibleMoveStep()
    {
        if (invincibleMoveCount <= 0)
        {
            return;
        }

        invincibleMoveCount--;
        Debug.Log($"이동 무적 차감 -> 남은 무적 이동: {invincibleMoveCount}");
    }

    private bool TryResolveThreat(GameObject source, bool canAttack, bool canParry, bool canEvade)
    {
        if (canEvade && isSliding)
        {
            OnSuccessfulEvade();
            return true;
        }

        bool forcedParry = false;
        if (canParry && nextBlockIsParry)
        {
            nextBlockIsParry = false;
            forcedParry = true;
            animator?.SetTrigger(DoShieldHash);
        }

        bool attackParry = canParry && isAttacking && nextAttackHasParry;
        if ((canParry && isParrying) || forcedParry || attackParry)
        {
            if (attackParry)
            {
                nextAttackHasParry = false;
            }

            OnSuccessfulParry(source);
            return true;
        }

        if (canAttack && isAttacking)
        {
            OnSuccessfulAttack(source);
            return true;
        }

        return CheckAndConsumeInvincibility();
    }

    private void OnSuccessfulAttack(GameObject source)
    {
        Debug.Log("적 공격 성공! - 남은 이동 수 1 증가");
        if (diceManager != null)
        {
            diceManager.ModifyMoves(1);
        }

        DestroyCardTarget(source, "공격 성공");
    }

    private void OnSuccessfulParry(GameObject source)
    {
        Debug.Log("패링 성공! - 남은 이동 수 1 증가");
        if (diceManager != null)
        {
            diceManager.ModifyMoves(1);
        }

        if (parryGodBonusDistance != 0)
        {
            AdjustCardDistance(parryGodBonusDistance, "ParryGod");
        }

        if (parryReflectActive)
        {
            ReflectThreat(source);
        }
    }

    private void OnSuccessfulEvade()
    {
        Debug.Log("회피 성공!");

        if (dashAfterEvadeActive)
        {
            TriggerInvincibleDash(dashAfterEvadeDistance);
        }
    }

    private void ReflectThreat(GameObject source)
    {
        if (source == null)
        {
            return;
        }

        if (source.TryGetComponent<BossController>(out BossController _))
        {
            StageManager.Instance?.DamageBoss(parryReflectDamage);
            Debug.Log($"패링 반사: 보스에게 {parryReflectDamage} 피해");
            return;
        }

        if (source.TryGetComponent<RangeObject>(out RangeObject _))
        {
            Destroy(source);
            Debug.Log("패링 반사: 투사체 제거");
            return;
        }

        DestroyCardTarget(source, "패링 반사");
    }

    private void OnDamaged(int damage, GameObject source)
    {
        if (hedonismBonusDistance != 0)
        {
            AdjustCardDistance(hedonismBonusDistance, "Hedonism");
        }
    }

    private void OnTargetDestroyed(string reason)
    {
        if (destroyerBonusDistance != 0)
        {
            AdjustCardDistance(destroyerBonusDistance, $"Destroyer:{reason}");
        }
    }

    private void AdjustCardDistance(int amount, string reason)
    {
        if (amount == 0)
        {
            return;
        }

        if (diceManager != null && diceManager.IsMoving)
        {
            diceManager.ModifyMoves(amount);
        }
        else
        {
            StageManager.Instance?.ModifyDistance(amount);
        }

        Debug.Log($"{reason}: 거리 {amount:+#;-#;0}");
    }
    #endregion

    #region 외부 이동 처리
    // DiceManager에서 이동 처리 시 PlayerController에 적용할 수 있는 메서드
    public void ApplyMove(float deltaX)
    {
        if (Mathf.Approximately(deltaX, 0f))
            return;

        rb.MovePosition(rb.position + new Vector3(deltaX, 0f, 0f));
        StageManager.Instance?.ApplyPlayerDelta(deltaX);
    }

    public void SnapToPosition(Vector3 position)
    {
        float deltaX = position.x - rb.position.x;
        rb.position = position;

        if (!Mathf.Approximately(deltaX, 0f))
        {
            StageManager.Instance?.ApplyPlayerDelta(deltaX);
        }
    }

    public void SetAutoMoveAnimation(bool moving)
    {
        isMoving = moving;
        animator?.SetBool(IsMoveHash, moving);
    }

    public void ReceiveDirectDamage(int damage, GameObject source = null, bool canParry = false)
    {
        if (damage <= 0)
        {
            return;
        }

        if (TryResolveThreat(source, false, canParry, false))
        {
            return;
        }

        animator?.SetTrigger(DoHitHash);
        GameManager.Instance?.TakeDamage(damage);
        OnDamaged(damage, source);
        Debug.Log($"직접 피해 {damage} -> 현재 체력: {GameManager.Instance?.PlayerHP ?? 0}");
    }
    #endregion
}
