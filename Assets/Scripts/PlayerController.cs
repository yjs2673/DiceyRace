using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Rigidbody rb;

    [Header("Player Settings")]
    public float jumpForce = 5f;
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

    [Header("Managers")]
    public DiceManager diceManager;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // 물리 충돌 시 멋대로 넘어지지 않도록 회전 고정
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    #region 입력 처리
    // Input System: Jump (Q)
    public void OnJump(InputValue value)
    {
        if (value.isPressed && !isJumping)
        {            
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isJumping = true;
            Debug.Log("점프 (Q)");   
        }
    }

    // Input System: Slide (W)
    public void OnSlide(InputValue value)
    {
        if (value.isPressed && !isSliding)
        {
            StartCoroutine(SlideRoutine());
            Debug.Log("슬라이딩 (W)");
        }
    }

    // Input System: Attack (E)
    public void OnAttack(InputValue value)
    {
        if (value.isPressed)
        {
            StartCoroutine(AttackRoutine());
            Debug.Log("공격 (E)");
        }
    }

    // Input System: Parry (R)
    public void OnParry(InputValue value)
    {
        if (value.isPressed)
        {
            StartCoroutine(ParryRoutine());
            Debug.Log("패링 (R)");
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        yield return new WaitForSeconds(attackDuration); // 공격 지속 시간
        isAttacking = false;
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
        Quaternion targetRotation = Quaternion.Euler(0, 0, 90);

        // (0, 0, 90)으로 눕기
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
        // 바닥에 닿으면 슬라이딩 상태 해제
        if (collision.gameObject.CompareTag("Tile"))
        {
            isJumping = false;
            isSliding = false;
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
            // 무적 방어막이 켜져있다면 효과 차감 후 그냥 지나감 (넉백 X)
            if (CheckAndConsumeInvincibility()) return;

            diceManager.ModifyMoves(-1);
            Debug.Log("장애물 충돌 - 넉백 및 이동 수 1 감소");
            // GameManager를 통한 체력 감소 로직 필요 시 여기에 추가
            if (diceManager != null)
            {
                diceManager.ApplyPenaltyKnockback();
            }
        }
        // 적 충돌
        else if (other.gameObject.CompareTag("Enemy"))
        {
            if (!isAttacking && !isParrying)
            {
                // 방어/공격 안 했는데 무적도 없으면 넉백
                if (CheckAndConsumeInvincibility()) return;

                diceManager.ModifyMoves(-1);
                Debug.Log("적 충돌 (피격) - 넉백 및 이동 수 1 감소");
                if (diceManager != null)
                {
                    diceManager.ApplyPenaltyKnockback();
                }
            }
            else if (isAttacking)
            {
                Debug.Log("적 공격 성공! - 남은 이동 수 1 증가");
                if (diceManager != null)
                {
                    diceManager.ModifyMoves(1);
                }
            }
            else if (isParrying) 
            {
                Debug.Log("적 패링 성공! - 남은 이동 수 1 증가");
                if (diceManager != null)
                {
                    diceManager.ModifyMoves(1);
                }
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
    #endregion
}