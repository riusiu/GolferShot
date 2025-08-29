using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChangeButton : MonoBehaviour
{
    [SerializeField, Scene] private int _sceneIndex;
    [SerializeField, Label("ボタン")] private SpriteRenderer _buttonRenderer;
    [SerializeField, Label("色変更時間")] private float _hoverDuration;
    [SerializeField, Label("クリック時間")] private float _clickDuration;
    [SerializeField] private bool _isSelected = false;
    [SerializeField] private Ease _hoverEase;
    [SerializeField] private Ease _clickEase;

    private readonly CompositeMotionHandle _motionHandles = new(2);
    private int _index;
    private System.Action<int> _mouseAction;
    private bool _isSceneChanged = false;

    private void Awake()
    {
        if (_isSelected) _buttonRenderer.color = Color.white;
    }

    public void SetMouseActionCallback(int index, System.Action<int> mouseAction)
    {
        _index = index;
        _mouseAction = mouseAction;
    }

    private void OnMouseEnter()
    {
        if (_isSelected) return;
        _motionHandles.Cancel();
        LMotion.Create(Color.gray, Color.white, _hoverDuration)
            .WithEase(_hoverEase)
            .BindToColor(_buttonRenderer)
            .AddTo(_motionHandles);
        _isSelected = true;
        _mouseAction?.Invoke(_index);
    }

    private void OnMouseDown()
    {
        if (_isSceneChanged) return;
        _isSceneChanged = true;
        LMotion.Create(Vector3.one, Vector3.one * 0.9f, _clickDuration)
            .WithLoops(2, LoopType.Yoyo)
            .WithEase(_clickEase)
            .BindToLocalScale(transform)
            .AddTo(gameObject);
        FadeManager.Instance.SceneFadeOut(() =>
        {
            SceneManager.LoadScene(_sceneIndex);
            FadeManager.Instance.SceneFadeIn(null).Forget();
        }).Forget();
    }

    public void MouseExit()
    {
        if (_isSelected is false) return;
        _motionHandles.Cancel();
        LMotion.Create(Color.white, Color.gray, _hoverDuration)
            .WithEase(_hoverEase)
            .BindToColor(_buttonRenderer)
            .AddTo(_motionHandles);
        _isSelected = false;
    }
}
