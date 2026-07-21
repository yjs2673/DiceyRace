using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform player; // 따라갈 플레이어

    [Header("Settings")]
    public float smoothSpeed = 10f; // 카메라가 따라가는 속도 (클수록 덜 미끄러짐)

    private float fixedY;
    private float fixedZ;
    private float offsetX;

    private void Start()
    {
        if (player != null)
        {
            // 게임 시작 시 카메라의 원래 Y, Z 위치를 고정값으로 저장해둬
            fixedY = transform.position.y;
            fixedZ = transform.position.z;
            
            // 플레이어와 카메라 사이의 X축 간격(오프셋) 계산
            offsetX = transform.position.x - player.position.x;
        }
    }

    private void LateUpdate()
    {
        if (player == null) return;

        // 목표 위치: 플레이어의 X 위치에 오프셋을 더하고, Y와 Z는 고정값 사용
        Vector3 targetPosition = new Vector3(player.position.x + offsetX, fixedY, fixedZ);
        
        // Lerp를 이용해 부드럽게 목표 위치로 이동
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
    }
}