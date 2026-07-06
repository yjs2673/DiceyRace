using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    private Rigidbody rb;
    
    [Header("Player Settings")]
    public float jumpForce = 5f;
    private bool isSliding = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        // 물리 충돌 시 멋대로 넘어지지 않도록 회전 고정
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    // Input System: Jump (Q) - Send Messages 방식에 맞춘 시그니처
    public void OnJump(InputValue value)
    {
        if (value.isPressed) 
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
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
            Debug.Log("공격 (E)");
        }
    }

    // Input System: Parry (R)
    public void OnParry(InputValue value)
    {
        if (value.isPressed)
        {
            Debug.Log("패링 (R)");
        }
    }

    // 슬라이딩 코루틴: 총 1초 동안 진행 (0.5초 눕기 -> 0.5초 일어나기)
    private IEnumerator SlideRoutine()
    {
        isSliding = true;
        
        float duration = 1.0f;
        float halfDuration = duration / 2.0f;
        float elapsedTime = 0f;

        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.Euler(0, 0, 90);

        // 1. (0, 0, 90)으로 눕기
        while (elapsedTime < halfDuration)
        {
            transform.rotation = Quaternion.Lerp(startRotation, targetRotation, elapsedTime / halfDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.rotation = targetRotation; // 오차 보정

        elapsedTime = 0f;

        // 2. 다시 원래 상태로 되돌리기
        while (elapsedTime < halfDuration)
        {
            transform.rotation = Quaternion.Lerp(targetRotation, startRotation, elapsedTime / halfDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.rotation = startRotation; // 오차 보정

        isSliding = false;
    }
}