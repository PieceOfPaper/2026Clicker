using UnityEngine;
using UnityEngine.UI;

public class UITargetHP : MonoBehaviour
{
    [SerializeField] private Slider _sliderHP;
    [SerializeField] private TMPro.TMP_Text _textHp;
    [SerializeField] private Slider _sliderTime;
    [SerializeField] private TMPro.TMP_Text _textTime;
    

    private void Start()
    {
        if (GameMain.Instance != null)
            GameMain.Instance.OnMonsterHpChangedCallback += Refresh;

        Refresh();
        RefreshBossTime();
    }

    private void Update()
    {
        RefreshBossTime();
    }

    private void OnDestroy()
    {
        if (GameMain.Instance != null)
            GameMain.Instance.OnMonsterHpChangedCallback -= Refresh;
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
}
