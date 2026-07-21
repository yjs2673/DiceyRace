using UnityEngine;

public class RangeObject : MonoBehaviour
{
    [Header("Range Object Settings")]
    public float moveSpeed = 10f; // 이동 속도
    public int damage = 1; // 피해량

    void Update()
    {
        transform.Translate(transform.right * -1 * moveSpeed * Time.deltaTime);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Destroy(gameObject); // 충돌 후 오브젝트 제거
        }
    }
}
