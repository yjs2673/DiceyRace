using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DiceRuntimeDie : MonoBehaviour
{
    private enum DiceFaceReadMode
    {
        TopFace,
        BottomFace
    }

    [System.Serializable]
    private struct FaceDirection
    {
        public int value;
        public Vector3 localUpDirection;
    }

    [System.Serializable]
    private struct FaceAnchor
    {
        public int value;
        public Transform anchor;
    }

    [Header("Result Detection")]
    [SerializeField] private float linearStopThreshold = 0.08f;
    [SerializeField] private float angularStopThreshold = 0.15f;
    [SerializeField] private float stableDuration = 0.4f;
    [SerializeField] private float maxRollDuration = 6f;

    [Header("Face Readout")]
    [SerializeField] private DiceFaceReadMode readMode = DiceFaceReadMode.TopFace;
    [SerializeField] private FaceAnchor[] faceAnchors;
    [SerializeField] private bool useDiceFaceAnchorComponents = true;

    private Rigidbody rb;
    private float stableTimer;
    private float rollTimer;
    private int lastDetectedFace;
    private FaceDirection[] runtimeFaces;
    private Vector3 authoredLocalScale = Vector3.one;
    private DiceFaceAnchor[] componentFaceAnchors;

    public bool IsRolling { get; private set; }
    public bool HasResolved { get; private set; }
    public int Result { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.maxAngularVelocity = Mathf.Max(rb.maxAngularVelocity, 50f);
        authoredLocalScale = transform.localScale;
        runtimeFaces = CreateDefaultFaces();
        RefreshFaceAnchors();
    }

    public void ResetForRoll(Vector3 worldPosition, Quaternion worldRotation)
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        transform.SetPositionAndRotation(worldPosition, worldRotation);

        stableTimer = 0f;
        rollTimer = 0f;
        lastDetectedFace = 0;
        Result = 0;
        IsRolling = false;
        HasResolved = false;
    }

    public void Park(Transform parent, Vector3 localPosition, Quaternion localRotation)
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        transform.SetParent(parent, false);
        transform.localPosition = localPosition;
        transform.localRotation = localRotation;

        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        IsRolling = false;
    }

    public void ApplyDiceData(Dice diceData)
    {
        RefreshFaceAnchors();

        if (diceData == null)
        {
            runtimeFaces = CreateDefaultFaces();
            ApplyModelSettings(1f, 1f);
            return;
        }

        runtimeFaces = CreateFacesFromDiceData(diceData);
        ApplyModelSettings(diceData.modelScale, diceData.rigidbodyMass);
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
        int resolvedFace = GetResolvedFace();

        if (rollTimer >= maxRollDuration)
        {
            Resolve(resolvedFace);
            return;
        }

        bool almostStopped = rb.linearVelocity.sqrMagnitude <= linearStopThreshold * linearStopThreshold
            && rb.angularVelocity.sqrMagnitude <= angularStopThreshold * angularStopThreshold;

        if (!almostStopped)
        {
            stableTimer = 0f;
            lastDetectedFace = resolvedFace;
            return;
        }

        if (resolvedFace != lastDetectedFace)
        {
            lastDetectedFace = resolvedFace;
            stableTimer = 0f;
            return;
        }

        stableTimer += Time.fixedDeltaTime;
        if (stableTimer >= stableDuration)
        {
            Resolve(resolvedFace);
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

    private int GetResolvedFace()
    {
        if (HasValidFaceAnchors())
        {
            return GetFaceFromAnchors();
        }

        return GetFaceFromDirections();
    }

    private int GetFaceFromDirections()
    {
        if (runtimeFaces == null || runtimeFaces.Length == 0)
        {
            runtimeFaces = CreateDefaultFaces();
        }

        float maxDot = float.NegativeInfinity;
        int bestValue = 1;
        Vector3 targetDirection = GetReadReferenceDirection();

        for (int i = 0; i < runtimeFaces.Length; i++)
        {
            Vector3 worldDirection = transform.TransformDirection(runtimeFaces[i].localUpDirection.normalized);
            float dot = Vector3.Dot(worldDirection, targetDirection);
            if (dot > maxDot)
            {
                maxDot = dot;
                bestValue = runtimeFaces[i].value;
            }
        }

        return bestValue;
    }

    private int GetFaceFromAnchors()
    {
        float maxDot = float.NegativeInfinity;
        int bestValue = 1;
        Vector3 targetDirection = GetReadReferenceDirection();

        if (HasValidComponentFaceAnchors())
        {
            for (int i = 0; i < componentFaceAnchors.Length; i++)
            {
                DiceFaceAnchor faceAnchor = componentFaceAnchors[i];
                if (faceAnchor == null)
                {
                    continue;
                }

                float dot = Vector3.Dot(faceAnchor.transform.up, targetDirection);
                if (dot > maxDot)
                {
                    maxDot = dot;
                    bestValue = faceAnchor.Value;
                }
            }

            return bestValue;
        }

        for (int i = 0; i < faceAnchors.Length; i++)
        {
            if (faceAnchors[i].anchor == null)
            {
                continue;
            }

            float dot = Vector3.Dot(faceAnchors[i].anchor.up, targetDirection);
            if (dot > maxDot)
            {
                maxDot = dot;
                bestValue = faceAnchors[i].value;
            }
        }

        return bestValue;
    }

    private bool HasValidFaceAnchors()
    {
        if (HasValidComponentFaceAnchors())
        {
            return true;
        }

        if (faceAnchors == null || faceAnchors.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < faceAnchors.Length; i++)
        {
            if (faceAnchors[i].anchor != null)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasValidComponentFaceAnchors()
    {
        if (!useDiceFaceAnchorComponents)
        {
            return false;
        }

        if (componentFaceAnchors == null || componentFaceAnchors.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < componentFaceAnchors.Length; i++)
        {
            if (componentFaceAnchors[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private Vector3 GetReadReferenceDirection()
    {
        return readMode == DiceFaceReadMode.BottomFace
            ? Vector3.down
            : Vector3.up;
    }

    private void RefreshFaceAnchors()
    {
        componentFaceAnchors = GetComponentsInChildren<DiceFaceAnchor>(true);
    }

    private void ApplyModelSettings(float modelScale, float rigidbodyMass)
    {
        float scaleMultiplier = Mathf.Max(0.1f, modelScale);
        transform.localScale = authoredLocalScale * scaleMultiplier;
        rb.mass = Mathf.Max(0.1f, rigidbodyMass);
    }

    private FaceDirection[] CreateFacesFromDiceData(Dice diceData)
    {
        if (diceData != null && diceData.faceDefinitions != null && diceData.faceDefinitions.Count > 0)
        {
            FaceDirection[] faces = new FaceDirection[diceData.faceDefinitions.Count];
            for (int i = 0; i < faces.Length; i++)
            {
                faces[i] = new FaceDirection
                {
                    value = diceData.faceDefinitions[i].value,
                    localUpDirection = diceData.faceDefinitions[i].localUpDirection.sqrMagnitude > 0f
                        ? diceData.faceDefinitions[i].localUpDirection.normalized
                        : Vector3.up
                };
            }

            return faces;
        }

        return CreateDefaultFaces();
    }

    private FaceDirection[] CreateDefaultFaces()
    {
        return new[]
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
