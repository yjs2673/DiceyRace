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
    public bool nextAttackHasParry;
    public bool nextBlockIsParry;
}

[System.Serializable]
public class FieldSceneState
{
    public string sceneName;
    public string stageName;
    public Vector3 playerPosition;
    public int stageDistance;
    public int remainingMoves;
    public int remainingRerolls;
    public int currentDiceValue;
    public int playerInvincibleMoveCount;
    public int playerIgnoreHitCount;
    public int bossCurrentHp;
    public TurnPhase returnPhase;
    public PlayerCardRuntimeState playerCardRuntimeState;
}

[DefaultExecutionOrder(-1000)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private const string DefaultShopSceneName = "ShopScene";
    private static bool hasRuntimeCache;
    private static int cachedCoin;
    private static int cachedPirateCoin;
    private static int cachedReroll;
    private static int cachedPlayerHP;
    private static int cachedPlayerDamage;
    private static string cachedStageName;
    private static List<Dice> cachedHasDice = new List<Dice>();
    private static List<CardData> cachedHasCard = new List<CardData>();
    private static FieldSceneState cachedSavedFieldState;
    private static FieldSceneState cachedLatestFieldCheckpoint;

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
    
    [Header("Runtime Debug")]
    [SerializeField] private int coin;
    [SerializeField] private int pirateCoin;
    [SerializeField] private int reroll;
    [SerializeField] private int playerHP;
    [SerializeField] private int playerDamage;
    [SerializeField] private string stageName;
    [SerializeField] private List<Dice> hasDice = new List<Dice>();
    [SerializeField] private List<CardData> hasCard = new List<CardData>();
    [SerializeField] private FieldSceneState savedFieldState;
    [SerializeField] private FieldSceneState latestFieldCheckpoint;
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

        if (hasRuntimeCache)
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
        coin = initialCoin;
        pirateCoin = initialPirateCoin;
        reroll = initialReroll;
        playerHP = initialPlayerHP;
        playerDamage = initialPlayerDamage;
        stageName = initialStageName;
        hasCard.Clear();
        hasDice.Clear();
        savedFieldState = null;
        latestFieldCheckpoint = null;

        if (initialCards != null)
            hasCard.AddRange(initialCards);

        if (initialDice != null)
            hasDice.AddRange(initialDice);

        RefreshRuntimeDebugInfo();
    }
    // coin
    public void AddCoin(int amount)
    {
        coin += amount;
        RefreshRuntimeDebugInfo();
    }

    public bool SpendCoin(int amount)
    {
        if (coin < amount)
            return false;

        coin -= amount;
        RefreshRuntimeDebugInfo();
        return true;
    }
    // pirate coin
    public void AddPirateCoin(int amount)
    {
        pirateCoin += amount;
        RefreshRuntimeDebugInfo();
    }

    public bool SpendPirateCoin(int amount)
    {
        if (pirateCoin < amount)
            return false;

        pirateCoin -= amount;
        RefreshRuntimeDebugInfo();
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
    public void AddCard(CardData card)
    {
        if (card == null)
            return;

        hasCard.Add(card);
        RefreshRuntimeDebugInfo();
    }

    public void RemoveCard(CardData card)
    {
        hasCard.Remove(card);
        RefreshRuntimeDebugInfo();
    }
    // reroll
    public bool UseReroll()
    {
        if (reroll <= 0)
            return false;

        reroll--;
        RefreshRuntimeDebugInfo();
        return true;
    }

    public void AddReroll(int amount)
    {
        reroll += amount;
        RefreshRuntimeDebugInfo();
    }

    public void SetReroll(int amount)
    {
        reroll = Mathf.Max(0, amount);
        RefreshRuntimeDebugInfo();
    }
    // player HP
    public void SetPlayerHP(int hp)
    {
        playerHP = hp;
        RefreshRuntimeDebugInfo();
    }

    public void TakeDamage(int damage)
    {
        playerHP -= damage;

        if (playerHP < 0)
            playerHP = 0;

        RefreshRuntimeDebugInfo();
    }

    public void Heal(int amount)
    {
        playerHP += amount;
        RefreshRuntimeDebugInfo();
    }
    // player Damage
    public void AddPlayerDamage(int amount)
    {
        playerDamage += amount;
        RefreshRuntimeDebugInfo();
    }

    public void ReducePlayerDamage(int amount)
    {
        playerDamage -= amount;

        if (playerDamage < 0)
            playerDamage = 0;

        RefreshRuntimeDebugInfo();
    }

    // 턴 종료 시 소지한 카드 모두 제거
    public void ClearCards()
    {
        hasCard.Clear();
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
        return HasSavedFieldState
            && SceneManager.GetActiveScene().name == savedFieldState.sceneName;
    }

    public void EnterShopFromField(PlayerController player, DiceManager diceManager, StageManager stageManager, TurnPhase returnPhase = TurnPhase.End, string shopSceneName = DefaultShopSceneName)
    {
        if (player == null || diceManager == null || stageManager == null)
        {
            Debug.LogError("상점 진입에 필요한 필드 상태 참조가 부족합니다.");
            return;
        }

        SaveFieldSceneState(player, diceManager, stageManager, returnPhase);
        SceneManager.LoadScene(shopSceneName);
    }

    public void CaptureFieldCheckpoint(PlayerController player, DiceManager diceManager, StageManager stageManager, TurnPhase phase)
    {
        if (player == null || diceManager == null || stageManager == null)
        {
            return;
        }

        string activeSceneName = SceneManager.GetActiveScene().name;
        if (activeSceneName.IndexOf("Shop", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return;
        }

        latestFieldCheckpoint = CreateFieldSceneState(player, diceManager, stageManager, phase);
        RefreshRuntimeDebugInfo();
    }

    public void ReturnToSavedFieldScene()
    {
        if (!HasSavedFieldState)
        {
            if (latestFieldCheckpoint != null)
            {
                savedFieldState = latestFieldCheckpoint;
                Debug.LogWarning($"복귀용 저장 상태가 없어 마지막 체크포인트로 대체합니다. -> {savedFieldState.sceneName}");
                RefreshRuntimeDebugInfo();
            }
            else
            {
                Debug.LogWarning("복귀할 필드 저장 상태가 없습니다.");
                return;
            }
        }

        SceneManager.LoadScene(savedFieldState.sceneName);
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

        StageManager stageManager = FindFirstObjectByType<StageManager>();
        PlayerController player = FindFirstObjectByType<PlayerController>();
        DiceManager diceManager = FindFirstObjectByType<DiceManager>();
        TurnManager turnManager = FindFirstObjectByType<TurnManager>();

        if (stageManager == null || player == null || diceManager == null || turnManager == null)
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

        StageManager stageManager = FindFirstObjectByType<StageManager>();
        PlayerController player = FindFirstObjectByType<PlayerController>();
        DiceManager diceManager = FindFirstObjectByType<DiceManager>();
        TurnManager turnManager = FindFirstObjectByType<TurnManager>();

        if (stageManager == null || player == null || diceManager == null)
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
        hasRuntimeCache = true;
        cachedCoin = coin;
        cachedPirateCoin = pirateCoin;
        cachedReroll = reroll;
        cachedPlayerHP = playerHP;
        cachedPlayerDamage = playerDamage;
        cachedStageName = stageName;
        cachedHasDice = new List<Dice>(hasDice);
        cachedHasCard = new List<CardData>(hasCard);
        cachedSavedFieldState = CloneFieldSceneState(savedFieldState);
        cachedLatestFieldCheckpoint = CloneFieldSceneState(latestFieldCheckpoint);
    }

    private void RestoreFromRuntimeCache()
    {
        coin = cachedCoin;
        pirateCoin = cachedPirateCoin;
        reroll = cachedReroll;
        playerHP = cachedPlayerHP;
        playerDamage = cachedPlayerDamage;
        stageName = cachedStageName;
        hasDice = new List<Dice>(cachedHasDice);
        hasCard = new List<CardData>(cachedHasCard);
        savedFieldState = CloneFieldSceneState(cachedSavedFieldState);
        latestFieldCheckpoint = CloneFieldSceneState(cachedLatestFieldCheckpoint);
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
            nextAttackHasParry = source.nextAttackHasParry,
            nextBlockIsParry = source.nextBlockIsParry
        };
    }

    private static string BuildFieldStateSummary(FieldSceneState state)
    {
        if (state == null)
        {
            return "None";
        }

        return $"{state.sceneName} | pos:{state.playerPosition} | dist:{state.stageDistance} | moves:{state.remainingMoves} | rerolls:{state.remainingRerolls} | dice:{state.currentDiceValue} | phase:{state.returnPhase}";
    }
}
