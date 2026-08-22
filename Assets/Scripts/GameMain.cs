using UnityEngine;

public class GameMain : MonoBehaviour
{
    [SerializeField] private Transform _stagePivot;
    [SerializeField] private Transform _characterPivot;
    [SerializeField] private Transform _monsterPivot;

    [Header("Dummy Resources")]
    [SerializeField] private string _dummyStageName = "DummyStage";
    [SerializeField] private string _dummyCharcterName = "DummyChar";
    [SerializeField] private string _dummyMonsterName = "DummyMonster";

    [Header("Test Battle Settings")]
    [SerializeField, Min(1f)] private float _tapDamage = 10f;
    [SerializeField, Min(1f)] private float _normalMonsterBaseHp = 30f;
    [SerializeField, Min(1)] private int _normalMonsterReward = 5;
    [SerializeField, Min(1)] private int _normalMonstersPerStage = 10;
    [SerializeField, Min(1f)] private float _bossHpMultiplier = 10f;
    [SerializeField, Min(1f)] private float _bossTimeLimitSeconds = 30f;

    private static GameMain s_instance;
    public static GameMain Instance => s_instance;
    
    private const string RESOURCES_PATH_STAGE = "Stage";
    private const string RESOURCES_PATH_CHARACTER = "Character";
    private const string RESOURCES_PATH_MONSTER = "Monster";
    

    public int CurrentStage { get; private set; } = 1;
    public int NormalMonstersDefeated { get; private set; }
    public int Currency { get; private set; }
    public float TapDamage => _tapDamage;
    public float BossTimeRemaining { get; private set; }
    public bool IsBossBattle { get; private set; }
    public bool IsBossRetryAvailable { get; private set; }
    public GameMonster CurrentMonster => _monster;
    public float CurrentMonsterMaxHp { get; private set; }
    public float CurrentMonsterHp { get; private set; }
    public int CurrentMonsterCurrencyReward { get; private set; }
    public bool IsCurrentMonsterDead => CurrentMonsterHp <= 0f;

    private GameStage _stage;
    public GameStage Stage => _stage;

    private GameCharacter _character;
    public GameCharacter Character => _character;

    private GameMonster _monster;
    public GameMonster Monster => _monster;

    public System.Action OnStartCallback;
    public System.Action<float> OnAttackCallback;

    
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
        SetupDummyResources();
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
        if (_character == null || _monster == null || IsCurrentMonsterDead)
            return;

        _character.Attack();
        CurrentMonsterHp = Mathf.Max(0f, CurrentMonsterHp - _tapDamage);
        _monster.Hit();
        OnAttackCallback?.Invoke(_tapDamage);

        if (IsCurrentMonsterDead)
            DefeatCurrentMonster();
    }

    public void StartBossBattle()
    {
        if (IsBossBattle || (!IsBossRetryAvailable && NormalMonstersDefeated < _normalMonstersPerStage))
            return;

        IsBossRetryAvailable = false;
        SpawnMonster(true);
    }

    private void OnDestroy()
    {
        if (s_instance == this)
            s_instance = null;
    }

    private void SetupDummyResources()
    {
        _stage = InstantiateResource<GameStage>($"{RESOURCES_PATH_STAGE}/{_dummyStageName}", _stagePivot);
        _character = InstantiateResource<GameCharacter>($"{RESOURCES_PATH_CHARACTER}/{_dummyCharcterName}", _characterPivot);
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

        _monster = InstantiateResource<GameMonster>($"{RESOURCES_PATH_MONSTER}/{_dummyMonsterName}", _monsterPivot);
        if (_monster == null)
        {
            CurrentMonsterMaxHp = 0f;
            CurrentMonsterHp = 0f;
            CurrentMonsterCurrencyReward = 0;
            return;
        }

        var stageHp = _normalMonsterBaseHp + (CurrentStage - 1) * _normalMonsterBaseHp * 0.5f;
        CurrentMonsterMaxHp = isBoss ? stageHp * _bossHpMultiplier : stageHp;
        CurrentMonsterHp = CurrentMonsterMaxHp;
        CurrentMonsterCurrencyReward = isBoss
            ? _normalMonsterReward * _normalMonstersPerStage
            : _normalMonsterReward;

        IsBossBattle = isBoss;
        BossTimeRemaining = isBoss ? _bossTimeLimitSeconds : 0f;
    }

    private void DefeatCurrentMonster()
    {
        Currency += CurrentMonsterCurrencyReward;

        if (IsBossBattle)
        {
            CurrentStage++;
            NormalMonstersDefeated = 0;
            IsBossBattle = false;
            BossTimeRemaining = 0f;
            SpawnNormalMonster();
            return;
        }

        NormalMonstersDefeated++;
        if (NormalMonstersDefeated >= _normalMonstersPerStage && !IsBossRetryAvailable)
        {
            StartBossBattle();
            return;
        }

        if (NormalMonstersDefeated >= _normalMonstersPerStage)
            NormalMonstersDefeated = 0;

        SpawnNormalMonster();
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
