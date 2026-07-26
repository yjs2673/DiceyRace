using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DiceManager : MonoBehaviour
{
    [Header("UI References")]
    public Button rollButton;
    public Text diceText;
    public Text remainingRerollText;
    public Text remainingMoveText;

    [Header("Movement Settings")]
    public PlayerController player;
    public float moveDuration = 0.3f;
    public float moveDistance = 1f;
    public int remainingRerolls = 0;
    [SerializeField] private float turnMoveSpeedMultiplier = 1f;

    [Header("Dice Board")]
    [SerializeField] private Transform diceBoardRoot;
    [SerializeField] public float autoMoveDelay = 1.5f;
    [SerializeField] private bool createFallbackBoardIfMissing = true;
    [SerializeField] private bool fallbackToLegacyRollIfBoardUnavailable = false;

    [Header("Test Settings")]
    public bool enableTestMode = false;
    public int testMaxNum = 6;
    public int testMinNum = 1;

    private int currentDiceValue;
    private int remainingMoves;
    private bool isMoving;
    private bool isMoveRoutineQueued;
    private bool isKnockedBack;
    private bool isRollingDice;
    private bool isAwaitingMoveStart;
    private Coroutine activeMoveRoutine;
    private Coroutine pendingAutoMoveRoutine;
    private DiceBoardController diceBoardController;
    private readonly WaitForFixedUpdate fixedUpdateYield = new WaitForFixedUpdate();

    public bool IsMoving => isMoving;
    public bool IsRollingDice => isRollingDice;
    public bool HasPendingMoveBudget => isMoving || isAwaitingMoveStart || remainingMoves > 0;
    public float StepDistance => moveDistance;
    public int RemainingMoves => remainingMoves;
    public int RemainingRerolls => remainingRerolls;
    public int CurrentDiceValue => currentDiceValue;
    public float TurnMoveSpeedMultiplier => turnMoveSpeedMultiplier;

    private void Start()
    {
        if (rollButton != null)
        {
            rollButton.onClick.AddListener(HandlePrimaryActionButton);
        }

        EnsureDiceBoardController();
        PrepareParkedDiceBoard();
        UpdateDiceLabel("주사위: -");
        UpdateUI(remainingMoves, remainingRerolls);
        SubscribeTurnManager();
        RefreshRollButtonState();
        CaptureFieldCheckpoint();
    }

    private void OnDestroy()
    {
        if (rollButton != null)
        {
            rollButton.onClick.RemoveListener(HandlePrimaryActionButton);
        }

        CancelPendingAutoMove();
        UnsubscribeTurnManager();
    }

    public void RollDice()
    {
        if (TurnManager.Instance == null || TurnManager.Instance.CurrentPhase != TurnPhase.Standby)
        {
            return;
        }

        if (isMoving || isRollingDice || isAwaitingMoveStart || remainingRerolls <= 0 || remainingMoves > 0)
        {
            return;
        }

        bool canUseBoard = EnsureDiceBoardController() && diceBoardController != null && diceBoardController.CanRoll;
        if (!canUseBoard && !fallbackToLegacyRollIfBoardUnavailable)
        {
            Debug.LogWarning("DiceBoardController를 준비하지 못해 3D 주사위 굴리기를 시작할 수 없습니다.");
            return;
        }

        remainingRerolls--;
        UpdateUI(remainingMoves, remainingRerolls);
        RefreshRollButtonState();

        if (canUseBoard)
        {
            StartCoroutine(RollDiceBoardRoutine());
            return;
        }

        ApplyRollResult(Random.Range(testMinNum, testMaxNum + 1), null);
    }

    public void ApplyPenaltyKnockback()
    {
        if (!isMoving || remainingMoves > 0)
        {
            return;
        }

        isKnockedBack = true;
    }

    public void AddReroll(int amount)
    {
        remainingRerolls += amount;
        UpdateUI(remainingMoves, remainingRerolls);
        RefreshRollButtonState();
        Debug.Log($"리롤 횟수 증가: {amount} -> 남은 리롤: {remainingRerolls}");
        CaptureFieldCheckpoint();
    }

    public void QueueForcedMove(int amount)
    {
        if (amount == 0)
        {
            return;
        }

        if (isMoving || isAwaitingMoveStart)
        {
            ModifyMoves(amount);
            return;
        }

        if (amount < 0)
        {
            StageManager.Instance?.ModifyDistance(amount);
            return;
        }

        remainingMoves += amount;
        currentDiceValue = Mathf.Max(currentDiceValue, remainingMoves);
        UpdateUI(remainingMoves, remainingRerolls);

        if (TurnManager.Instance != null && TurnManager.Instance.CurrentPhase == TurnPhase.End)
        {
            StageManager.Instance?.ModifyDistance(amount);
            remainingMoves = 0;
            UpdateUI(remainingMoves, remainingRerolls);
            return;
        }

        if (TurnManager.Instance != null && TurnManager.Instance.CurrentPhase != TurnPhase.Move)
        {
            TurnManager.Instance.SetPhase(TurnPhase.Move);
        }

        StartMoveRoutine();
    }

    public void SetTurnMoveSpeedMultiplier(float multiplier, bool captureCheckpoint = true)
    {
        float normalizedMultiplier = Mathf.Max(0.1f, multiplier);
        if (Mathf.Approximately(turnMoveSpeedMultiplier, normalizedMultiplier))
        {
            return;
        }

        turnMoveSpeedMultiplier = normalizedMultiplier;
        Debug.Log($"이동 속도 배율 변경 -> {turnMoveSpeedMultiplier:0.##}배");

        if (captureCheckpoint)
        {
            CaptureFieldCheckpoint();
        }
    }

    public void StopRemainingMoves()
    {
        if (remainingMoves <= 0 && !isAwaitingMoveStart && !isMoving)
        {
            return;
        }

        CancelPendingAutoMove();
        isAwaitingMoveStart = false;
        remainingMoves = 0;
        UpdateUI(remainingMoves, remainingRerolls);
        RefreshRollButtonState();
        CaptureFieldCheckpoint();
        Debug.Log("남은 이동을 즉시 종료합니다.");

        if (!isMoving && TurnManager.Instance != null && TurnManager.Instance.CurrentPhase == TurnPhase.Move)
        {
            TurnManager.Instance.SetPhase(TurnPhase.End);
        }
    }

    public void RestoreSavedFieldState(FieldSceneState savedState)
    {
        if (savedState == null)
        {
            return;
        }

        CancelPendingAutoMove();
        if (activeMoveRoutine != null)
        {
            StopCoroutine(activeMoveRoutine);
            activeMoveRoutine = null;
        }

        currentDiceValue = savedState.currentDiceValue;
        remainingMoves = Mathf.Max(0, savedState.remainingMoves);
        remainingRerolls = Mathf.Max(0, savedState.remainingRerolls);
        isMoving = false;
        isMoveRoutineQueued = false;
        isKnockedBack = false;
        isRollingDice = false;
        isAwaitingMoveStart = false;

        UpdateDiceLabel(currentDiceValue > 0
            ? $"주사위 합: {currentDiceValue}"
            : "주사위: -");
        UpdateUI(remainingMoves, remainingRerolls);
        RefreshRollButtonState();
        player?.SetAutoMoveAnimation(false);
        PrepareParkedDiceBoard();
    }

    public void ModifyMoves(int amount)
    {
        if (!HasPendingMoveBudget)
        {
            return;
        }

        remainingMoves += amount;
        if (remainingMoves < 0)
        {
            remainingMoves = 0;
        }

        UpdateUI(remainingMoves, remainingRerolls);
        Debug.Log($"이동 수 변경: {amount} -> 남은 이동 수: {remainingMoves}");

        if (isAwaitingMoveStart)
        {
            if (remainingMoves <= 0)
            {
                CancelPendingAutoMove();
                isAwaitingMoveStart = false;
                RefreshRollButtonState();
                if (TurnManager.Instance != null && TurnManager.Instance.CurrentPhase == TurnPhase.Move)
                {
                    TurnManager.Instance.SetPhase(TurnPhase.End);
                }
            }
            else
            {
                UpdateDiceLabel($"주사위 합: {currentDiceValue}\n버튼을 눌러서 이동");
            }
        }

        CaptureFieldCheckpoint();
    }

    private void HandlePrimaryActionButton()
    {
        if (isAwaitingMoveStart)
        {
            BeginMovementNow();
            return;
        }

        RollDice();
    }

    private IEnumerator RollDiceBoardRoutine()
    {
        isRollingDice = true;
        RefreshRollButtonState();

        List<Dice> ownedDice = GetOwnedDiceDefinitions();
        yield return diceBoardController.PlayRollSequence(ownedDice);

        isRollingDice = false;
        ApplyRollResult(diceBoardController.LastRollSum, diceBoardController.LastRollResults);
    }

    private void ApplyRollResult(int diceValue, IReadOnlyList<int> results)
    {
        currentDiceValue = Mathf.Max(0, diceValue);
        remainingMoves = currentDiceValue;

        if (results != null && results.Count > 0)
        {
            string breakdown = string.Join(" + ", results);
            UpdateDiceLabel($"주사위 합: {currentDiceValue}\n({breakdown})");
        }
        else
        {
            UpdateDiceLabel($"주사위 합: {currentDiceValue}");
        }

        UpdateUI(remainingMoves, remainingRerolls);
        CaptureFieldCheckpoint();

        if (remainingMoves <= 0)
        {
            RefreshRollButtonState();
            TurnManager.Instance?.SetPhase(TurnPhase.End);
            return;
        }

        TurnManager.Instance?.SetPhase(TurnPhase.Move);
        BeginAwaitingMoveStart();
    }

    private void BeginAwaitingMoveStart()
    {
        if (remainingMoves <= 0)
        {
            return;
        }

        CancelPendingAutoMove();
        isAwaitingMoveStart = true;
        UpdateDiceLabel($"주사위 합: {currentDiceValue}\n버튼을 누르거나 잠시 후 이동");
        RefreshRollButtonState();

        if (autoMoveDelay > 0f)
        {
            pendingAutoMoveRoutine = StartCoroutine(AutoMoveAfterDelayRoutine());
        }
    }

    private IEnumerator AutoMoveAfterDelayRoutine()
    {
        yield return new WaitForSeconds(autoMoveDelay);
        pendingAutoMoveRoutine = null;

        if (isAwaitingMoveStart && !isMoving && remainingMoves > 0)
        {
            BeginMovementNow();
        }
    }

    private void BeginMovementNow()
    {
        if (!isAwaitingMoveStart || remainingMoves <= 0)
        {
            return;
        }

        CancelPendingAutoMove();
        isAwaitingMoveStart = false;
        RefreshRollButtonState();
        StartMoveRoutine();
    }

    private IEnumerator MoveRoutine()
    {
        isMoveRoutineQueued = false;
        isMoving = true;
        player.SetAutoMoveAnimation(true);
        RefreshRollButtonState();

        while (remainingMoves > 0)
        {
            if (HasReachedStageGoal())
            {
                remainingMoves = 0;
                UpdateUI(remainingMoves, remainingRerolls);
                break;
            }

            remainingMoves--;
            isKnockedBack = false;
            float movedDistance = 0f;

            while (movedDistance < moveDistance)
            {
                float moveSpeed = (moveDistance / Mathf.Max(0.01f, moveDuration)) * Mathf.Max(0.1f, turnMoveSpeedMultiplier);
                float delta = Mathf.Min(moveSpeed * Time.fixedDeltaTime, moveDistance - movedDistance);
                player.ApplyMove(delta);
                movedDistance += delta;

                yield return fixedUpdateYield;
            }

            if (isKnockedBack && !enableTestMode)
            {
                break;
            }

            StageManager.Instance?.AdvanceDistance(1);
            player?.OnMoveStepCompleted(StageManager.Instance != null ? StageManager.Instance.CurrentDistance : 0);
            UpdateUI(remainingMoves, remainingRerolls);
            CaptureFieldCheckpoint();

            if (HasReachedStageGoal())
            {
                remainingMoves = 0;
                UpdateUI(remainingMoves, remainingRerolls);
                break;
            }

            if (remainingMoves <= 0)
            {
                CheckStopTile();
                break;
            }

            if (StageManager.Instance != null && StageManager.Instance.IsStageResolved)
            {
                remainingMoves = 0;
                UpdateUI(remainingMoves, remainingRerolls);
                break;
            }
        }

        if (remainingMoves <= 0)
        {
            remainingMoves = 0;
            UpdateUI(remainingMoves, remainingRerolls);
        }

        isMoving = false;
        activeMoveRoutine = null;
        player.SetAutoMoveAnimation(false);
        RefreshRollButtonState();
        TurnManager.Instance?.SetPhase(TurnPhase.End);
    }

    private void CaptureFieldCheckpoint()
    {
        if (GameManager.Instance == null || player == null || StageManager.Instance == null)
        {
            return;
        }

        TurnPhase phase = TurnManager.Instance != null
            ? TurnManager.Instance.CurrentPhase
            : TurnPhase.Standby;
        GameManager.Instance.CaptureFieldCheckpoint(player, this, StageManager.Instance, phase);
    }

    private void SubscribeTurnManager()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPhaseChanged += HandlePhaseChanged;
        }
    }

    private void UnsubscribeTurnManager()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPhaseChanged -= HandlePhaseChanged;
        }
    }

    private void HandlePhaseChanged(TurnPhase phase)
    {
        RefreshRollButtonState();
    }

    private void RefreshRollButtonState()
    {
        if (rollButton == null)
        {
            return;
        }

        if (isRollingDice)
        {
            rollButton.interactable = false;
            return;
        }

        if (isAwaitingMoveStart)
        {
            rollButton.interactable = remainingMoves > 0 && !isMoving;
            return;
        }

        bool isStandbyPhase = TurnManager.Instance != null && TurnManager.Instance.CurrentPhase == TurnPhase.Standby;
        rollButton.interactable = isStandbyPhase && !isMoving && remainingRerolls > 0 && remainingMoves <= 0;
    }

    private bool HasReachedStageGoal()
    {
        return StageManager.Instance != null
            && (StageManager.Instance.IsStageResolved || StageManager.Instance.HasReachedGoalTrigger);
    }

    private void StartMoveRoutine()
    {
        if (isMoving || isMoveRoutineQueued || activeMoveRoutine != null || remainingMoves <= 0)
        {
            return;
        }

        isMoveRoutineQueued = true;
        activeMoveRoutine = StartCoroutine(MoveRoutine());
    }

    private void CancelPendingAutoMove()
    {
        if (pendingAutoMoveRoutine == null)
        {
            return;
        }

        StopCoroutine(pendingAutoMoveRoutine);
        pendingAutoMoveRoutine = null;
    }

    private void UpdateDiceLabel(string message)
    {
        if (diceText != null)
        {
            diceText.text = message;
        }
    }

    private void UpdateUI(int currentMoves, int currentRerolls)
    {
        if (remainingMoveText != null)
        {
            remainingMoveText.text = $"남은 이동: {currentMoves}";
        }

        if (remainingRerollText != null)
        {
            remainingRerollText.text = $"남은 리롤: {currentRerolls}";
        }
    }

    private List<Dice> GetOwnedDiceDefinitions()
    {
        List<Dice> ownedDice = new List<Dice>();
        if (GameManager.Instance == null || GameManager.Instance.HasDice == null)
        {
            return ownedDice;
        }

        for (int i = 0; i < GameManager.Instance.HasDice.Count; i++)
        {
            if (GameManager.Instance.HasDice[i] != null)
            {
                ownedDice.Add(GameManager.Instance.HasDice[i]);
            }
        }

        return ownedDice;
    }

    private void PrepareParkedDiceBoard()
    {
        if (!EnsureDiceBoardController() || diceBoardController == null)
        {
            return;
        }

        diceBoardController.PrepareParkedDice(GetOwnedDiceDefinitions());
    }

    private bool EnsureDiceBoardController()
    {
        if (diceBoardController == null)
        {
            Transform boardRoot = diceBoardRoot;
            if (boardRoot == null)
            {
                GameObject boardObject = GameObject.Find("DiceBoard");
                boardRoot = boardObject != null ? boardObject.transform : null;
            }

            if (boardRoot == null && createFallbackBoardIfMissing)
            {
                diceBoardController = DiceBoardController.CreateRuntimeFallbackBoard();
                boardRoot = diceBoardController != null ? diceBoardController.transform : null;
            }

            if (boardRoot != null && diceBoardController == null)
            {
                diceBoardController = boardRoot.GetComponent<DiceBoardController>();
                if (diceBoardController == null)
                {
                    diceBoardController = boardRoot.gameObject.AddComponent<DiceBoardController>();
                }
            }
        }

        if (diceBoardController == null)
        {
            return false;
        }

        diceBoardController.Configure(player != null ? player.transform : null);
        return true;
    }

    private void CheckStopTile()
    {
        if (Physics.Raycast(player.PhysicsPosition, Vector3.down, out RaycastHit hit, 2f))
        {
            if (!hit.collider.CompareTag("Tile"))
            {
                return;
            }

            Tile tile = hit.collider.GetComponent<Tile>();
            if (tile != null && tile.tileType == TileType.Stop)
            {
                EffectProcessor.ApplyTileEffect(tile, this, player);
            }
        }
    }
}
