using UnityEngine;                                      // Unityの基本機能
using System.Collections;                               // コルーチン(IEnumerator)を使うため

/// <summary>
/// 「ショットまではKinematic、ショット時にDynamic、止まったら（orタイムアウト）Kinematicに戻す」
/// ＋「スポーン直後は一定時間（既定3秒）だけDynamicで落下させ、その後自動でKinematic固定」
/// を管理するコンポーネント。打てるオブジェクト側に付ける。
/// </summary>
[DisallowMultipleComponent]                             // 同じコンポーネントの重複付与を防ぐ
public class AutoKinematicOnStop : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Rigidbody rb;              // 監視対象のRigidbody（未設定ならAwakeで取得）

    [Header("初期状態")]
    public bool startAsKinematic = false;               // ★変更推奨：スポーン直後はDynamic(=false)で落下させる
                                                        //  trueだと最初からKinematicになり落下しません

    [Header("停止判定（ショット後用）")]
    public float nearZeroSpeed = 0.15f;                 // 「ほぼ停止」とみなす速度（m/s）
    public float settleDuration = 0.5f;                 // ほぼ停止がこの秒数続いたらKinematicへ戻す

    [Header("保険（ショット後タイムアウト）")]
    public float maxDynamicSeconds = 7f;                // ショット後、何秒経ったら強制でKinematicに戻すか

    [Header("スポーン直後の自動固定")]
    public bool  autoKinematicAfterStart = true;        // 起動直後に「一定時間後にKinematicへ固定」するか
    public float startKinematicDelay = 3f;              // 何秒後に固定を試みるか（既定3秒）

    // 内部状態
    private bool shotActive = false;                    // いま「ショット後で監視中」か
    private float shotStartTime = -1f;                  // ショットが始まった実時間（秒）
    private float lowSpeedTimer = 0f;                   // 低速継続時間のカウンタ
    private Coroutine startFixRoutine;                  // 起動後の自動固定コルーチン参照

    void Awake()                                        // 起動時
    {
        if (!rb) rb = GetComponent<Rigidbody>();        // Rigidbodyを取得（未割り当て対策）
        if (!rb)                                        // 無ければエラー通知して停止
        {
            Debug.LogError($"[AutoKinematicOnStop] Rigidbody が見つかりません: {name}");
            enabled = false;
            return;
        }

        // 初期のKinematic状態を設定（落下させたい場合は false 推奨）
        SetKinematic(startAsKinematic);                 // true:固定 / false:落下(Dynamic)
    }

    void Start()                                        // Awakeの次に1回呼ばれる
    {
        // スポーン直後の自動固定を使う場合はコルーチンを開始
        if (autoKinematicAfterStart)
        {
            // 既存のルーチンがあれば止める（多重起動防止）
            if (startFixRoutine != null) StopCoroutine(startFixRoutine);
            startFixRoutine = StartCoroutine(AutoFixAfterStartDelay()); // 3秒待ってからKinematic化を試みる
        }
    }

    void Update()                                       // 毎フレーム監視（ショット後の停止判定用）
    {
        if (!shotActive) return;                        // ショット監視中でなければ何もしない

        float elapsed = Time.time - shotStartTime;      // ショット開始からの経過秒
        float speed = rb.velocity.magnitude;            // 現在速度（大きさ）

        if (speed <= nearZeroSpeed)                     // 低速なら
            lowSpeedTimer += Time.deltaTime;            // 継続時間を加算
        else
            lowSpeedTimer = 0f;                         // 高速になったらリセット

        bool shouldKinematicByStop = (lowSpeedTimer >= settleDuration); // 一定時間低速が続いたか
        bool shouldKinematicByTimeout = (elapsed >= maxDynamicSeconds); // タイムアウトしたか

        if (shouldKinematicByStop || shouldKinematicByTimeout) // どちらか成立したら
        {
            ReturnToKinematic();                        // Kinematicへ戻す
        }
    }

    /// <summary>
    /// ショットの瞬間に呼ぶ：Dynamic化し、ショット後の停止監視を開始。
    /// </summary>
    public void ActivateDynamicOnShot()
    {
        // 起動直後の自動固定タイマーはショットが起きたら不要なので停止
        if (startFixRoutine != null)
        {
            StopCoroutine(startFixRoutine);             // 自動固定をキャンセル
            startFixRoutine = null;                     // 参照クリア
        }

        SetKinematic(false);                            // Dynamicに切り替え（落下/加速OK）
        rb.WakeUp();                                    // スリープ解除（確実に動かす）
        shotActive = true;                              // ショット後監視フラグON
        shotStartTime = Time.time;                      // 開始時刻を記録
        lowSpeedTimer = 0f;                             // 低速カウンタをリセット
    }

    /// <summary>
    /// いま強制でKinematicに戻したい時（リスポーンや停止演出の直後など）。
    /// </summary>
    public void ForceKinematicNow()
    {
        // 起動直後の自動固定タイマーは不要になるので停止
        if (startFixRoutine != null)
        {
            StopCoroutine(startFixRoutine);
            startFixRoutine = null;
        }
        ReturnToKinematic();                            // 内部の戻し処理をそのまま使用
    }

    // ===== 内部ユーティリティ =====

    private void ReturnToKinematic()                    // Kinematicへ戻す共通処理
    {
        rb.velocity = Vector3.zero;                     // 速度を完全停止
        rb.angularVelocity = Vector3.zero;              // 回転速度も停止
        SetKinematic(true);                             // Kinematicに切り替え（固定）
        shotActive = false;                             // ショット監視終了
        // Debug.Log($"[AutoKinematicOnStop] Back to Kinematic: {name}");
    }

    private void SetKinematic(bool kinematic)           // Kinematic切り替えのラッパ
    {
        rb.isKinematic = kinematic;                     // 物理のON/OFF
        rb.interpolation = kinematic                    // 見栄えの補間設定
            ? RigidbodyInterpolation.None               // 固定中は補間なし
            : RigidbodyInterpolation.Interpolate;       // 動作中は補間あり
        rb.collisionDetectionMode = kinematic           // 当たり精度（負荷）設定
            ? CollisionDetectionMode.Discrete           // 固定中は離散
            : CollisionDetectionMode.ContinuousSpeculative; // 動作中は高精度寄り
    }

    private IEnumerator AutoFixAfterStartDelay()        // 起動直後の自動固定コルーチン
    {
        // 1) 指定秒数だけ待つ（この間にショットが来たら終了）
        float endTime = Time.time + Mathf.Max(0f, startKinematicDelay); // 目標時刻を算出
        while (Time.time < endTime)                     // 指定時間まで待機
        {
            if (shotActive) yield break;                // もうショットが起きていればキャンセル
            yield return null;                          // 1フレーム待つ
        }

        // 2) その後、速度がほぼ0になるのを少し待ってから固定（空中で固まるのを回避）
        float localLowTimer = 0f;                       // 低速継続カウンタ
        float safetyTimeout = Mathf.Max(3f, settleDuration * 2f); // 追加の保険時間
        float safetyStart = Time.time;                  // 保険の基点時刻

        while (!shotActive)                             // ショットが来るまでは監視継続
        {
            float spd = rb.velocity.magnitude;          // 現在速度
            if (spd <= nearZeroSpeed) localLowTimer += Time.deltaTime; // 低速継続を加算
            else localLowTimer = 0f;                    // 高速ならリセット

            bool settled = (localLowTimer >= settleDuration);          // 十分に落ち着いた？
            bool safety  = (Time.time - safetyStart >= safetyTimeout); // 保険時間を超えた？

            if (settled || safety)                      // どちらか成立したら
            {
                ReturnToKinematic();                    // Kinematic固定
                break;                                  // ループ終了
            }
            yield return null;                          // 次フレームへ
        }

        startFixRoutine = null;                         // コルーチン参照クリア
    }
}
