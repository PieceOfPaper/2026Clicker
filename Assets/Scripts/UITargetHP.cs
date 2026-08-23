using UnityEngine;
using UnityEngine.UI;

public class UITargetHP : MonoBehaviour
{
    [SerializeField] private Slider _sliderHP;
    [SerializeField] private TMPro.TMP_Text _textHp;
    

    private void Start()
    {
        if (GameMain.Instance != null)
            GameMain.Instance.OnMonsterHpChangedCallback += Refresh;

        Refresh();
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
}
