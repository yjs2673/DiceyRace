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
    private Quaternion followRotation;
    private bool hasFocusTarget;
    private Transform focusTarget;
    private Vector3 focusOffset;
    private Quaternion focusRotation;

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

        followRotation = transform.rotation;
    }

    private void LateUpdate()
    {
        if (hasFocusTarget && focusTarget != null)
        {
            Vector3 focusPosition = focusTarget.position + focusOffset;
            transform.position = Vector3.Lerp(transform.position, focusPosition, smoothSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, focusRotation, smoothSpeed * Time.deltaTime);
            return;
        }

        if (player == null) return;

        // 목표 위치: 플레이어의 X 위치에 오프셋을 더하고, Y와 Z는 고정값 사용
        Vector3 targetPosition = new Vector3(player.position.x + offsetX, fixedY, fixedZ);
        
        // Lerp를 이용해 부드럽게 목표 위치로 이동
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, followRotation, smoothSpeed * Time.deltaTime);
    }

    public void SetTemporaryFocus(Transform target, Vector3 offset, Vector3 eulerAngles)
    {
        if (target == null)
        {
            return;
        }

        focusTarget = target;
        focusOffset = offset;
        focusRotation = Quaternion.Euler(eulerAngles);
        hasFocusTarget = true;
    }

    public void ClearTemporaryFocus()
    {
        hasFocusTarget = false;
        focusTarget = null;
    }

    public bool IsNearFollowPose(float positionThreshold = 0.15f, float rotationThreshold = 2f)
    {
        if (player == null)
        {
            return true;
        }

        Vector3 targetPosition = new Vector3(player.position.x + offsetX, fixedY, fixedZ);
        return Vector3.Distance(transform.position, targetPosition) <= positionThreshold
            && Quaternion.Angle(transform.rotation, followRotation) <= rotationThreshold;
    }

    public void SnapToFollowTarget()
    {
        if (player == null)
        {
            return;
        }

        transform.position = new Vector3(player.position.x + offsetX, fixedY, fixedZ);
        transform.rotation = followRotation;
    }
}
