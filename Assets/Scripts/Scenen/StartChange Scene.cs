using LitMotion;
using LitMotion.Extensions;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class StartchangeScene : MonoBehaviour
{
    [SerializeField, Label("ボタン")] private Image _buttonImage;
    [SerializeField, Label("上書き色")] private Color _hoverColor;
    [SerializeField, Label("色変更時間")] private float _hoverDuration;
    [SerializeField] private Ease _hoverEase;
    [SerializeField] private Ease _clickEase;

    private readonly CompositeMotionHandle _motionHandles = new(2);

    private void OnDestroy()
    {
        _motionHandles.Cancel();
    }

    public void OnClickStartButton()
    {
        SceneManager.LoadScene("EntryScene");
    }

    private void OnMouseEnter()
    {
        _motionHandles.Cancel();
        LMotion.Create(Color.white, _hoverColor, _hoverDuration)
            .WithEase(_hoverEase)
            .BindToColor(_buttonImage)
            .AddTo(_motionHandles);
    }

    private void OnMouseExit()
    {
        _motionHandles.Cancel();
        LMotion.Create(_hoverColor, Color.white, _hoverDuration)
            .WithEase(_hoverEase)
            .BindToColor(_buttonImage)
            .AddTo(_motionHandles);
    }
    private void OnMouseDown()
    {

    }
    
}