using UnityEngine;

public class DiceBoardAnchor : MonoBehaviour
{
    [SerializeField] private Transform followTarget;
    [SerializeField] private bool followXOnly = true;
    [SerializeField] private Vector3 boardOffset;

    private bool hasCachedOffset;
    private bool isSubscribedToTurnManager;

    public void Configure(Transform target)
    {
        followTarget = target;
        CacheOffsetIfNeeded();
        SnapToTarget();
        SubscribeTurnManagerIfNeeded();
    }

    private void OnEnable()
    {
        SubscribeTurnManagerIfNeeded();
    }

    private void OnDisable()
    {
        UnsubscribeTurnManager();
    }

    public void SnapToTarget()
    {
        if (followTarget == null)
        {
            return;
        }

        CacheOffsetIfNeeded();
        transform.position = GetTargetPosition(followTarget.position);
    }

    public void SnapToPosition(Vector3 targetPosition)
    {
        CacheOffsetIfNeeded(targetPosition);
        transform.position = GetTargetPosition(targetPosition);
    }

    private Vector3 GetTargetPosition(Vector3 targetPosition)
    {
        if (!followXOnly)
        {
            return targetPosition + boardOffset;
        }

        return new Vector3(
            targetPosition.x + boardOffset.x,
            boardOffset.y,
            boardOffset.z);
    }

    private void CacheOffsetIfNeeded()
    {
        if (hasCachedOffset || followTarget == null)
        {
            return;
        }

        CacheOffsetIfNeeded(followTarget.position);
    }

    private void CacheOffsetIfNeeded(Vector3 targetPosition)
    {
        if (hasCachedOffset)
        {
            return;
        }

        Vector3 currentPosition = transform.position;
        boardOffset = followXOnly
            ? new Vector3(currentPosition.x - targetPosition.x, currentPosition.y, currentPosition.z)
            : currentPosition - targetPosition;
        hasCachedOffset = true;
    }

    private void SubscribeTurnManagerIfNeeded()
    {
        if (isSubscribedToTurnManager || TurnManager.Instance == null)
        {
            return;
        }

        TurnManager.Instance.OnPhaseChanged += HandlePhaseChanged;
        isSubscribedToTurnManager = true;
    }

    private void UnsubscribeTurnManager()
    {
        if (!isSubscribedToTurnManager || TurnManager.Instance == null)
        {
            return;
        }

        TurnManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
        isSubscribedToTurnManager = false;
    }

    private void HandlePhaseChanged(TurnPhase phase)
    {
        if (phase != TurnPhase.End)
        {
            return;
        }

        SnapToTarget();
    }
}
