using TMPro;
using UnityEngine;

public class UIGoldDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text _goldText;
    [SerializeField] private string _prefix = "GOLD  ";

    private GameMain _subscribedGameMain;

    private void Awake()
    {
        if (_goldText == null)
            _goldText = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        Subscribe();
        Refresh(GameMain.Instance != null ? GameMain.Instance.Currency : BigNumber.Zero);
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        var gameMain = GameMain.Instance;
        if (gameMain == null || gameMain == _subscribedGameMain)
            return;

        Unsubscribe();
        _subscribedGameMain = gameMain;
        _subscribedGameMain.OnGoldChangedCallback += Refresh;
    }

    private void Unsubscribe()
    {
        if (_subscribedGameMain != null)
            _subscribedGameMain.OnGoldChangedCallback -= Refresh;

        _subscribedGameMain = null;
    }

    private void Refresh(BigNumber amount)
    {
        if (_goldText != null)
            _goldText.text = _prefix + BigNumberFormatter.Format(amount);
    }
}
