using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class PlayerCardRuntimeState
{
    public bool gamblerActive;
    public int gamblerSwing;
    public int nextGamblerThreshold;
    public bool dashAfterEvadeActive;
    public int dashAfterEvadeDistance;
    public int hedonismBonusDistance;
    public int parryGodBonusDistance;
    public int destroyerBonusDistance;
    public int jumpCrazyBonusDistance;
    public bool parryReflectActive;
    public int parryReflectDamage;
    public int remainingAttackParryCount;
    public bool attackParryForTurn;
    public int remainingAttackDestroyCount;
    public bool attackDestroyForTurn;
    public int remainingBlockParryCount;
    public bool blockParryForTurn;
    public int remainingIgnoredMoveTileCount;
    public bool ignoreMoveTileEffectsForTurn;
    public float turnMoveSpeedMultiplier;
}

[System.Serializable]
public class FieldSceneState
{
    public string sceneName;
    public string stageName;
    public Vector3 playerPosition;
    public int stageDistance;
    public bool reachedGoalTrigger;
    public int remainingMoves;
    public int remainingRerolls;
    public int currentDiceValue;
    public int playerInvincibleMoveCount;
    public int playerIgnoreHitCount;
    public int bossCurrentHp;
    public TurnPhase returnPhase;
    public PlayerCardRuntimeState playerCardRuntimeState;
}

[System.Serializable]
public class PendingSceneTransition
{
    public string targetSceneName;
    public List<Sprite> transitionImages = new List<Sprite>();
    public float fadeDuration = 1f;
    public float fadeHoldDuration = 0.5f;
    public float imageDisplayDuration = 2.5f;
    public float imageFadeDuration = 0.35f;
    public float imageGapDuration = 0.15f;
}

[DefaultExecutionOrder(-1000)]
public class GameManager : MonoBehaviour
{
    private static class RuntimeCache
    {
        public static bool HasCache;
        public static bool HasStartupDefaults;
        public static int InitialCoin;
        public static int InitialPirateCoin;
        public static int InitialReroll;
        public static int InitialPlayerHP;
        public static int InitialPlayerDamage;
        public static string InitialStageName;
        public static List<Dice> InitialDice = new List<Dice>();
        public static List<CardData> InitialCards = new List<CardData>();
        public static int Coin;
        public static int PirateCoin;
        public static int Reroll;
        public static int PlayerHP;
        public static int PlayerDamage;
        public static string StageName;
        public static List<Dice> HasDice = new List<Dice>();
        public static List<CardData> OwnedCards = new List<CardData>();
        public static List<CardData> TurnCards = new List<CardData>();
        public static FieldSceneState SavedFieldState;
        public static FieldSceneState LatestFieldCheckpoint;
        public static PendingSceneTransition PendingShopExitTransition;
    }

    public static GameManager Instance { get; private set; }
    private const string DefaultShopSceneName = "ShopScene";

    // 게임 시작 시 초기값 설정 //
    [SerializeField] private int initialCoin = 1000;
    [SerializeField] private int initialPirateCoin = 100;
    [SerializeField] private int initialReroll = 999;
    [SerializeField] private int initialPlayerHP = 100;
    [SerializeField] private int initialPlayerDamage = 10;
    [SerializeField] private string initialStageName = "DebugGround";
    [Header("테스트용 시작 인벤토리")]
    [SerializeField] private List<CardData> initialCards;
    [SerializeField] private List<Dice> initialDice;

    [Header("Scene Transition")]
    [SerializeField] private float shopFadeDuration = 0.75f;
    [SerializeField] private float shopFadeHoldDuration = 0.2f;
    
    [Header("Runtime Debug")]
    [SerializeField] private int coin;
    [SerializeField] private int pirateCoin;
    [SerializeField] private int reroll;
    [SerializeField] private int playerHP;
    [SerializeField] private int playerDamage;
    [SerializeField] private string stageName;
    [SerializeField] private List<Dice> hasDice = new List<Dice>();
    [SerializeField] private List<CardData> hasCard = new List<CardData>();
    [SerializeField] private List<CardData> currentTurnCards = new List<CardData>();
    [SerializeField] private FieldSceneState savedFieldState;
    [SerializeField] private FieldSceneState latestFieldCheckpoint;
    [SerializeField] private PendingSceneTransition pendingShopExitTransition;
    [SerializeField] private bool hasSavedFieldStateDebug;
    [SerializeField] private string activeSceneNameDebug;
    [SerializeField] private int instanceIdDebug;
    [SerializeField] private string owningSceneDebug;
    [SerializeField] private bool isPrimaryInstanceDebug;
    [SerializeField] private string savedFieldStateSummaryDebug;
    [SerializeField] private string latestFieldCheckpointSummaryDebug;

    public int Coin => coin;
    public int PirateCoin => pirateCoin;
    public int Reroll => reroll;
    public int PlayerHP => playerHP;
    public int PlayerDamage => playerDamage;
    public string StageName => stageName;

    public IReadOnlyList<Dice> HasDice => hasDice;
    public IReadOnlyList<CardData> HasCard => hasCard;
    public IReadOnlyList<CardData> CurrentTurnCards => currentTurnCards;
    public bool HasSavedFieldState => savedFieldState != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                $"중복 GameManager 감지 -> 새 인스턴스:{GetInstanceID()} ({gameObject.scene.name}), " +
                $"기존 싱글톤:{Instance.GetInstanceID()} ({Instance.gameObject.scene.name})");
            DestroyImmediate(gameObject);
            return;
        }

        Instance = this;
        gameObject.name = "GameManager [Persistent]";
        DontDestroyOnLoad(gameObject);
        CacheStartupDefaultsIfNeeded();

        if (RuntimeCache.HasCache)
        {
            RestoreFromRuntimeCache();
        }
        else
        {
            InitDefaultData();
        }

        RefreshRuntimeDebugInfo();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void InitDefaultData()
    {
        coin = RuntimeCache.HasStartupDefaults ? RuntimeCache.InitialCoin : initialCoin;
        pirateCoin = RuntimeCache.HasStartupDefaults ? RuntimeCache.InitialPirateCoin : initialPirateCoin;
        reroll = RuntimeCache.HasStartupDefaults ? RuntimeCache.InitialReroll : initialReroll;
        playerHP = RuntimeCache.HasStartupDefaults ? RuntimeCache.InitialPlayerHP : initialPlayerHP;
        playerDamage = RuntimeCache.HasStartupDefaults ? RuntimeCache.InitialPlayerDamage : initialPlayerDamage;
        stageName = RuntimeCache.HasStartupDefaults ? RuntimeCache.InitialStageName : initialStageName;
        hasCard.Clear();
        currentTurnCards.Clear();
        hasDice.Clear();
        savedFieldState = null;
        latestFieldCheckpoint = null;
        pendingShopExitTransition = null;

        if (RuntimeCache.HasStartupDefaults)
        {
            hasCard.AddRange(RuntimeCache.InitialCards);
            hasDice.AddRange(RuntimeCache.InitialDice);
        }
        else
        {
            if (initialCards != null)
                hasCard.AddRange(initialCards);

            if (initialDice != null)
                hasDice.AddRange(initialDice);
        }

        RefreshRuntimeDebugInfo();
    }
    // coin
    public void AddCoin(int amount)
    {
        UpdateCoin(coin + amount);
    }

    public bool SpendCoin(int amount)
    {
        if (coin < amount)
            return false;

        UpdateCoin(coin - amount);
        return true;
    }
    // pirate coin
    public void AddPirateCoin(int amount)
    {
        UpdatePirateCoin(pirateCoin + amount);
    }

    public bool SpendPirateCoin(int amount)
    {
        if (pirateCoin < amount)
            return false;

        UpdatePirateCoin(pirateCoin - amount);
        return true;
    }
    // dice
    public void AddDice(Dice dice)
    {
        if (dice == null)
            return;

        hasDice.Add(dice);
        RefreshRuntimeDebugInfo();
    }

    public void RemoveDice(Dice dice)
    {
        hasDice.Remove(dice);
        RefreshRuntimeDebugInfo();
    }
    // card
    public void AddOwnedCard(CardData card)
    {
        if (card == null)
            return;

        hasCard.Add(card);
        RefreshRuntimeDebugInfo();
    }

    public void RemoveOwnedCard(CardData card)
    {
        hasCard.Remove(card);
        RefreshRuntimeDebugInfo();
    }

    public void AddTurnCard(CardData card)
    {
        if (card == null)
            return;

        currentTurnCards.Add(card);
        RefreshRuntimeDebugInfo();
    }

    public void RemoveTurnCard(CardData card)
    {
        currentTurnCards.Remove(card);
        RefreshRuntimeDebugInfo();
    }
    
    // reroll
    public bool UseReroll()
    {
        if (reroll <= 0)
            return false;

        UpdateReroll(reroll - 1);
        return true;
    }

    public void AddReroll(int amount)
    {
        UpdateReroll(reroll + amount);
    }

    public void SetReroll(int amount)
    {
        UpdateReroll(Mathf.Max(0, amount));
    }
    // player HP
    public void SetPlayerHP(int hp)
    {
        UpdatePlayerHP(hp);
    }

    public void ResetRuntimeDataToDefaults()
    {
        CacheStartupDefaultsIfNeeded();
        InitDefaultData();
    }

    private void CacheStartupDefaultsIfNeeded()
    {
        if (RuntimeCache.HasStartupDefaults)
        {
            return;
        }

        RuntimeCache.HasStartupDefaults = true;
        RuntimeCache.InitialCoin = initialCoin;
        RuntimeCache.InitialPirateCoin = initialPirateCoin;
        RuntimeCache.InitialReroll = initialReroll;
        RuntimeCache.InitialPlayerHP = initialPlayerHP;
        RuntimeCache.InitialPlayerDamage = initialPlayerDamage;
        RuntimeCache.InitialStageName = initialStageName;
        RuntimeCache.InitialCards = initialCards != null ? new List<CardData>(initialCards) : new List<CardData>();
        RuntimeCache.InitialDice = initialDice != null ? new List<Dice>(initialDice) : new List<Dice>();
    }

    public void PrepareForFieldSceneRetry()
    {
        savedFieldState = null;
        latestFieldCheckpoint = null;
        pendingShopExitTransition = null;
        currentTurnCards.Clear();
        playerHP = initialPlayerHP;
        RefreshRuntimeDebugInfo();
    }

    public void TakeDamage(int damage)
    {
        UpdatePlayerHP(Mathf.Max(0, playerHP - damage));
    }

    public void Heal(int amount)
    {
        UpdatePlayerHP(playerHP + amount);
    }
    // player Damage
    public void AddPlayerDamage(int amount)
    {
        UpdatePlayerDamage(playerDamage + amount);
    }

    public void ReducePlayerDamage(int amount)
    {
        UpdatePlayerDamage(Mathf.Max(0, playerDamage - amount));
    }

    // 턴 종료 시 소지한 카드 모두 제거
    public void ClearTurnCards()
    {
        currentTurnCards.Clear();
        RefreshRuntimeDebugInfo();
    }

    public void SetStageName(string stageName)
    {
        if (string.IsNullOrWhiteSpace(stageName))
        {
            return;
        }

        this.stageName = stageName;
        RefreshRuntimeDebugInfo();
    }

    public bool ShouldRestoreFieldStateForActiveScene()
    {
        if (IsTitleScene(SceneManager.GetActiveScene().name))
        {
            return false;
        }

        return HasSavedFieldState
            && SceneManager.GetActiveScene().name == savedFieldState.sceneName;
    }

    public void EnterShopFromField(
        PlayerController player,
        DiceManager diceManager,
        StageManager stageManager,
        TurnPhase returnPhase = TurnPhase.End,
        string shopSceneName = DefaultShopSceneName,
        PendingSceneTransition shopExitTransition = null)
    {
        if (player == null || diceManager == null || stageManager == null)
        {
            Debug.LogError("상점 진입에 필요한 필드 상태 참조가 부족합니다.");
            return;
        }

        pendingShopExitTransition = ClonePendingSceneTransition(shopExitTransition);
        SaveFieldSceneState(player, diceManager, stageManager, returnPhase);
        FadeToScene(shopSceneName);
    }

    public void CaptureFieldCheckpoint(PlayerController player, DiceManager diceManager, StageManager stageManager, TurnPhase phase)
    {
        if (!CanCaptureFieldState(player, diceManager, stageManager))
        {
            return;
        }

        latestFieldCheckpoint = CreateFieldSceneState(player, diceManager, stageManager, phase);
        RefreshRuntimeDebugInfo();
    }

    public void ReturnToSavedFieldScene()
    {
        if (TryReturnViaPendingSceneTransition())
        {
            return;
        }

        if (!EnsureSavedFieldStateForReturn())
        {
            Debug.LogWarning("복귀할 필드 저장 상태가 없습니다.");
            return;
        }

        FadeToScene(savedFieldState.sceneName);
    }

    private void SaveFieldSceneState(PlayerController player, DiceManager diceManager, StageManager stageManager, TurnPhase returnPhase)
    {
        savedFieldState = CreateFieldSceneState(player, diceManager, stageManager, returnPhase);
        latestFieldCheckpoint = savedFieldState;

        SetStageName(savedFieldState.stageName);
        SetReroll(savedFieldState.remainingRerolls);
        Debug.Log($"필드 상태 저장 완료 -> {savedFieldState.sceneName}, 복귀 페이즈 {returnPhase}");
        RefreshRuntimeDebugInfo();
    }

    private FieldSceneState CreateFieldSceneState(PlayerController player, DiceManager diceManager, StageManager stageManager, TurnPhase returnPhase)
    {
        string activeSceneName = SceneManager.GetActiveScene().name;
        return new FieldSceneState
        {
            sceneName = activeSceneName,
            stageName = activeSceneName,
            playerPosition = player.GetRestorePosition(),
            stageDistance = stageManager.CurrentDistance,
            reachedGoalTrigger = stageManager.HasReachedGoalTrigger,
            remainingMoves = diceManager.RemainingMoves,
            remainingRerolls = diceManager.RemainingRerolls,
            currentDiceValue = diceManager.CurrentDiceValue,
            playerInvincibleMoveCount = player.invincibleMoveCount,
            playerIgnoreHitCount = player.ignoreHitCount,
            bossCurrentHp = stageManager.IsBossField && stageManager.boss != null ? stageManager.boss.CurrentHP : 0,
            returnPhase = returnPhase,
            playerCardRuntimeState = player.CaptureCardRuntimeState()
        };
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        activeSceneNameDebug = scene.name;
        RefreshRuntimeDebugInfo();

        if (IsShopScene(scene.name))
        {
            return;
        }

        if (IsTitleScene(scene.name))
        {
            return;
        }

        if (!HasSavedFieldState || scene.name != savedFieldState.sceneName)
        {
            StartCoroutine(CaptureFieldCheckpointNextFrame(scene.name));
            return;
        }

        StartCoroutine(RestoreFieldSceneStateNextFrame());
    }

    private IEnumerator RestoreFieldSceneStateNextFrame()
    {
        yield return null;

        if (savedFieldState == null)
        {
            yield break;
        }

        if (!TryFindFieldRuntime(out StageManager stageManager, out PlayerController player, out DiceManager diceManager, out TurnManager turnManager, true))
        {
            Debug.LogError("필드 상태 복원에 필요한 오브젝트를 찾지 못했습니다.");
            yield break;
        }

        FieldSceneState stateToRestore = savedFieldState;
        savedFieldState = null;

        SetStageName(stateToRestore.stageName);
        SetReroll(stateToRestore.remainingRerolls);

        stageManager.RestoreSavedFieldState(stateToRestore);
        player.RestoreSavedFieldState(stateToRestore);
        diceManager.RestoreSavedFieldState(stateToRestore);
        turnManager.RestoreSavedPhase(stateToRestore.returnPhase);
        diceManager.AlignBoardToPlayerPosition(stateToRestore.playerPosition);

        Debug.Log($"필드 상태 복원 완료 -> {stateToRestore.sceneName}");
        RefreshRuntimeDebugInfo();
    }

    private IEnumerator CaptureFieldCheckpointNextFrame(string sceneName)
    {
        yield return null;

        if (IsShopScene(sceneName))
        {
            yield break;
        }

        if (IsTitleScene(sceneName))
        {
            yield break;
        }

        if (!TryFindFieldRuntime(out StageManager stageManager, out PlayerController player, out DiceManager diceManager, out TurnManager turnManager, false))
        {
            Debug.LogWarning($"필드 체크포인트 자동 저장 실패 -> {sceneName}");
            yield break;
        }

        TurnPhase phase = turnManager != null ? turnManager.CurrentPhase : TurnPhase.Standby;
        CaptureFieldCheckpoint(player, diceManager, stageManager, phase);
        Debug.Log($"필드 체크포인트 자동 저장 -> {sceneName}");
        RefreshRuntimeDebugInfo();
    }

    private static bool IsShopScene(string sceneName)
    {
        return sceneName.IndexOf("Shop", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsTitleScene(string sceneName)
    {
        return sceneName.Equals("TitleScene", System.StringComparison.OrdinalIgnoreCase);
    }

    private bool TryReturnViaPendingSceneTransition()
    {
        if (pendingShopExitTransition == null || string.IsNullOrWhiteSpace(pendingShopExitTransition.targetSceneName))
        {
            return false;
        }

        PendingSceneTransition transition = ClonePendingSceneTransition(pendingShopExitTransition);
        pendingShopExitTransition = null;
        savedFieldState = null;
        latestFieldCheckpoint = null;
        RefreshRuntimeDebugInfo();

        if (transition.transitionImages != null && transition.transitionImages.Count > 0)
        {
            SceneTransitionFader.Instance.FadeThroughImagesToScene(
                transition.targetSceneName,
                transition.transitionImages,
                transition.fadeDuration,
                transition.fadeHoldDuration,
                transition.imageDisplayDuration,
                transition.imageFadeDuration,
                transition.imageGapDuration);
        }
        else
        {
            SceneTransitionFader.Instance.FadeToScene(
                transition.targetSceneName,
                transition.fadeDuration,
                transition.fadeHoldDuration);
        }

        return true;
    }

    private void FadeToScene(string sceneName)
    {
        SceneTransitionFader.Instance.FadeToScene(sceneName, shopFadeDuration, shopFadeHoldDuration);
    }

    private bool EnsureSavedFieldStateForReturn()
    {
        if (HasSavedFieldState)
        {
            return true;
        }

        if (latestFieldCheckpoint == null)
        {
            return false;
        }

        savedFieldState = latestFieldCheckpoint;
        Debug.LogWarning($"복귀용 저장 상태가 없어 마지막 체크포인트로 대체합니다. -> {savedFieldState.sceneName}");
        RefreshRuntimeDebugInfo();
        return true;
    }

    private bool CanCaptureFieldState(PlayerController player, DiceManager diceManager, StageManager stageManager)
    {
        if (player == null || diceManager == null || stageManager == null)
        {
            return false;
        }

        string activeSceneName = SceneManager.GetActiveScene().name;
        return !IsShopScene(activeSceneName) && !IsTitleScene(activeSceneName);
    }

    private static bool TryFindFieldRuntime(
        out StageManager stageManager,
        out PlayerController player,
        out DiceManager diceManager,
        out TurnManager turnManager,
        bool requireTurnManager)
    {
        stageManager = FindFirstObjectByType<StageManager>();
        player = FindFirstObjectByType<PlayerController>();
        diceManager = FindFirstObjectByType<DiceManager>();
        turnManager = FindFirstObjectByType<TurnManager>();

        return stageManager != null
            && player != null
            && diceManager != null
            && (!requireTurnManager || turnManager != null);
    }

    private void RefreshRuntimeDebugInfo()
    {
        hasSavedFieldStateDebug = savedFieldState != null;
        activeSceneNameDebug = SceneManager.GetActiveScene().name;
        instanceIdDebug = GetInstanceID();
        owningSceneDebug = gameObject.scene.name;
        isPrimaryInstanceDebug = Instance == this;
        savedFieldStateSummaryDebug = BuildFieldStateSummary(savedFieldState);
        latestFieldCheckpointSummaryDebug = BuildFieldStateSummary(latestFieldCheckpoint);

        if (Instance == this)
        {
            SaveToRuntimeCache();
        }
    }

    private void SaveToRuntimeCache()
    {
        RuntimeCache.HasCache = true;
        RuntimeCache.Coin = coin;
        RuntimeCache.PirateCoin = pirateCoin;
        RuntimeCache.Reroll = reroll;
        RuntimeCache.PlayerHP = playerHP;
        RuntimeCache.PlayerDamage = playerDamage;
        RuntimeCache.StageName = stageName;
        RuntimeCache.HasDice = new List<Dice>(hasDice);
        RuntimeCache.OwnedCards = new List<CardData>(hasCard);
        RuntimeCache.TurnCards = new List<CardData>(currentTurnCards);
        RuntimeCache.SavedFieldState = CloneFieldSceneState(savedFieldState);
        RuntimeCache.LatestFieldCheckpoint = CloneFieldSceneState(latestFieldCheckpoint);
        RuntimeCache.PendingShopExitTransition = ClonePendingSceneTransition(pendingShopExitTransition);
    }

    private void RestoreFromRuntimeCache()
    {
        coin = RuntimeCache.Coin;
        pirateCoin = RuntimeCache.PirateCoin;
        reroll = RuntimeCache.Reroll;
        playerHP = RuntimeCache.PlayerHP;
        playerDamage = RuntimeCache.PlayerDamage;
        stageName = RuntimeCache.StageName;
        hasDice = new List<Dice>(RuntimeCache.HasDice);
        hasCard = new List<CardData>(RuntimeCache.OwnedCards);
        currentTurnCards = new List<CardData>(RuntimeCache.TurnCards);
        savedFieldState = CloneFieldSceneState(RuntimeCache.SavedFieldState);
        latestFieldCheckpoint = CloneFieldSceneState(RuntimeCache.LatestFieldCheckpoint);
        pendingShopExitTransition = ClonePendingSceneTransition(RuntimeCache.PendingShopExitTransition);
    }

    private void UpdateCoin(int value)
    {
        coin = value;
        RefreshRuntimeDebugInfo();
    }

    private void UpdatePirateCoin(int value)
    {
        pirateCoin = value;
        RefreshRuntimeDebugInfo();
    }

    private void UpdateReroll(int value)
    {
        reroll = value;
        RefreshRuntimeDebugInfo();
    }

    private void UpdatePlayerHP(int value)
    {
        playerHP = value;
        RefreshRuntimeDebugInfo();
    }

    private void UpdatePlayerDamage(int value)
    {
        playerDamage = value;
        RefreshRuntimeDebugInfo();
    }

    private static FieldSceneState CloneFieldSceneState(FieldSceneState source)
    {
        if (source == null)
        {
            return null;
        }

        return new FieldSceneState
        {
            sceneName = source.sceneName,
            stageName = source.stageName,
            playerPosition = source.playerPosition,
            stageDistance = source.stageDistance,
            reachedGoalTrigger = source.reachedGoalTrigger,
            remainingMoves = source.remainingMoves,
            remainingRerolls = source.remainingRerolls,
            currentDiceValue = source.currentDiceValue,
            playerInvincibleMoveCount = source.playerInvincibleMoveCount,
            playerIgnoreHitCount = source.playerIgnoreHitCount,
            bossCurrentHp = source.bossCurrentHp,
            returnPhase = source.returnPhase,
            playerCardRuntimeState = ClonePlayerCardRuntimeState(source.playerCardRuntimeState)
        };
    }

    private static PendingSceneTransition ClonePendingSceneTransition(PendingSceneTransition source)
    {
        if (source == null)
        {
            return null;
        }

        return new PendingSceneTransition
        {
            targetSceneName = source.targetSceneName,
            transitionImages = source.transitionImages != null
                ? new List<Sprite>(source.transitionImages)
                : new List<Sprite>(),
            fadeDuration = source.fadeDuration,
            fadeHoldDuration = source.fadeHoldDuration,
            imageDisplayDuration = source.imageDisplayDuration,
            imageFadeDuration = source.imageFadeDuration,
            imageGapDuration = source.imageGapDuration
        };
    }

    private static PlayerCardRuntimeState ClonePlayerCardRuntimeState(PlayerCardRuntimeState source)
    {
        if (source == null)
        {
            return null;
        }

        return new PlayerCardRuntimeState
        {
            gamblerActive = source.gamblerActive,
            gamblerSwing = source.gamblerSwing,
            nextGamblerThreshold = source.nextGamblerThreshold,
            dashAfterEvadeActive = source.dashAfterEvadeActive,
            dashAfterEvadeDistance = source.dashAfterEvadeDistance,
            hedonismBonusDistance = source.hedonismBonusDistance,
            parryGodBonusDistance = source.parryGodBonusDistance,
            destroyerBonusDistance = source.destroyerBonusDistance,
            jumpCrazyBonusDistance = source.jumpCrazyBonusDistance,
            parryReflectActive = source.parryReflectActive,
            parryReflectDamage = source.parryReflectDamage,
            remainingAttackParryCount = source.remainingAttackParryCount,
            attackParryForTurn = source.attackParryForTurn,
            remainingAttackDestroyCount = source.remainingAttackDestroyCount,
            attackDestroyForTurn = source.attackDestroyForTurn,
            remainingBlockParryCount = source.remainingBlockParryCount,
            blockParryForTurn = source.blockParryForTurn,
            remainingIgnoredMoveTileCount = source.remainingIgnoredMoveTileCount,
            ignoreMoveTileEffectsForTurn = source.ignoreMoveTileEffectsForTurn,
            turnMoveSpeedMultiplier = source.turnMoveSpeedMultiplier
        };
    }

    private static string BuildFieldStateSummary(FieldSceneState state)
    {
        if (state == null)
        {
            return "None";
        }

        return $"{state.sceneName} | pos:{state.playerPosition} | dist:{state.stageDistance} | goal:{state.reachedGoalTrigger} | moves:{state.remainingMoves} | rerolls:{state.remainingRerolls} | dice:{state.currentDiceValue} | phase:{state.returnPhase}";
    }
}
