using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIStatUpgradeButton : MonoBehaviour
{
    public enum UpgradeType
    {
        TouchDamage,
        CriticalChance,
        CriticalDamage,
    }

    [SerializeField] private UpgradeType _upgradeType;
    [SerializeField] private Button _button;
    [SerializeField] private TMP_Text _label;

    private GameMain _subscribedGameMain;

    private void Awake()
    {
        if (_button == null)
            _button = GetComponent<Button>();
        if (_label == null)
            _label = GetComponentInChildren<TMP_Text>();
    }

    private void Start()
    {
        if (_button != null)
            _button.onClick.AddListener(Purchase);

        Subscribe();
        Refresh();
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(Purchase);

        Unsubscribe();
    }

    private void Subscribe()
    {
        var gameMain = GameMain.Instance;
        if (gameMain == null || gameMain == _subscribedGameMain)
            return;

        Unsubscribe();
        _subscribedGameMain = gameMain;
        _subscribedGameMain.OnGoldChangedCallback += OnGoldChanged;
        _subscribedGameMain.OnStatsChangedCallback += Refresh;
    }

    private void Unsubscribe()
    {
        if (_subscribedGameMain != null)
        {
            _subscribedGameMain.OnGoldChangedCallback -= OnGoldChanged;
            _subscribedGameMain.OnStatsChangedCallback -= Refresh;
        }

        _subscribedGameMain = null;
    }

    private void OnGoldChanged(BigNumber _)
    {
        Refresh();
    }

    private void Purchase()
    {
        var gameMain = GameMain.Instance;
        if (gameMain == null)
            return;

        switch (_upgradeType)
        {
            case UpgradeType.TouchDamage:
                gameMain.TryUpgradeTouchDamage();
                break;
            case UpgradeType.CriticalChance:
                gameMain.TryUpgradeCriticalChance();
                break;
            case UpgradeType.CriticalDamage:
                gameMain.TryUpgradeCriticalDamage();
                break;
        }

        Refresh();
    }

    private void Refresh()
    {
        var gameMain = GameMain.Instance;
        if (gameMain == null)
        {
            if (_button != null)
                _button.interactable = false;
            return;
        }

        switch (_upgradeType)
        {
            case UpgradeType.TouchDamage:
                SetView(
                    "TOUCH DAMAGE",
                    gameMain.TouchDamageLevel,
                    BigNumberFormatter.Format(gameMain.TapDamage),
                    BigNumberFormatter.Format(gameMain.GetNextTouchDamage()),
                    gameMain.GetTouchDamageUpgradeCost(),
                    gameMain.CanUpgradeTouchDamage(),
                    false);
                break;
            case UpgradeType.CriticalChance:
                SetView(
                    "CRITICAL CHANCE",
                    gameMain.TouchCriticalChanceLevel,
                    FormatPercent(gameMain.TouchCriticalChance),
                    FormatPercent(Mathf.Min(gameMain.TouchCriticalChance + 0.0001f, 0.5f)),
                    gameMain.GetCriticalChanceUpgradeCost(),
                    gameMain.CanUpgradeCriticalChance(),
                    gameMain.TouchCriticalChanceLevel >= 5000);
                break;
            case UpgradeType.CriticalDamage:
                SetView(
                    "CRITICAL DAMAGE",
                    gameMain.TouchCriticalDamageLevel,
                    FormatPercent(gameMain.TouchCriticalDamageMultiplier),
                    FormatPercent(Mathf.Min(gameMain.TouchCriticalDamageMultiplier + 0.0001f, 2.5f)),
                    gameMain.GetCriticalDamageUpgradeCost(),
                    gameMain.CanUpgradeCriticalDamage(),
                    gameMain.TouchCriticalDamageLevel >= 5000);
                break;
        }
    }

    private void SetView(
        string title,
        int level,
        string currentValue,
        string nextValue,
        BigNumber cost,
        bool canPurchase,
        bool isMaxLevel)
    {
        if (_label != null)
        {
            _label.text = isMaxLevel
                ? $"{title}\nLv. {level}   {currentValue}\nMAX LEVEL"
                : $"{title}\nLv. {level}   {currentValue}  >  {nextValue}\nCOST {BigNumberFormatter.Format(cost)} GOLD";
        }

        if (_button != null)
            _button.interactable = canPurchase && !isMaxLevel;
    }

    private static string FormatPercent(float rate)
    {
        return (rate * 100f).ToString("0.00") + "%";
    }
}
