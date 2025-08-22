using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

internal class HoldGauge : MonoBehaviour
{
    // 入力を受け取る対象のAction
    [SerializeField] private InputActionReference _hold;
    
    // ゲージのUI
    [SerializeField] private Image _gaugeImage;

    private InputAction _holdAction;
    
    private void Awake()
    {
        if (_hold == null) return;

        _holdAction = _hold.action;
        
        // 入力を受け取るためには必ず有効化する必要がある
        _holdAction.Enable();
    }

    private void Update()
    {
        if (_holdAction == null) return;
        
        // 長押しの進捗を取得
        var progress = _holdAction.GetTimeoutCompletionPercentage();

        // 進捗をゲージに反映
        _gaugeImage.fillAmount = progress;
    }
}