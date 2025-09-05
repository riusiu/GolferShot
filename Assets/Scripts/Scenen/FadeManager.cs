using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

public class FadeManager : Singleton<FadeManager>
{
    [SerializeField] private CanvasGroup _fadePanel;
    [SerializeField] private float       _fadeTime;

    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(this);
    }

    public async UniTask SceneFadeOut(System.Action fadeEndAction)
    {
        _fadePanel.gameObject.SetActive(true);
        await LMotion.Create(0f, 1f, _fadeTime)
            .WithOnComplete(() => fadeEndAction?.Invoke())
            .BindToAlpha(_fadePanel)
            .AddTo(gameObject);
    }

    public async UniTask SceneFadeIn(System.Action fadeEndAction)
    {
        await LMotion.Create(1f, 0f, _fadeTime)
            .WithOnComplete(() => { fadeEndAction?.Invoke(); _fadePanel.gameObject.SetActive(false); })
            .BindToAlpha(_fadePanel)
            .AddTo(gameObject);
    }

}