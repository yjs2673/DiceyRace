using System.Collections;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum FieldMode
{
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

    [Header("Runtime References")]
    public PlayerController player;
    public BossController boss;

    public int CurrentDistance { get; private set; }
    public bool IsBossField => fieldMode == FieldMode.Boss;
    public bool IsStageResolved { get; private set; }

    private bool bossFollowInitialized;

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

        if (GameManager.Instance != null && GameManager.Instance.PlayerHP <= 0)
        {
            IsStageResolved = true;
            Debug.Log("[StageManager] 플레이어가 쓰러졌습니다. 게임 오버.");
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
        CurrentDistance = Mathf.Clamp(savedState.stageDistance, 0, targetDistance);

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

        bool reachedTarget = CurrentDistance >= targetDistance;

        if (!IsBossField)
        {
            if (reachedTarget)
            {
                ClearStage();
            }

            return;
        }

        bool bossDefeated = boss != null && boss.IsDead;
        bool cleared = bossMustBeDefeatedToClear
            ? reachedTarget && bossDefeated
            : reachedTarget || bossDefeated;

        if (cleared)
        {
            ClearStage();
        }
    }

    private void ClearStage()
    {
        IsStageResolved = true;
        Debug.Log("[StageManager] 스테이지 클리어!");
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
}
