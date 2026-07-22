using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DiceRuntimeDie : MonoBehaviour
{
    [System.Serializable]
    private struct FaceDirection
    {
        public int value;
        public Vector3 localUpDirection;
    }

    [Header("Result Detection")]
    [SerializeField] private float linearStopThreshold = 0.08f;
    [SerializeField] private float angularStopThreshold = 0.15f;
    [SerializeField] private float stableDuration = 0.4f;
    [SerializeField] private float maxRollDuration = 6f;
    [SerializeField] private FaceDirection[] faceDirections;

    private Rigidbody rb;
    private float stableTimer;
    private float rollTimer;
    private int lastDetectedFace;

    public bool IsRolling { get; private set; }
    public bool HasResolved { get; private set; }
    public int Result { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.maxAngularVelocity = Mathf.Max(rb.maxAngularVelocity, 50f);
        EnsureDefaultFaces();
    }

    public void ResetForRoll(Vector3 worldPosition, Quaternion worldRotation)
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        transform.SetPositionAndRotation(worldPosition, worldRotation);
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        stableTimer = 0f;
        rollTimer = 0f;
        lastDetectedFace = 0;
        Result = 0;
        IsRolling = false;
        HasResolved = false;
    }

    public void Roll(Vector3 force, Vector3 torque)
    {
        stableTimer = 0f;
        rollTimer = 0f;
        lastDetectedFace = 0;
        Result = 0;
        HasResolved = false;
        IsRolling = true;

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.AddForce(force, ForceMode.Impulse);
        rb.AddTorque(torque, ForceMode.Impulse);
    }

    private void FixedUpdate()
    {
        if (!IsRolling)
        {
            return;
        }

        rollTimer += Time.fixedDeltaTime;
        int topFace = GetTopFace();

        if (rollTimer >= maxRollDuration)
        {
            Resolve(topFace);
            return;
        }

        bool almostStopped = rb.linearVelocity.sqrMagnitude <= linearStopThreshold * linearStopThreshold
            && rb.angularVelocity.sqrMagnitude <= angularStopThreshold * angularStopThreshold;

        if (!almostStopped)
        {
            stableTimer = 0f;
            lastDetectedFace = topFace;
            return;
        }

        if (topFace != lastDetectedFace)
        {
            lastDetectedFace = topFace;
            stableTimer = 0f;
            return;
        }

        stableTimer += Time.fixedDeltaTime;
        if (stableTimer >= stableDuration)
        {
            Resolve(topFace);
        }
    }

    private void Resolve(int topFace)
    {
        IsRolling = false;
        HasResolved = true;
        Result = topFace;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    private int GetTopFace()
    {
        EnsureDefaultFaces();

        float maxDot = float.NegativeInfinity;
        int bestValue = 1;

        for (int i = 0; i < faceDirections.Length; i++)
        {
            Vector3 worldDirection = transform.TransformDirection(faceDirections[i].localUpDirection.normalized);
            float dot = Vector3.Dot(worldDirection, Vector3.up);
            if (dot > maxDot)
            {
                maxDot = dot;
                bestValue = faceDirections[i].value;
            }
        }

        return bestValue;
    }

    private void EnsureDefaultFaces()
    {
        if (faceDirections != null && faceDirections.Length == 6)
        {
            return;
        }

        faceDirections = new[]
        {
            new FaceDirection { value = 1, localUpDirection = Vector3.up },
            new FaceDirection { value = 6, localUpDirection = Vector3.down },
            new FaceDirection { value = 2, localUpDirection = Vector3.right },
            new FaceDirection { value = 5, localUpDirection = Vector3.left },
            new FaceDirection { value = 3, localUpDirection = Vector3.forward },
            new FaceDirection { value = 4, localUpDirection = Vector3.back }
        };
    }
}
