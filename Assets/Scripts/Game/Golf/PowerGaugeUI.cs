using UnityEngine;                                   // Unityの基本機能
using UnityEngine.UI;                                // UI Image を使う
using System.Collections.Generic;                    // 辞書を使う（プレイヤー毎の保存に使用）

/// <summary>
/// 画面に表示するパワーゲージ。
/// ・構え開始で Begin(owner) を呼ぶと表示&上下往復を開始
/// ・離した瞬間に Commit(owner) を呼ぶとその位置を確定し、倍率(0..1)を保存して非表示
/// ・倍率は ShotPowerBuffer に保存され、ShotHitCollider から参照される
/// </summary>
[RequireComponent(typeof(Image))]                     // 必ず Image が必要
public class PowerGaugeUI : MonoBehaviour
{
    [Header("参照（必須）")]
    public Image gaugeImage;                          // ゲージ画像（あなたのPNGを貼ったImage）

    [Header("動き")]
    public float pingPongSpeed = 1.5f;                // 上下往復の速さ（往復/秒 のイメージ）
    public float minFill = 0.0f;                      // 最小の fillAmount（0=最下）
    public float maxFill = 1.0f;                      // 最大の fillAmount（1=最上）
    public bool  startFromBottom = true;              // 構え開始時に下から始めるか

    [Header("見た目")]
    public CanvasGroup canvasGroup;                   // フェード/非表示に使う（未指定なら自動補完）
    public bool fadeInOut = true;                     // 出/消えをふわっとさせるか
    public float fadeTime = 0.12f;                    // フェード時間（秒）

    [Header("パワー補正（UI位置→最終倍率の写像）")]
    public AnimationCurve powerCurve =                // 0..1→0..1（例：上の方が強くなるS字など）
        AnimationCurve.Linear(0, 0, 1, 1);            // 既定は線形
    public bool clamp01 = true;                       // 曲線の出力を0..1にクランプするか

    // 内部
    private bool _active = false;                     // 構え中フラグ（アニメ更新するか）
    private float _phase = 0f;                        // PingPong 用の位相
    private PlayerController _currentOwner;           // 今ゲージを使っているプレイヤー

    // ===== シングルトン相当（シーンに1つ想定） =====
    private static PowerGaugeUI _instance;            // 自分の参照を静的に保持
    public static PowerGaugeUI Instance => _instance; // 外から取れる入口

    void Awake()
    {
        _instance = this;                             // インスタンス登録
        if (!gaugeImage) gaugeImage = GetComponent<Image>();        // 自動取得
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();// あれば取得
        // Image の設定チェック（自動で直す）
        if (gaugeImage != null)
        {
            gaugeImage.type = Image.Type.Filled;                      // Filled で使う
            gaugeImage.fillMethod = Image.FillMethod.Vertical;        // 縦に満ちる
            gaugeImage.fillOrigin = 0;                                // 0=下から 1=上から（お好みで）
            gaugeImage.fillAmount = startFromBottom ? minFill : maxFill; // 初期fill
            gaugeImage.preserveAspect = true;                         // 縦横比保持
        }
        SetVisible(false, true);                                      // 起動時は非表示（即時）
    }

    void Update()
    {
        if (!_active || gaugeImage == null) return;                   // 構え中だけ動かす

        // 時間ベースの Ping-Pong（0→1→0→…）
        _phase += Time.unscaledDeltaTime * pingPongSpeed * 2f;        // 2倍で「往復/秒」の体感に近づける
        float t01 = Mathf.PingPong(_phase, 1f);                       // 0..1 を往復

        // min..max へ線形補間（ゲージの上下）
        gaugeImage.fillAmount = Mathf.Lerp(minFill, maxFill, t01);
    }

    /// <summary>
    /// 構え開始時に呼ぶ：ゲージを表示＆往復開始
    /// </summary>
    public void Begin(PlayerController owner)
    {
        _currentOwner = owner;                                        // 誰のゲージか覚える
        _phase = startFromBottom ? 0f : 1f;                           // 始点
        _active = true;                                               // 作動ON
        SetVisible(true, !fadeInOut ? true : false);                  // 表示（即時 or フェード）
        // 初期fill
        if (gaugeImage)
            gaugeImage.fillAmount = startFromBottom ? minFill : maxFill;
    }

    /// <summary>
    /// 離した瞬間に呼ぶ：現在の位置を確定→倍率(0..1)を ShotPowerBuffer に保存→非表示
    /// </summary>
    public float Commit(PlayerController owner)
    {
        if (gaugeImage == null) { Cancel(); return 1f; }              // 画像が無ければ1倍で返す

        // fillAmount を 0..1 正規化（min/max の間だけを有効範囲とする）
        float t01 = 0.5f;
        if (Mathf.Abs(maxFill - minFill) > 0.0001f)
            t01 = Mathf.InverseLerp(minFill, maxFill, gaugeImage.fillAmount);

        // 曲線で補正（例：下を弱く、上を強く、など）
        float scaled = powerCurve != null ? powerCurve.Evaluate(t01) : t01;
        if (clamp01) scaled = Mathf.Clamp01(scaled);

        // プレイヤー毎に保存（ShotHitCollider から参照）
        ShotPowerBuffer.Set(owner, scaled);

        _active = false;                                              // アニメ停止
        SetVisible(false, !fadeInOut ? true : false);                 // 非表示（即時 or フェード）

        return scaled;                                                // 使った倍率を返す（必要なら）
    }

    /// <summary>
    /// キャンセル（撃たなかった等）：非表示に戻す
    /// </summary>
    public void Cancel()
    {
        _active = false;                                              // 停止
        SetVisible(false, !fadeInOut ? true : false);                 // 隠す
        if (_currentOwner) ShotPowerBuffer.Clear(_currentOwner);      // 残存倍率を消す
        _currentOwner = null;
    }

    // ===== 表示制御（CanvasGroup を使ってフェード/即時） =====
    private void SetVisible(bool show, bool instant)
    {
        if (canvasGroup == null)
        {
            // CanvasGroup が無ければ単純に Active を切る
            gameObject.SetActive(show);
            return;
        }

        gameObject.SetActive(true);                                   // CanvasGroup を使う場合は ON にしてから
        StopAllCoroutines();                                          // フェードの競合を止める
        if (instant || fadeTime <= 0f)
        {
            canvasGroup.alpha = show ? 1f : 0f;
            canvasGroup.interactable = show;
            canvasGroup.blocksRaycasts = show;
            if (!show) gameObject.SetActive(false);
        }
        else
        {
            StartCoroutine(Fade(show ? 1f : 0f));
        }
    }

    private System.Collections.IEnumerator Fade(float to)
    {
        float from = canvasGroup.alpha;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / fadeTime);
            canvasGroup.alpha = Mathf.Lerp(from, to, a);
            yield return null;
        }
        canvasGroup.alpha = to;
        bool show = to > 0.5f;
        canvasGroup.interactable = show;
        canvasGroup.blocksRaycasts = show;
        if (!show) gameObject.SetActive(false);
    }
}
