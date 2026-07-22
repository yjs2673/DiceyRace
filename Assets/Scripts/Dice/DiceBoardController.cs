using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceBoardController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform boardCenter;
    [SerializeField] private GameObject dicePrefab;

    [Header("Camera Focus")]
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 8f, -5.5f);
    [SerializeField] private Vector3 cameraRotation = new Vector3(55f, 35f, 0f);
    [SerializeField] private float preRollDelay = 0.15f;
    [SerializeField] private float postRollDelay = 0.5f;

    [Header("Dice Physics")]
    [SerializeField] private float spawnHeight = 2.4f;
    [SerializeField] private float rollUpForceMin = 6.5f;
    [SerializeField] private float rollUpForceMax = 9f;
    [SerializeField] private float rollSideForce = 2.5f;
    [SerializeField] private float rollTorqueForce = 16f;
    [SerializeField] private float spawnMargin = 0.85f;

    private readonly List<DiceRuntimeDie> runtimeDice = new List<DiceRuntimeDie>();
    private readonly List<int> lastResults = new List<int>();

    private DiceBoardAnchor boardAnchor;
    private CameraFollow cameraFollow;

    public bool IsRolling { get; private set; }
    public int LastRollSum { get; private set; }
    public IReadOnlyList<int> LastRollResults => lastResults;
    public bool CanRoll => transform != null;

    public void Configure(Transform followTarget, GameObject overrideDicePrefab)
    {
        boardCenter = boardCenter != null ? boardCenter : transform;
        dicePrefab = overrideDicePrefab != null ? overrideDicePrefab : dicePrefab;

        if (boardAnchor == null)
        {
            boardAnchor = GetComponent<DiceBoardAnchor>();
            if (boardAnchor == null)
            {
                boardAnchor = gameObject.AddComponent<DiceBoardAnchor>();
            }
        }

        boardAnchor.Configure(followTarget);
    }

    public IEnumerator PlayRollSequence(int diceCount)
    {
        diceCount = Mathf.Max(1, diceCount);
        IsRolling = true;
        LastRollSum = 0;
        lastResults.Clear();

        boardAnchor?.SnapToTarget();
        FocusBoard();
        yield return new WaitForSeconds(preRollDelay);

        EnsureDicePool(diceCount);
        PositionDice(diceCount);
        RollDice(diceCount);

        yield return new WaitUntil(AllDiceResolved);

        LastRollSum = 0;
        lastResults.Clear();
        for (int i = 0; i < diceCount; i++)
        {
            int result = runtimeDice[i].Result;
            lastResults.Add(result);
            LastRollSum += result;
        }

        yield return new WaitForSeconds(postRollDelay);
        ReleaseBoardFocus();
        IsRolling = false;
    }

    public static DiceBoardController CreateRuntimeFallbackBoard()
    {
        GameObject existingBoard = GameObject.Find("RuntimeDiceBoard");
        if (existingBoard != null)
        {
            DiceBoardController existingController = existingBoard.GetComponent<DiceBoardController>();
            if (existingController != null)
            {
                return existingController;
            }
        }

        GameObject root = new GameObject("RuntimeDiceBoard");
        DiceBoardController controller = root.AddComponent<DiceBoardController>();
        controller.BuildFallbackTray();
        return controller;
    }

    private void EnsureDicePool(int diceCount)
    {
        while (runtimeDice.Count < diceCount)
        {
            runtimeDice.Add(CreateDieInstance(runtimeDice.Count));
        }

        for (int i = 0; i < runtimeDice.Count; i++)
        {
            runtimeDice[i].gameObject.SetActive(i < diceCount);
        }
    }

    private DiceRuntimeDie CreateDieInstance(int index)
    {
        GameObject dieObject = dicePrefab != null
            ? Instantiate(dicePrefab, transform)
            : GameObject.CreatePrimitive(PrimitiveType.Cube);

        dieObject.name = $"RuntimeDie_{index + 1}";
        dieObject.transform.SetParent(transform, true);

        if (dicePrefab == null)
        {
            dieObject.transform.localScale = Vector3.one * 0.75f;
        }

        Rigidbody rigidbody = dieObject.GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = dieObject.AddComponent<Rigidbody>();
        }

        if (dieObject.GetComponent<Collider>() == null)
        {
            dieObject.AddComponent<BoxCollider>();
        }

        rigidbody.mass = 1f;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

        DiceRuntimeDie runtimeDie = dieObject.GetComponent<DiceRuntimeDie>();
        if (runtimeDie == null)
        {
            runtimeDie = dieObject.AddComponent<DiceRuntimeDie>();
        }

        return runtimeDie;
    }

    private void PositionDice(int diceCount)
    {
        Bounds trayBounds = CalculateTrayBounds();
        int columns = Mathf.CeilToInt(Mathf.Sqrt(diceCount));
        int rows = Mathf.CeilToInt((float)diceCount / columns);

        float usableWidth = Mathf.Max(1f, trayBounds.size.x - spawnMargin * 2f);
        float usableDepth = Mathf.Max(1f, trayBounds.size.z - spawnMargin * 2f);
        float xStep = columns > 1 ? usableWidth / (columns - 1) : 0f;
        float zStep = rows > 1 ? usableDepth / (rows - 1) : 0f;

        Vector3 start = new Vector3(
            trayBounds.center.x - usableWidth * 0.5f,
            trayBounds.max.y + spawnHeight,
            trayBounds.center.z - usableDepth * 0.5f);

        for (int i = 0; i < diceCount; i++)
        {
            int column = i % columns;
            int row = i / columns;
            Vector3 worldPosition = start + new Vector3(column * xStep, 0f, row * zStep);
            Quaternion worldRotation = Random.rotation;
            runtimeDice[i].ResetForRoll(worldPosition, worldRotation);
        }
    }

    private void RollDice(int diceCount)
    {
        for (int i = 0; i < diceCount; i++)
        {
            Vector3 force = Vector3.up * Random.Range(rollUpForceMin, rollUpForceMax)
                + transform.right * Random.Range(-rollSideForce, rollSideForce)
                + transform.forward * Random.Range(-rollSideForce, rollSideForce);
            Vector3 torque = Random.onUnitSphere * rollTorqueForce;
            runtimeDice[i].Roll(force, torque);
        }
    }

    private bool AllDiceResolved()
    {
        for (int i = 0; i < runtimeDice.Count; i++)
        {
            if (!runtimeDice[i].gameObject.activeSelf)
            {
                continue;
            }

            if (!runtimeDice[i].HasResolved)
            {
                return false;
            }
        }

        return true;
    }

    private void FocusBoard()
    {
        if (cameraFollow == null && Camera.main != null)
        {
            cameraFollow = Camera.main.GetComponent<CameraFollow>();
        }

        cameraFollow?.SetTemporaryFocus(boardCenter != null ? boardCenter : transform, cameraOffset, cameraRotation);
    }

    private void ReleaseBoardFocus()
    {
        if (cameraFollow == null && Camera.main != null)
        {
            cameraFollow = Camera.main.GetComponent<CameraFollow>();
        }

        cameraFollow?.ClearTemporaryFocus();
    }

    private Bounds CalculateTrayBounds()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        Bounds bounds = default;
        bool hasBounds = false;

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].GetComponentInParent<DiceRuntimeDie>() != null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = colliders[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(colliders[i].bounds);
            }
        }

        if (hasBounds)
        {
            return bounds;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].GetComponentInParent<DiceRuntimeDie>() != null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderers[i].bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        if (hasBounds)
        {
            return bounds;
        }

        return new Bounds(transform.position, new Vector3(6f, 2f, 6f));
    }

    private void BuildFallbackTray()
    {
        boardCenter = transform;
        transform.position = new Vector3(0f, 0f, 0f);
        CreateTrayPart("Floor", new Vector3(0f, 0f, 0f), new Vector3(6f, 0.5f, 6f));
        CreateTrayPart("Wall_Front", new Vector3(0f, 1f, 3f), new Vector3(6f, 2f, 0.5f));
        CreateTrayPart("Wall_Back", new Vector3(0f, 1f, -3f), new Vector3(6f, 2f, 0.5f));
        CreateTrayPart("Wall_Left", new Vector3(-3f, 1f, 0f), new Vector3(0.5f, 2f, 6f));
        CreateTrayPart("Wall_Right", new Vector3(3f, 1f, 0f), new Vector3(0.5f, 2f, 6f));
    }

    private void CreateTrayPart(string name, Vector3 localPosition, Vector3 localScale)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(transform, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
    }
}
