using System.Collections;
// using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DiceManager : MonoBehaviour
{
    [Header("UI References")]
    public Button rollButton;
    public Text diceText;               // 주사위 결과
    public Text remainingRerollText;    // 남은 리롤 횟수
    public Text remainingMoveText;      // 남은 이동 횟수

    [Header("Movement Settings")]
    public PlayerController player;
    public float moveDuration = 0.3f;   // 한 칸 이동하는 시간
    public float moveDistance = 1f;     // 한 칸 이동하는 거리
    public int remainingRerolls = 0;    // 남은 리롤 횟수

    [Header("Test Settings")]
    public bool enableTestMode = false; // 테스트 모드 활성화
    public int testMaxNum = 6;
    public int testMinNum = 1;

    private int currentDiceValue = 0;
    private int remainingMoves = 0;
    private bool isMoving = false;
    private bool isMoveRoutineQueued = false;
    private Coroutine activeMoveRoutine;

    private bool isKnockedBack = false; // 넉백 상태인지 여부
    private readonly WaitForFixedUpdate fixedUpdateYield = new WaitForFixedUpdate();

    public bool IsMoving => isMoving;
    public float StepDistance => moveDistance;
    public int RemainingMoves => remainingMoves;
    public int RemainingRerolls => remainingRerolls;
    public int CurrentDiceValue => currentDiceValue;

    private void Start()
    {
        // 버튼 클릭 이벤트 연결
        rollButton.onClick.AddListener(RollDice);
        UpdateUI(0, remainingRerolls);
        SubscribeTurnManager();
        RefreshRollButtonState();
        CaptureFieldCheckpoint();
    }

    private void OnDestroy()
    {
        if (rollButton != null)
        {
            rollButton.onClick.RemoveListener(RollDice);
        }

        UnsubscribeTurnManager();
    }

    public void RollDice()
    {
        // Standby 페이즈가 아니면 주사위 굴리기 금지
        if (TurnManager.Instance.CurrentPhase != TurnPhase.Standby) return;
        // 이동 중이거나 남은 횟수가 있으면 주사위 굴리기 금지
        if (isMoving || remainingRerolls <= 0 || remainingMoves > 0) return;

        int diceValue = Random.Range(testMinNum, testMaxNum + 1);
        currentDiceValue = diceValue;
        diceText.text = $"주사위: {diceValue}";

        remainingRerolls--;
        remainingMoves = diceValue;
        UpdateUI(remainingMoves, remainingRerolls);

        RefreshRollButtonState();

        TurnManager.Instance.SetPhase(TurnPhase.Move); // Move 페이즈로 전환

        StartMoveRoutine();
    }

    // PlayerController에서 충돌 시 호출할 넉백 함수
    public void ApplyPenaltyKnockback()
    {
        if (!isMoving || remainingMoves > 0) return;
        isKnockedBack = true;
    }

    // 플레이어 이동 코루틴
    private IEnumerator MoveRoutine()
    {
        isMoveRoutineQueued = false;
        isMoving = true;
        player.SetAutoMoveAnimation(true);
        float moveSpeed = moveDistance / moveDuration;

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
            Vector3 startPos = player.PhysicsPosition;
            float movedDistance = 0f;

            // 칸 판정은 유지하되, 대기 없이 연속적으로 전진
            while (movedDistance < moveDistance)
            {
                /*if (isKnockedBack && !enableTestMode)     // 넉백 로직 일단 주석 처리
                {
                    player.SnapToPosition(startPos);
                    CheckStopTile();
                    UpdateUI(remainingMoves, remainingRerolls);
                    isKnockedBack = false;
                    remainingMoves = 0;
                    break;
                }*/

                float delta = Mathf.Min(moveSpeed * Time.fixedDeltaTime, moveDistance - movedDistance);
                player.ApplyMove(delta); // PlayerController자동 이동 적용
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

            // 이동이 모두 끝났을 때 정지 발판 체크
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

        // 루프 종료 후 남은 이동 수 UI 동기화 방어코드
        if (remainingMoves <= 0)
        {
            remainingMoves = 0;
            UpdateUI(remainingMoves, remainingRerolls);
        }

        isMoving = false;
        activeMoveRoutine = null;
        player.SetAutoMoveAnimation(false);
        rollButton.interactable = true;

        // 엔드 턴으로 넘어가기
        TurnManager.Instance.SetPhase(TurnPhase.End);
    }

    public void AddReroll(int amount)
    {
        remainingRerolls += amount;
        Debug.Log($"리롤 횟수 증가: {amount} -> 남은 리롤: {remainingRerolls}");
        CaptureFieldCheckpoint();
    }

    public void QueueForcedMove(int amount)
    {
        if (amount == 0)
        {
            return;
        }

        if (isMoving)
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
        UpdateUI(remainingMoves, remainingRerolls);

        if (TurnManager.Instance != null && TurnManager.Instance.CurrentPhase == TurnPhase.End)
        {
            StageManager.Instance?.ModifyDistance(amount);
            remainingMoves = 0;
            UpdateUI(remainingMoves, remainingRerolls);
            return;
        }

        if (rollButton != null)
        {
            RefreshRollButtonState();
        }

        if (TurnManager.Instance != null && TurnManager.Instance.CurrentPhase != TurnPhase.Move)
        {
            TurnManager.Instance.SetPhase(TurnPhase.Move);
        }

        StartMoveRoutine();
    }

    public void RestoreSavedFieldState(FieldSceneState savedState)
    {
        if (savedState == null)
        {
            return;
        }

        currentDiceValue = savedState.currentDiceValue;
        remainingMoves = Mathf.Max(0, savedState.remainingMoves);
        remainingRerolls = Mathf.Max(0, savedState.remainingRerolls);
        isMoving = false;
        isKnockedBack = false;

        if (diceText != null)
        {
            diceText.text = currentDiceValue > 0
                ? $"주사위: {currentDiceValue}"
                : "주사위: -";
        }

        UpdateUI(remainingMoves, remainingRerolls);

        if (rollButton != null)
        {
            RefreshRollButtonState();
        }

        player?.SetAutoMoveAnimation(false);
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

    // PlayerController에서 충돌 시 호출할 메서드
    public void ModifyMoves(int amount)
    {
        if (!isMoving) return;

        remainingMoves += amount;
        if (remainingMoves < 0)
        {
            remainingMoves = 0; // 음수 방지
        }

        UpdateUI(remainingMoves, remainingRerolls);
        Debug.Log($"이동 수 변경: {amount} -> 남은 이동 수: {remainingMoves}");
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

        bool isStandbyPhase = TurnManager.Instance != null && TurnManager.Instance.CurrentPhase == TurnPhase.Standby;
        rollButton.interactable = isStandbyPhase && !isMoving && remainingRerolls > 0 && remainingMoves <= 0;
    }

    private bool HasReachedStageGoal()
    {
        return StageManager.Instance != null
            && (StageManager.Instance.IsStageResolved || StageManager.Instance.RemainingDistanceToGoal <= 0);
    }

    private void StartMoveRoutine()
    {
        if (isMoving || isMoveRoutineQueued || activeMoveRoutine != null)
        {
            return;
        }

        isMoveRoutineQueued = true;
        activeMoveRoutine = StartCoroutine(MoveRoutine());
    }

    private void CheckStopTile()
    {
        // Raycast를 사용하여 정지 발판 구분
        if (Physics.Raycast(player.PhysicsPosition, Vector3.down, out RaycastHit hit, 2f))
        {
            if (hit.collider.CompareTag("Tile"))
            {
                Tile tile = hit.collider.GetComponent<Tile>();

                if (tile != null && tile.tileType == TileType.Stop)
                {
                    EffectProcessor.ApplyTileEffect(tile, this, player);
                }
            }
        }
    }
}
