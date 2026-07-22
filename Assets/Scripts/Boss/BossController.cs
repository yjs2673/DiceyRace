using UnityEngine;
using System.Collections;

public class BossController : MonoBehaviour
{
    private static readonly int DoAttackHash = Animator.StringToHash("doAttack");

    [Header("Boss Settings")]
    public int maxHP = 100;
    public int attackDamage = 10;
    public float attackDelay = 0.4f;

    [Header("Animation")]
    public Animator bossAnimator;

    public int CurrentHP { get; private set; }
    public bool IsDead => CurrentHP <= 0;

    private Transform followTarget;
    private float followOffsetX;
    private float fixedY;
    private float fixedZ;
    private Quaternion fixedRotation;

    private void Awake()
    {
        bossAnimator = bossAnimator != null ? bossAnimator : GetComponentInChildren<Animator>();
        CurrentHP = maxHP;
        CacheFixedTransform();
    }

    public void InitializeFollow(Transform playerTransform)
    {
        followTarget = playerTransform;
        CacheFixedTransform();

        if (followTarget == null)
        {
            return;
        }

        followOffsetX = transform.position.x - followTarget.position.x;
        SyncToFollowTarget();
    }

    public void ApplyPlayerDelta(float deltaX)
    {
        if (Mathf.Approximately(deltaX, 0f))
        {
            return;
        }

        Vector3 position = transform.position;
        position.x += deltaX;
        position.y = fixedY;
        position.z = fixedZ;
        transform.position = position;
        transform.rotation = fixedRotation;
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead)
        {
            return;
        }

        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        Debug.Log($"[Boss] 피격 {amount} -> 현재 HP: {CurrentHP}/{maxHP}");
    }

    public void RestoreHP(int amount)
    {
        CurrentHP = Mathf.Clamp(amount, 0, maxHP);
    }

    public IEnumerator PerformEndTurnAttack(PlayerController player)
    {
        if (IsDead || player == null)
        {
            yield break;
        }

        SyncToFollowTarget();
        bossAnimator?.SetTrigger(DoAttackHash);
        yield return new WaitForSeconds(attackDelay);
        player.ReceiveDirectDamage(attackDamage, gameObject, true);
    }

    private void LateUpdate()
    {
        Vector3 position = transform.position;
        bool needsCorrection = false;

        if (!Mathf.Approximately(position.y, fixedY))
        {
            position.y = fixedY;
            needsCorrection = true;
        }

        if (!Mathf.Approximately(position.z, fixedZ))
        {
            position.z = fixedZ;
            needsCorrection = true;
        }

        if (needsCorrection)
        {
            transform.position = position;
        }

        if (transform.rotation != fixedRotation)
        {
            transform.rotation = fixedRotation;
        }
    }

    private void CacheFixedTransform()
    {
        fixedY = transform.position.y;
        fixedZ = transform.position.z;
        fixedRotation = transform.rotation;
    }

    private void SyncToFollowTarget()
    {
        if (followTarget == null)
        {
            return;
        }

        Vector3 position = transform.position;
        position.x = followTarget.position.x + followOffsetX;
        position.y = fixedY;
        position.z = fixedZ;
        transform.position = position;
        transform.rotation = fixedRotation;
    }
}
