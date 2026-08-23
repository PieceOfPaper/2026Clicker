using UnityEngine;
using UnityEngine.UI;

public class UITargetHP : MonoBehaviour
{
    [SerializeField] private Slider _sliderHP;
    [SerializeField] private TMPro.TMP_Text _textHp;
    [SerializeField] private Slider _sliderTime;
    [SerializeField] private TMPro.TMP_Text _textTime;
    [SerializeField] private Button _buttonBoss;
    

    private void Start()
    {
        if (GameMain.Instance != null)
            GameMain.Instance.OnMonsterHpChangedCallback += Refresh;

        if (_buttonBoss != null)
            _buttonBoss.onClick.AddListener(OnClickBoss);

        Refresh();
        RefreshBossTime();
        RefreshBossButton();
    }

    private void LateUpdate()
    {
        RefreshBossTime();
        RefreshBossButton();
    }

    private void OnDestroy()
    {
        if (GameMain.Instance != null)
            GameMain.Instance.OnMonsterHpChangedCallback -= Refresh;

        if (_buttonBoss != null)
            _buttonBoss.onClick.RemoveListener(OnClickBoss);
    }

    private void Refresh()
    {
        if (GameMain.Instance == null)
        {
            UpdateUI_Hp(default, default);
            return;
        }

        UpdateUI_Hp(GameMain.Instance.CurrentMonsterHp, GameMain.Instance.CurrentMonsterMaxHp);
    }

    
    private void UpdateUI_Hp(BigNumber hp, BigNumber hpMax)
    {
        if (_sliderHP != null)
            _sliderHP.value = hpMax.IsZero ? 0f : (float)(hp / hpMax).ToDouble();

        if (_textHp != null)
            _textHp.text = $"{BigNumberFormatter.Format(hp)} / {BigNumberFormatter.Format(hpMax)}";
    }

    private void RefreshBossTime()
    {
        var gameMain = GameMain.Instance;
        var showBossTime = gameMain != null &&
                           gameMain.IsBossBattle &&
                           gameMain.Monster != null &&
                           !gameMain.IsCurrentMonsterDead;

        if (_sliderTime != null)
        {
            _sliderTime.gameObject.SetActive(showBossTime);
            if (showBossTime)
            {
                var timeLimit = gameMain.CurrentBossTimeLimit;
                _sliderTime.value = timeLimit <= 0f
                    ? 0f
                    : Mathf.Clamp01(gameMain.BossTimeRemaining / timeLimit);
            }
        }

        if (_textTime != null)
        {
            _textTime.gameObject.SetActive(showBossTime);
            if (showBossTime)
                _textTime.text = gameMain.BossTimeRemaining.ToString("0.0");
        }
    }

    private void RefreshBossButton()
    {
        if (_buttonBoss == null)
            return;

        var gameMain = GameMain.Instance;
        var showBossButton = gameMain != null &&
                             gameMain.IsBossRetryAvailable &&
                             !gameMain.IsBossBattle &&
                             gameMain.Monster != null;
        _buttonBoss.gameObject.SetActive(showBossButton);
    }

    private void OnClickBoss()
    {
        if (GameMain.Instance == null)
            return;

        GameMain.Instance.StartBossBattle();
        RefreshBossButton();
        RefreshBossTime();
    }
}
