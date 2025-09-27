using UnityEngine;    // Unityの基本
using UnityEngine.UI; // UI

/// <summary>
/// 構え中だけ表示する縦ゲージ（Ping-Pong）。シングルトン禁止、プレイヤー専用。
/// ImageのFillAmount(0..1)で上下。離した瞬間の値を返す。
/// </summary>
public class PowerGaugeUI : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Canvas canvas;    // そのプレイヤーのCanvas（OverlayでOK）
    [SerializeField] private Image  fillImage; // MGPG_5_270px を割り当て（Fill=Vertical/BOTTOM）

    [Header("挙動")]
    [SerializeField] private float speed = 1.5f;   // 往復スピード（/秒）
    [SerializeField] private bool pingPong = true; // 端で折り返す

    private bool  active = false; // 稼働中？
    private float t01    = 0f;    // 現在値 0..1
    private int   dir    = +1;    // 方向 +1/-1

    public void Begin(float start01 = 0f)               // 構え開始
    {
        t01    = Mathf.Clamp01(start01);
        dir    = +1;
        active = true;
        if (canvas) canvas.enabled          = true;
        if (fillImage) fillImage.fillAmount = t01;
        gameObject.SetActive(true);
    }

    public float EndAndGet()                            // 構え終了（値確定）
    {
        active = false;
        gameObject.SetActive(false);                    // 非表示
        return t01;
    }

    public void Cancel()                                // 中断
    {
        active = false;
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!active) return;
        float delta = speed * Time.deltaTime;
        t01 += dir * delta;

        if (pingPong)
        {
            if (t01 > 1f) { t01 = 1f - (t01 - 1f); dir = -1; }
            if (t01 < 0f) { t01 = -t01;            dir = +1; }
        }
        else
        {
            t01 = Mathf.Clamp01(t01);
        }
        if (fillImage) fillImage.fillAmount = t01;
    }

    public float Current01 => t01;                      // 参照用
}