using System.Collections;
using UnityEngine;
using GoogleSheetsTable;

public class GameMain : MonoBehaviour
{
    [SerializeField] private Transform _stagePivot;
    [SerializeField] private Transform _characterPivot;
    [SerializeField] private Transform _monsterPivot;

    [Header("Fallback Resources")]
    [SerializeField] private string _fallbackStageName = "DummyStage";
    [SerializeField] private string _dummyCharcterName = "DummyChar";
    [SerializeField] private string _fallbackMonsterName = "DummyMonster";

    [Header("Battle Settings")]
    [SerializeField] private BigNumber _tapDamage = 10;

    private static GameMain s_instance;
    public static GameMain Instance => s_instance;
    
    private const string RESOURCES_PATH_STAGE = "Stage";
    private const string RESOURCES_PATH_CHARACTER = "Character";
    private const string RESOURCES_PATH_MONSTER = "Monster";
    

    public int CurrentStage { get; private set; } = 1;
    public int NormalMonstersDefeated { get; private set; }
    public BigNumber Currency { get; private set; }
    public BigNumber TapDamage => _tapDamage;
    public float BossTimeRemaining { get; private set; }
    public bool IsBossBattle { get; private set; }
    public bool IsBossRetryAvailable { get; private set; }
    public BigNumber CurrentMonsterMaxHp { get; private set; }
    public BigNumber CurrentMonsterHp { get; private set; }
    public BigNumber CurrentMonsterCurrencyReward { get; private set; }
    public bool IsCurrentMonsterDead => CurrentMonsterHp <= 0f;

    private GameStage _stage;
    public GameStage Stage => _stage;

    private GameCharacter _character;
    public GameCharacter Character => _character;

    private GameMonster _monster;
    public GameMonster Monster => _monster;

    private GameTableDatabase _tableDatabase;
    private Stage _currentStageData;
    private bool _isMonsterDefeatSequenceActive;

    public System.Action OnStartCallback;
    public System.Action<BigNumber> OnAttackCallback;
    public System.Action OnMonsterHpChangedCallback;

    
    private void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_instance = this;
    }

    private void Start()
    {
        if (!LoadTablesAndStage())
            return;

        SetupResources();
        SpawnNormalMonster();
        
        OnStartCallback?.Invoke();
    }

    private void Update()
    {
        if (!IsBossBattle || _monster == null || IsCurrentMonsterDead)
            return;

        BossTimeRemaining = Mathf.Max(0f, BossTimeRemaining - Time.deltaTime);
        if (BossTimeRemaining <= 0f)
            FailBossBattle();
    }

    
    public void Touch()
    {
        if (_character == null || _monster == null || IsCurrentMonsterDead || _isMonsterDefeatSequenceActive)
            return;

        _character.Attack();
        CurrentMonsterHp = BigNumber.Max(BigNumber.Zero, CurrentMonsterHp - _tapDamage);
        _monster.Hit();
        OnMonsterHpChangedCallback?.Invoke();
        OnAttackCallback?.Invoke(_tapDamage);

        if (IsCurrentMonsterDead)
            DefeatCurrentMonster();
    }

    public void StartBossBattle()
    {
        if (_isMonsterDefeatSequenceActive || IsBossBattle ||
            (!IsBossRetryAvailable && NormalMonstersDefeated < _currentStageData.MonsterIds.Length))
            return;

        IsBossRetryAvailable = false;
        SpawnMonster(true);
    }

    private void OnDestroy()
    {
        if (s_instance == this)
            s_instance = null;

        _tableDatabase?.Dispose();
    }

    private bool LoadTablesAndStage()
    {
        _tableDatabase = new GameTableDatabase();
        if (!_tableDatabase.Load())
            return false;

        return TrySetStage(CurrentStage);
    }

    private void SetupResources()
    {
        SpawnStage();
        _character = InstantiateResource<GameCharacter>($"{RESOURCES_PATH_CHARACTER}/{_dummyCharcterName}", _characterPivot);
    }

    private void SpawnStage()
    {
        if (_stage != null)
            Destroy(_stage.gameObject);

        var prefabName = string.IsNullOrWhiteSpace(_currentStageData.PrefabName)
            ? _fallbackStageName
            : _currentStageData.PrefabName;
        _stage = InstantiateResource<GameStage>($"{RESOURCES_PATH_STAGE}/{prefabName}", _stagePivot);
    }

    private void SpawnNormalMonster()
    {
        IsBossBattle = false;
        BossTimeRemaining = 0f;
        SpawnMonster(false);
    }

    private void SpawnMonster(bool isBoss)
    {
        if (_monster != null)
            Destroy(_monster.gameObject);

        var monsterId = isBoss
            ? _currentStageData.BossMonsterId
            : _currentStageData.MonsterIds[Mathf.Clamp(NormalMonstersDefeated, 0, _currentStageData.MonsterIds.Length - 1)];
        var monsterData = _tableDatabase.Tables.GetMonsterByID(monsterId);
        if (!monsterData.IsValid)
        {
            Debug.LogError($"Monster table row not found: ID {monsterId}", this);
            return;
        }

        var prefabName = string.IsNullOrWhiteSpace(monsterData.PrefabName)
            ? _fallbackMonsterName
            : monsterData.PrefabName;
        _monster = InstantiateResource<GameMonster>($"{RESOURCES_PATH_MONSTER}/{prefabName}", _monsterPivot);
        if (_monster == null)
        {
            CurrentMonsterMaxHp = BigNumber.Zero;
            CurrentMonsterHp = BigNumber.Zero;
            CurrentMonsterCurrencyReward = BigNumber.Zero;
            return;
        }

        var baseHp = ParseTableNumber(monsterData.BaseHp, $"Monster {monsterId} BaseHp");
        var baseReward = ParseTableNumber(monsterData.BaseReward, $"Monster {monsterId} BaseReward");
        var hpMultiplier = ParseTableNumber(_currentStageData.HpMultiplier, $"Stage {CurrentStage} HpMultiplier");
        var rewardMultiplier = ParseTableNumber(_currentStageData.RewardMultiplier, $"Stage {CurrentStage} RewardMultiplier");

        CurrentMonsterMaxHp = baseHp * hpMultiplier;
        CurrentMonsterHp = CurrentMonsterMaxHp;
        CurrentMonsterCurrencyReward = baseReward * rewardMultiplier;

        IsBossBattle = isBoss;
        BossTimeRemaining = isBoss ? _currentStageData.BossTimeLimitSeconds : 0f;
        _isMonsterDefeatSequenceActive = false;
        _monster.Appear();
        OnMonsterHpChangedCallback?.Invoke();
    }

    private void DefeatCurrentMonster()
    {
        if (_isMonsterDefeatSequenceActive)
            return;

        _isMonsterDefeatSequenceActive = true;
        Currency += CurrentMonsterCurrencyReward;
        _monster.Defeat();
        StartCoroutine(SpawnAfterDefeat(IsBossBattle));
    }

    private IEnumerator SpawnAfterDefeat(bool defeatedBoss)
    {
        yield return new WaitForSeconds(1f);

        if (defeatedBoss)
        {
            var nextStageId = _currentStageData.NextStageId;
            NormalMonstersDefeated = 0;
            IsBossBattle = false;
            BossTimeRemaining = 0f;

            if (nextStageId > 0 && TrySetStage(nextStageId))
                SpawnStage();

            SpawnNormalMonster();
            yield break;
        }

        NormalMonstersDefeated++;
        if (NormalMonstersDefeated >= _currentStageData.MonsterIds.Length && !IsBossRetryAvailable)
        {
            IsBossRetryAvailable = false;
            SpawnMonster(true);
            yield break;
        }

        if (NormalMonstersDefeated >= _currentStageData.MonsterIds.Length)
            NormalMonstersDefeated = 0;

        SpawnNormalMonster();
    }

    private bool TrySetStage(int stageId)
    {
        var stageData = _tableDatabase.Tables.GetStageByID(stageId);
        if (!stageData.IsValid)
        {
            Debug.LogError($"Stage table row not found: ID {stageId}", this);
            return false;
        }

        if (stageData.MonsterIds == null || stageData.MonsterIds.Length == 0)
        {
            Debug.LogError($"Stage {stageId} has no normal monsters.", this);
            return false;
        }

        CurrentStage = stageId;
        _currentStageData = stageData;
        return true;
    }

    private BigNumber ParseTableNumber(string value, string fieldName)
    {
        if (BigNumber.TryParse(value, out var result))
            return result;

        Debug.LogError($"Invalid BigNumber in {fieldName}: '{value}'", this);
        return BigNumber.Zero;
    }

    private void FailBossBattle()
    {
        IsBossBattle = false;
        IsBossRetryAvailable = true;
        NormalMonstersDefeated = 0;
        SpawnNormalMonster();
    }

    private T InstantiateResource<T>(string resourcePath, Transform pivot) where T : Component
    {
        if (pivot == null)
        {
            Debug.LogError($"{typeof(T).Name} pivot is missing.", this);
            return null;
        }

        var prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
        {
            Debug.LogError($"Resource prefab not found: Resources/{resourcePath}", this);
            return null;
        }

        var instance = Instantiate(prefab, pivot);
        instance.name = prefab.name;
        instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        var component = instance.GetComponent<T>();
        if (component == null)
            Debug.LogError($"{prefab.name} does not contain {typeof(T).Name}.", instance);

        return component;
    }

    
    private void OnDrawGizmos()
    {
        DrawPivotGizmo(_stagePivot, Color.cyan, 0.3f);
        DrawPivotGizmo(_characterPivot, Color.green, 0.25f);
        DrawPivotGizmo(_monsterPivot, Color.red, 0.25f);
    }

    private static void DrawPivotGizmo(Transform pivot, Color color, float radius)
    {
        if (pivot == null)
            return;

        Gizmos.color = color;
        Gizmos.DrawWireSphere(pivot.position, radius);
        Gizmos.DrawLine(pivot.position - Vector3.right * radius, pivot.position + Vector3.right * radius);
        Gizmos.DrawLine(pivot.position - Vector3.up * radius, pivot.position + Vector3.up * radius);
    }
}
