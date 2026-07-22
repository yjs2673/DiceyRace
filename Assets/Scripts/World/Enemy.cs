using UnityEngine;
using System.Collections;

public enum EnemyType
{
    Normal, // 일반 적
    Flying, // 비행 적
    Range,  // 원거리 공격 적
    Boss    // 보스 적
}

public class Enemy : MonoBehaviour
{
    [Header("Enemy Settings")]
    public EnemyType enemyType;
    public float rangeLength = 5f;  // 원거리 공격 범위 길이
    public float shootInterval = 2f;// 원거리 공격 시간 간격

    [Header("Range Object")]
    public GameObject rangeObject;
    public Transform spawnPoint; // 원거리 공격 오브젝트 생성 위치

    private Coroutine shootRoutine;

    private void OnEnable()
    {
        if (enemyType == EnemyType.Range)
        {
            shootRoutine = StartCoroutine(ShootRoutine());
        }
    }

    private void OnDisable()
    {
        if (shootRoutine != null)
        {
            StopCoroutine(shootRoutine);
            shootRoutine = null;
        }
    }

    private IEnumerator ShootRoutine()
    {
        while (true)
        {
            if (CanShootPlayer())
            {
                // if (TurnManager.Instance.CurrentPhase == TurnPhase.Move)
                Instantiate(rangeObject, spawnPoint.position, spawnPoint.rotation);
                Debug.Log("원거리 공격!");
            }

            yield return new WaitForSeconds(shootInterval);
        }
    }

    private bool CanShootPlayer()
    {
        if (enemyType != EnemyType.Range || rangeObject == null || spawnPoint == null)
            return false;

        return Physics.Raycast(transform.position, transform.right, out RaycastHit hit, rangeLength)
            && hit.collider.CompareTag("Player");
    }
}
