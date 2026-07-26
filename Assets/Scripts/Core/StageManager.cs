using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum FieldMode
{
    Title,
    Normal,
    Boss
}

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Field Settings")]
    public FieldMode fieldMode = FieldMode.Normal;
    public int targetDistance = 20;
    public bool bossMustBeDefeatedToClear = false;
    [SerializeField] private string nextSceneName = "BossFieldScene";
    [SerializeField] private float clearFadeDuration = 1.0f;
    [SerializeField] private float clearFadeHoldDuration = 0.5f;
    [SerializeField] private int normalStageClearGoldReward = 100;
    [Header("Normal To Boss Sequence")]
    [SerializeField] private List<Sprite> transitionImages = new List<Sprite>();
    [SerializeField] private float transitionImageDisplayDuration = 2.5f;
    [SerializeField] private float transitionImageFadeDuration = 0.35f;
    [SerializeField] private float transitionImageGapDuration = 0.15f;

    [Header("Runtime References")]
    public PlayerController player;
    public BossController boss;

    public int CurrentDistance { get; private set; }
    public int RemainingDistanceToGoal => Mathf.Max(0, targetDistance - CurrentDistance);
    public bool HasReachedGoalTrigger { get; private set; }
    public bool IsBossField => fieldMode == FieldMode.Boss;
    public bool IsStageResolved { get; private set; }
    public bool IsGameOver { get; private set; }

    private bool bossFollowInitialized;
    private bool stageTransitionRequested;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapStageManager()
    {
        if (IsShopScene(SceneManager.GetActiveScene().name))
        {
            return;
        }

        if (FindObjectOfType<StageManager>() != null)
        {
            return;
        }

        GameObject stageManagerObject = new GameObject("StageManager");
        stageManagerObject.AddComponent<StageManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        ApplySceneDefaults();
        EnsureRuntimeReferences();
        string activeSceneName = SceneManager.GetActiveScene().name;
        if (!IsShopScene(activeSceneName))
        {
            GameManager.Instance?.SetStageName(activeSceneName);
        }
    }

    public void AdvanceDistance(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        ModifyDistance(amount);
    }

    public void ModifyDistance(int amount)
    {
        if (IsStageResolved || amount == 0)
        {
            return;
        }

        int previousDistance = CurrentDistance;
        CurrentDistance = Mathf.Clamp(CurrentDistance + amount, 0, targetDistance);

        if (previousDistance == CurrentDistance)
        {
            return;
        }

        Debug.Log($"[StageManager] 진행도 {CurrentDistance}/{targetDistance} ({CurrentDistance - previousDistance:+#;-#;0})");
        EvaluateStageResolution();
    }

    public void ApplyPlayerDelta(float deltaX)
    {
        EnsureRuntimeReferences();

        if (IsStageResolved || !IsBossField || boss == null || Mathf.Approximately(deltaX, 0f))
        {
            return;
        }

        boss.ApplyPlayerDelta(deltaX);
    }

    public void DamageBoss(int amount)
    {
        if (IsStageResolved || !IsBossField || boss == null || amount <= 0)
        {
            return;
        }

        boss.TakeDamage(amount);
        EvaluateStageResolution();
    }

    public void ReachGoalTrigger()
    {
        if (IsStageResolved || HasReachedGoalTrigger)
        {
            return;
        }

        HasReachedGoalTrigger = true;
        Debug.Log("[StageManager] Cave 도착 감지");
        EvaluateStageResolution();
    }

    public IEnumerator ResolveEndTurn()
    {
        EnsureRuntimeReferences();

        if (IsStageResolved)
        {
            yield break;
        }

        if (IsBossField && boss != null && !boss.IsDead && player != null)
        {
            yield return boss.PerformEndTurnAttack(player);
        }

        if (IsBossField
            && !HasReachedGoalTrigger
            && GameManager.Instance != null
            && GameManager.Instance.PlayerHP <= 0)
        {
            TriggerGameOver("[StageManager] 플레이어가 쓰러졌습니다. 게임 오버.");
            yield break;
        }

        EvaluateStageResolution();
    }

    public void RestoreSavedFieldState(FieldSceneState savedState)
    {
        if (savedState == null)
        {
            return;
        }

        EnsureRuntimeReferences();
        IsStageResolved = false;
        IsGameOver = false;
        CurrentDistance = Mathf.Clamp(savedState.stageDistance, 0, targetDistance);
        HasReachedGoalTrigger = savedState.reachedGoalTrigger;

        if (IsBossField && boss != null)
        {
            boss.RestoreHP(savedState.bossCurrentHp);
        }
    }

    private void EvaluateStageResolution()
    {
        if (IsStageResolved)
        {
            return;
        }

        if (!IsBossField)
        {
            if (HasReachedGoalTrigger)
            {
                ClearStage();
            }

            return;
        }

        bool bossDefeated = boss != null && boss.IsDead;
        bool cleared = bossMustBeDefeatedToClear
            ? HasReachedGoalTrigger && bossDefeated
            : HasReachedGoalTrigger || bossDefeated;

        if (cleared)
        {
            ClearStage();
        }
    }

    private void ClearStage()
    {
        IsStageResolved = true;
        IsGameOver = false;
        Debug.Log("[StageManager] 스테이지 클리어!");

        if (!IsBossField)
        {
            if (!HandleNormalStageClear())
            {
                TransitionToNextStage();
            }
        }
    }

    private void EnsureRuntimeReferences()
    {
        if (player == null)
        {
            player = FindObjectOfType<PlayerController>();
        }

        if (IsBossField && boss == null)
        {
            boss = FindObjectOfType<BossController>();
        }

        if (IsBossField && boss != null && player != null && !bossFollowInitialized)
        {
            boss.InitializeFollow(player.transform);
            bossFollowInitialized = true;
        }
    }

    public void TriggerGameOver(string reason)
    {
        if (IsStageResolved)
        {
            return;
        }

        IsStageResolved = true;
        IsGameOver = true;
        Debug.Log(reason);
        GameOverUiController.Instance?.ShowGameOver();
    }

    private void ApplySceneDefaults()
    {
        if (fieldMode == FieldMode.Normal && IsBossScene(SceneManager.GetActiveScene().name))
        {
            fieldMode = FieldMode.Boss;
        }
    }

    private static bool IsBossScene(string sceneName)
    {
        return sceneName.IndexOf("Boss", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsShopScene(string sceneName)
    {
        return sceneName.IndexOf("Shop", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool HandleNormalStageClear()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager가 없어 일반 스테이지 클리어 후 상점 이동을 건너뜁니다.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogWarning("다음 보스 씬 이름이 비어 있어 상점 연출을 시작할 수 없습니다.");
            return false;
        }

        EnsureRuntimeReferences();
        DiceManager diceManager = FindFirstObjectByType<DiceManager>();
        if (player == null || diceManager == null)
        {
            Debug.LogWarning("상점 이동에 필요한 플레이어 또는 주사위 매니저를 찾지 못해 다음 스테이지로 바로 이동합니다.");
            return false;
        }

        int rewardGold = Mathf.Max(0, normalStageClearGoldReward);
        if (rewardGold > 0)
        {
            GameManager.Instance.AddCoin(rewardGold);
            Debug.Log($"[StageManager] 일반 스테이지 클리어 보상 지급: {rewardGold}G");
        }

        GameManager.Instance.EnterShopFromField(
            player,
            diceManager,
            this,
            TurnPhase.End,
            shopExitTransition: CreatePendingBossSceneTransition());
        stageTransitionRequested = true;
        return true;
    }

    private PendingSceneTransition CreatePendingBossSceneTransition()
    {
        return new PendingSceneTransition
        {
            targetSceneName = nextSceneName,
            transitionImages = transitionImages != null ? new List<Sprite>(transitionImages) : new List<Sprite>(),
            fadeDuration = clearFadeDuration,
            fadeHoldDuration = clearFadeHoldDuration,
            imageDisplayDuration = transitionImageDisplayDuration,
            imageFadeDuration = transitionImageFadeDuration,
            imageGapDuration = transitionImageGapDuration
        };
    }

    public void TransitionToNextStage()
    {
        if (stageTransitionRequested)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogWarning("다음 스테이지 씬 이름이 비어 있어 전환을 건너뜁니다.");
            return;
        }

        stageTransitionRequested = true;
        if (transitionImages != null && transitionImages.Count > 0)
        {
            SceneTransitionFader.Instance.FadeThroughImagesToScene(
                nextSceneName,
                transitionImages,
                clearFadeDuration,
                clearFadeHoldDuration,
                transitionImageDisplayDuration,
                transitionImageFadeDuration,
                transitionImageGapDuration);
            return;
        }

        SceneTransitionFader.Instance.FadeToScene(nextSceneName, clearFadeDuration, clearFadeHoldDuration);
    }
}
