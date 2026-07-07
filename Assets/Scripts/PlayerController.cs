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
        if (value.isPressed)
        {
            if (!isJumping)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                isJumping = true;
                Debug.Log("점프 (Q)");
            }
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
        if (collision.gameObject.CompareTag("Floor"))
        {
            isJumping = false;
            isSliding = false;
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Obstacle"))
        {
            Debug.Log("장애물 충돌 - 남은 이동 수 1 감소");
            if (diceManager != null) diceManager.ModifyMoves(-1);
        }
        else if (other.gameObject.CompareTag("Enemy"))
        {
            if (!isAttacking && !isParrying)
            {
                Debug.Log("적 충돌 (피격) - 남은 이동 수 1 감소");
                if (diceManager != null) diceManager.ModifyMoves(-1);
            }
            else if (isAttacking)
            {
                Debug.Log("적 공격 성공! - 남은 이동 수 1 증가");
                if (diceManager != null) diceManager.ModifyMoves(1);
            }
            else if (isParrying)
            {
                Debug.Log("적 패링 성공! - 남은 이동 수 1 증가");
                if (diceManager != null) diceManager.ModifyMoves(1);
            }
        }
    }
    #endregion
}