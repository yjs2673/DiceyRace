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
        transform.position = GetTargetPosition();
    }

    private Vector3 GetTargetPosition()
    {
        if (!followXOnly)
        {
            return followTarget.position + boardOffset;
        }

        return new Vector3(
            followTarget.position.x + boardOffset.x,
            boardOffset.y,
            boardOffset.z);
    }

    private void CacheOffsetIfNeeded()
    {
        if (hasCachedOffset || followTarget == null)
        {
            return;
        }

        Vector3 currentPosition = transform.position;
        boardOffset = followXOnly
            ? new Vector3(currentPosition.x - followTarget.position.x, currentPosition.y, currentPosition.z)
            : currentPosition - followTarget.position;
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
