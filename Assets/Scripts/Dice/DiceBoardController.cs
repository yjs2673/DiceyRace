using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceBoardController : MonoBehaviour
{
    private sealed class RuntimeDieHandle
    {
        public GameObject prefabSource;
        public DiceRuntimeDie runtimeDie;
        public Dice sourceData;
        public Transform parkedParent;
        public Vector3 parkedLocalPosition;
        public Quaternion parkedLocalRotation;
        public bool isParked;
    }

    [Header("References")]
    [SerializeField] private Transform boardCenter;
    [SerializeField] private GameObject dicePrefab;
    [SerializeField] private Transform[] rollSpawnPoints;

    [Header("Camera Focus")]
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 8f, -5.5f);
    [SerializeField] private Vector3 cameraRotation = new Vector3(55f, 35f, 0f);
    [SerializeField] private float preRollDelay = 0.15f;
    [SerializeField] private float postRollDelay = 0.5f;

    [Header("Dice Physics")]
    [SerializeField] private float parkedHeight = 0.35f;
    [SerializeField] private float spawnHeight = 2.4f;
    [SerializeField] private float rollUpForceMin = 6.5f;
    [SerializeField] private float rollUpForceMax = 9f;
    [SerializeField] private float rollSideForce = 2.5f;
    [SerializeField] private float rollTorqueForce = 16f;
    [SerializeField] private float spawnMargin = 0.85f;

    private readonly List<RuntimeDieHandle> runtimeDice = new List<RuntimeDieHandle>();
    private readonly List<int> lastResults = new List<int>();

    private DiceBoardAnchor boardAnchor;
    private CameraFollow cameraFollow;
    private bool hasWarnedAboutSpawnPointCount;

    public bool IsRolling { get; private set; }
    public int LastRollSum { get; private set; }
    public IReadOnlyList<int> LastRollResults => lastResults;
    public bool CanRoll => transform != null;

    public void Configure(Transform followTarget)
    {
        boardCenter = boardCenter != null ? boardCenter : transform;

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

    public IEnumerator PlayRollSequence(IReadOnlyList<Dice> diceDefinitions)
    {
        int diceCount = diceDefinitions != null && diceDefinitions.Count > 0 ? diceDefinitions.Count : 1;
        IsRolling = true;
        LastRollSum = 0;
        lastResults.Clear();

        boardAnchor?.SnapToTarget();
        FocusBoard();
        yield return new WaitForSeconds(preRollDelay);

        EnsureDicePool(diceDefinitions, diceCount);
        PositionDice(diceDefinitions, diceCount);
        RollDice(diceDefinitions, diceCount);

        yield return new WaitUntil(AllDiceResolved);

        LastRollSum = 0;
        lastResults.Clear();
        for (int i = 0; i < diceCount; i++)
        {
            int result = runtimeDice[i].runtimeDie.Result;
            lastResults.Add(result);
            LastRollSum += result;
        }

        yield return new WaitForSeconds(postRollDelay);
        ReleaseBoardFocus();
        yield return WaitForCameraToReturn();
        ParkDiceAtSpawnPoints(diceCount);
        IsRolling = false;
    }

    public void PrepareParkedDice(IReadOnlyList<Dice> diceDefinitions)
    {
        int diceCount = diceDefinitions != null && diceDefinitions.Count > 0 ? diceDefinitions.Count : 1;
        EnsureDicePool(diceDefinitions, diceCount);
        PositionDice(diceDefinitions, diceCount);
        ParkDiceAtSpawnPoints(diceCount);
    }

    public void SnapToFollowPosition(Vector3 followPosition)
    {
        boardAnchor?.SnapToPosition(followPosition);
    }

    private void LateUpdate()
    {
        if (IsRolling)
        {
            return;
        }

        MaintainParkedDice();
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

    private void EnsureDicePool(IReadOnlyList<Dice> diceDefinitions, int diceCount)
    {
        while (runtimeDice.Count < diceCount)
        {
            runtimeDice.Add(new RuntimeDieHandle());
        }

        for (int i = 0; i < runtimeDice.Count; i++)
        {
            bool shouldBeActive = i < diceCount;
            RuntimeDieHandle handle = runtimeDice[i];

            if (!shouldBeActive)
            {
                if (handle.runtimeDie != null)
                {
                    handle.runtimeDie.gameObject.SetActive(false);
                }

                continue;
            }

            Dice diceData = GetDiceDefinition(diceDefinitions, i);
            GameObject prefabSource = GetPrefabForDice(diceData);

            if (handle.runtimeDie == null || handle.prefabSource != prefabSource)
            {
                RecreateRuntimeDie(i, prefabSource);
                handle = runtimeDice[i];
            }

            handle.sourceData = diceData;
            handle.runtimeDie.ApplyDiceData(diceData);
            handle.runtimeDie.gameObject.SetActive(true);
            handle.isParked = false;
        }
    }

    private void RecreateRuntimeDie(int index, GameObject prefabSource)
    {
        RuntimeDieHandle handle = runtimeDice[index];
        if (handle.runtimeDie != null)
        {
            Destroy(handle.runtimeDie.gameObject);
        }

        DiceRuntimeDie runtimeDie = CreateDieInstance(index, prefabSource);
        runtimeDice[index] = new RuntimeDieHandle
        {
            prefabSource = prefabSource,
            runtimeDie = runtimeDie
        };
    }

    private DiceRuntimeDie CreateDieInstance(int index, GameObject prefabSource)
    {
        GameObject dieObject = prefabSource != null
            ? Instantiate(prefabSource, transform)
            : GameObject.CreatePrimitive(PrimitiveType.Cube);

        dieObject.name = $"RuntimeDie_{index + 1}";
        dieObject.transform.SetParent(transform, true);

        if (prefabSource == null)
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

    private void PositionDice(IReadOnlyList<Dice> diceDefinitions, int diceCount)
    {
        bool[] usedSpawnSlots = rollSpawnPoints != null ? new bool[rollSpawnPoints.Length] : null;
        Bounds trayBounds = CalculateTrayBounds();

        int configuredSpawnPointCount = GetConfiguredSpawnPointCount();
        if (configuredSpawnPointCount > 0 && configuredSpawnPointCount < diceCount && !hasWarnedAboutSpawnPointCount)
        {
            Debug.LogWarning(
                $"DiceBoard spawn point가 부족합니다. 설정된 위치 {configuredSpawnPointCount}개, 현재 주사위 {diceCount}개입니다. " +
                "나머지 주사위는 보드 내부 fallback 위치를 사용합니다.");
            hasWarnedAboutSpawnPointCount = true;
        }

        for (int i = 0; i < diceCount; i++)
        {
            RuntimeDieHandle handle = runtimeDice[i];
            Dice diceData = handle.sourceData;

            if (TryGetSpawnPointPose(diceData, i, usedSpawnSlots, out Transform parkedParent, out Vector3 spawnPosition, out Quaternion spawnRotation))
            {
                ApplyExplicitSpawnPointPose(handle, diceData, parkedParent, spawnRotation);
                continue;
            }

            Vector3 fallbackPosition = GetGridSpawnPosition(trayBounds, diceCount, i);
            Quaternion fallbackRotation = Random.rotation;
            ApplySpawnPose(handle, diceData, transform, fallbackPosition, fallbackRotation, false);
        }
    }

    private void ApplyExplicitSpawnPointPose(RuntimeDieHandle handle, Dice diceData, Transform parkedParent, Quaternion baseRotation)
    {
        Transform safeParent = parkedParent != null ? parkedParent : transform;
        Quaternion authoredRotation = Quaternion.Euler(diceData != null ? diceData.spawnEulerAngles : Vector3.zero);
        Quaternion finalLocalRotation = authoredRotation;
        Quaternion finalWorldRotation = safeParent.rotation * finalLocalRotation;

        handle.parkedParent = safeParent;
        handle.parkedLocalPosition = Vector3.zero;
        handle.parkedLocalRotation = finalLocalRotation;
        handle.isParked = false;

        handle.runtimeDie.transform.SetParent(transform, true);
        handle.runtimeDie.ResetForRoll(safeParent.position, finalWorldRotation);
    }

    private void ApplySpawnPose(RuntimeDieHandle handle, Dice diceData, Transform parkedParent, Vector3 basePosition, Quaternion baseRotation, bool useSpawnHeight)
    {
        Vector3 boardOffset = diceData != null ? diceData.boardSpawnOffset : Vector3.zero;
        Quaternion authoredRotation = Quaternion.Euler(diceData != null ? diceData.spawnEulerAngles : Vector3.zero);
        Quaternion finalRotation = baseRotation * authoredRotation;
        Vector3 worldOffset = baseRotation * boardOffset;

        Transform safeParent = parkedParent != null ? parkedParent : transform;
        Vector3 parkedWorldPosition = basePosition + worldOffset + Vector3.up * parkedHeight;
        handle.parkedParent = safeParent;
        handle.parkedLocalPosition = safeParent.InverseTransformPoint(parkedWorldPosition);
        handle.parkedLocalRotation = Quaternion.Inverse(safeParent.rotation) * finalRotation;
        handle.isParked = false;

        Vector3 rollStartPosition = basePosition + worldOffset + Vector3.up * (useSpawnHeight ? spawnHeight : parkedHeight);
        handle.runtimeDie.transform.SetParent(transform, true);
        handle.runtimeDie.ResetForRoll(rollStartPosition, finalRotation);
    }

    private void ParkDiceAtSpawnPoints(int diceCount)
    {
        for (int i = 0; i < diceCount; i++)
        {
            RuntimeDieHandle handle = runtimeDice[i];
            if (handle.runtimeDie == null)
            {
                continue;
            }

            handle.runtimeDie.Park(handle.parkedParent, handle.parkedLocalPosition, handle.parkedLocalRotation);
            handle.isParked = true;
        }
    }

    private void MaintainParkedDice()
    {
        for (int i = 0; i < runtimeDice.Count; i++)
        {
            RuntimeDieHandle handle = runtimeDice[i];
            if (handle.runtimeDie == null || !handle.runtimeDie.gameObject.activeSelf || !handle.isParked)
            {
                continue;
            }

            handle.runtimeDie.Park(handle.parkedParent, handle.parkedLocalPosition, handle.parkedLocalRotation);
        }
    }

    private bool TryGetSpawnPointPose(Dice diceData, int orderIndex, bool[] usedSpawnSlots, out Transform parkedParent, out Vector3 spawnPosition, out Quaternion spawnRotation)
    {
        parkedParent = null;
        spawnPosition = default;
        spawnRotation = Quaternion.identity;

        if (rollSpawnPoints == null || rollSpawnPoints.Length == 0)
        {
            return false;
        }

        int preferredSlot = diceData != null ? diceData.preferredBoardSlot : -1;
        if (preferredSlot >= 0 && preferredSlot < rollSpawnPoints.Length && !usedSpawnSlots[preferredSlot] && rollSpawnPoints[preferredSlot] != null)
        {
            usedSpawnSlots[preferredSlot] = true;
            parkedParent = rollSpawnPoints[preferredSlot];
            spawnPosition = parkedParent.position;
            spawnRotation = parkedParent.rotation;
            return true;
        }

        if (orderIndex >= 0 && orderIndex < rollSpawnPoints.Length && !usedSpawnSlots[orderIndex] && rollSpawnPoints[orderIndex] != null)
        {
            usedSpawnSlots[orderIndex] = true;
            parkedParent = rollSpawnPoints[orderIndex];
            spawnPosition = parkedParent.position;
            spawnRotation = parkedParent.rotation;
            return true;
        }

        for (int i = 0; i < rollSpawnPoints.Length; i++)
        {
            if (usedSpawnSlots[i] || rollSpawnPoints[i] == null)
            {
                continue;
            }

            usedSpawnSlots[i] = true;
            parkedParent = rollSpawnPoints[i];
            spawnPosition = parkedParent.position;
            spawnRotation = parkedParent.rotation;
            return true;
        }

        return false;
    }

    private Vector3 GetGridSpawnPosition(Bounds trayBounds, int gridCount, int gridIndex)
    {
        int columns = Mathf.CeilToInt(Mathf.Sqrt(Mathf.Max(1, gridCount)));
        int rows = Mathf.CeilToInt((float)Mathf.Max(1, gridCount) / columns);

        float usableWidth = Mathf.Max(1f, trayBounds.size.x - spawnMargin * 2f);
        float usableDepth = Mathf.Max(1f, trayBounds.size.z - spawnMargin * 2f);
        float xStep = columns > 1 ? usableWidth / (columns - 1) : 0f;
        float zStep = rows > 1 ? usableDepth / (rows - 1) : 0f;

        Vector3 start = new Vector3(
            trayBounds.center.x - usableWidth * 0.5f,
            trayBounds.max.y + spawnHeight,
            trayBounds.center.z - usableDepth * 0.5f);

        int column = gridIndex % columns;
        int row = gridIndex / columns;
        return start + new Vector3(column * xStep, 0f, row * zStep);
    }

    private void RollDice(IReadOnlyList<Dice> diceDefinitions, int diceCount)
    {
        for (int i = 0; i < diceCount; i++)
        {
            Dice diceData = GetDiceDefinition(diceDefinitions, i);
            float forceMultiplier = diceData != null ? Mathf.Max(0.1f, diceData.rollForceMultiplier) : 1f;
            float torqueMultiplier = diceData != null ? Mathf.Max(0.1f, diceData.torqueMultiplier) : 1f;

            Vector3 force = (Vector3.up * Random.Range(rollUpForceMin, rollUpForceMax)
                + transform.right * Random.Range(-rollSideForce, rollSideForce)
                + transform.forward * Random.Range(-rollSideForce, rollSideForce)) * forceMultiplier;
            Vector3 torque = Random.onUnitSphere * rollTorqueForce * torqueMultiplier;

            runtimeDice[i].runtimeDie.Roll(force, torque);
        }
    }

    private bool AllDiceResolved()
    {
        for (int i = 0; i < runtimeDice.Count; i++)
        {
            if (runtimeDice[i].runtimeDie == null || !runtimeDice[i].runtimeDie.gameObject.activeSelf)
            {
                continue;
            }

            if (!runtimeDice[i].runtimeDie.HasResolved)
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

    private IEnumerator WaitForCameraToReturn()
    {
        if (cameraFollow == null && Camera.main != null)
        {
            cameraFollow = Camera.main.GetComponent<CameraFollow>();
        }

        if (cameraFollow == null)
        {
            yield break;
        }

        yield return new WaitUntil(() => cameraFollow.IsNearFollowPose());
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

    private Dice GetDiceDefinition(IReadOnlyList<Dice> diceDefinitions, int index)
    {
        if (diceDefinitions == null || index < 0 || index >= diceDefinitions.Count)
        {
            return null;
        }

        return diceDefinitions[index];
    }

    private GameObject GetPrefabForDice(Dice diceData)
    {
        if (diceData != null && diceData.runtimePrefab != null)
        {
            return diceData.runtimePrefab;
        }

        return dicePrefab;
    }

    private int GetConfiguredSpawnPointCount()
    {
        if (rollSpawnPoints == null || rollSpawnPoints.Length == 0)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < rollSpawnPoints.Length; i++)
        {
            if (rollSpawnPoints[i] != null)
            {
                count++;
            }
        }

        return count;
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
