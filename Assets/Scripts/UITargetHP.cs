using UnityEngine;
using UnityEngine.UI;

public class UITargetHP : MonoBehaviour
{
    [SerializeField] private Slider _sliderHP;
    [SerializeField] private TMPro.TMP_Text _textHp;
    

    private void Start()
    {
        if (GameMain.Instance != null)
            GameMain.Instance.OnStartCallback += OnStart;
        
        if (GameMain.Instance != null)
            GameMain.Instance.OnAttackCallback += OnAttack;
        
        UpdateUI_Hp(default, default);
    }

    private void OnDestroy()
    {
        if (GameMain.Instance != null)
            GameMain.Instance.OnStartCallback -= OnStart;
        
        if (GameMain.Instance != null)
            GameMain.Instance.OnAttackCallback -= OnAttack;
    }

    
    private void OnStart()
    {
        if (GameMain.Instance == null)
            return;

        UpdateUI_Hp(GameMain.Instance.CurrentMonsterHp, GameMain.Instance.CurrentMonsterMaxHp);
    }
    
    private void OnAttack(float damage)
    {
        if (GameMain.Instance == null)
            return;

        UpdateUI_Hp(GameMain.Instance.CurrentMonsterHp, GameMain.Instance.CurrentMonsterMaxHp);
    }

    
    private void UpdateUI_Hp(float hp, float hpMax)
    {
        if (_sliderHP != null) _sliderHP.value = hpMax == 0 ? 0f : hp / hpMax;
        if (_textHp != null) _textHp.text = $"{hp} / {hpMax}";
    }
}
