using TMPro;                                        // TextMeshProUGUI を使うため
using UnityEngine;                                   // Unityの基本機能
using Cysharp.Threading.Tasks;                       // UniTask（非同期/コルーチン代替）
                                                     // ※ ResultCarrier は別ファイル（DontDestroyOnLoadでデータ運搬）

public class TimerView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;  // 画面に表示するテキスト参照（mm:ss 表示）

    [SerializeField] private float _timer = 3f * 60f;                 // 残りタイマー（初期値：3分=180秒）

    private System.Action _endCallback;              // タイムアップ時に呼びたい任意のコールバック（外部用）

    // ==== ★追加：リザルト遷移の設定 ====
    [Header("Result Scene")]
    [SerializeField] private string resultSceneName = "ResultScene"; // 遷移先のリザルトシーン名（ビルドに含める）
    [SerializeField] private bool   loadResultOnEnd = true;          // タイムアップ時に自動でリザルトへ行くか
    [SerializeField] private float  loadDelay       = 1.0f;          // タイムアップ後、遷移までの待機（演出用/秒）

    void Start()                                                      // ゲーム開始時に1回呼ばれる
    {
        TimerDown().Forget();                                         // 非同期のカウントダウンを開始（awaitせず投げっぱなし）
    }
    
    private async UniTask TimerDown()                                 // タイマーを1フレームずつ減らす非同期処理
    {
        while (_timer > 0f)                                           // 残り時間が0になるまでループ
        {
            _timer -= Time.deltaTime;                                 // 経過フレームの実時間だけ減らす
            _timer = Mathf.Max(_timer, 0f);                           // マイナスにならないように下限クランプ

            var min  = Mathf.Floor(_timer / 60f);                     // 分 = 60で割って切り捨て
            var sec  = Mathf.Floor(_timer - min * 60f);               // 秒 = 残りから分相当を引いて切り捨て
            // var msec = (_timer - (min * 60 + sec)) * 100;          // 100分の1秒（未表示だが残しておくならこのまま）

            if (_text != null)                                        // テキスト参照が割り当て済みなら
                _text.text = min.ToString() + ":" + sec.ToString("00"); // "m:ss" 形式で表示（秒はゼロ埋め2桁）

            await UniTask.Yield();                                    // 次のフレームまで待機（UI更新や他処理に譲る）
        }

        _endCallback?.Invoke();                                       // タイムアップ時：外部コールバックがあれば実行

        // ==== ★ここから：タイムアップ→リザルトシーンへ ====
        if (loadResultOnEnd)                                          // 自動遷移が有効なら
        {
            if (loadDelay > 0f)                                       // 演出用に少し待つ設定なら
                await UniTask.Delay(System.TimeSpan.FromSeconds(loadDelay)); // 指定秒数待機（演出やSEに合わせる）

            // 現在シーンのプレイヤーとスコア/プレハブ情報を回収して、リザルトシーンへ移動
            ResultCarrier.Instance.CaptureAndLoadResultScene(resultSceneName); // 前回のResultCarrierを利用して遷移
        }
    }

    // ==== ★任意：外部から「タイムアップ時にこれもやって」を差し込むためのAPI ====
    public void SetOnEnd(System.Action onEnd)                          // 外部からタイムアップ時の処理を登録
    {
        _endCallback = onEnd;                                          // コールバックを差し替え
    }

    // ==== ★任意：外部から残り時間をセット/リセットしたい時のAPI ====
    public void SetTimeSeconds(float seconds)                          // 残り時間を秒で設定（例：SetTimeSeconds(90f)）
    {
        _timer = Mathf.Max(0f, seconds);                               // マイナスにならないようクランプ
    }
}
