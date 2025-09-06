using UnityEngine; // Unityの基本機能
using TMPro;       // TextMeshProを使うため

/// <summary>
/// 画面のTextMeshProUGUIにプレイヤーのスコアを表示するだけの軽量UI。
/// ・PlayerControllerのOnScoredイベントを購読して自動で更新
/// ・開始時にも現在スコアを即時反映
/// ・割り当てが空でもできるだけ自動取得でフォロー
/// </summary>
public class ScoreUI : MonoBehaviour
{
    [Header("参照")]
    public PlayerController targetPlayer; // 表示したいプレイヤー（未設定なら自動探索）
    public TextMeshProUGUI scoreText;     // 画面上のText（TMPro）

    [Header("表示フォーマット")]
    public string format = "SCORE : {0}";     // 表示形式（{0}にスコアが入る）

    void Awake()
    {
        // ▼scoreTextが未設定なら、同じオブジェクトから自動取得を試みる
        if (scoreText == null)
            scoreText = GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        // ▼プレイヤー未指定なら、親かシーンから自動で探す（ローカル1人想定ならこれでOK）
        if (targetPlayer == null)
        {
            targetPlayer = GetComponentInParent<PlayerController>(); // 親に居れば取る
            if (targetPlayer == null)
                targetPlayer = FindObjectOfType<PlayerController>(); // シーンから最初の1人を探す
        }

        // ▼イベント購読：スコアが増えたら即UI更新
        if (targetPlayer != null)
        {
            targetPlayer.OnScored += HandleScored; // (加点, 合計) が飛んでくる
            UpdateScoreNow();                      // 開始時点のスコアも反映
        }
        else
        {
            // プレイヤーが見つからない場合の最小フォールバック表示
            if (scoreText) scoreText.text = string.Format(format, 0);
        }
    }

    void OnDestroy()
    {
        // ▼イベント購読解除（メモリリーク/重複購読防止）
        if (targetPlayer != null)
            targetPlayer.OnScored -= HandleScored;
    }

    private void HandleScored(int added, int total)
    {
        // ▼加点イベントを受け取ったらTextを更新
        if (scoreText)
            scoreText.text = string.Format(format, total);
    }

    public void UpdateScoreNow()
    {
        // ▼明示的に現在値を表示したい時に呼べる
        if (targetPlayer != null && scoreText != null)
            scoreText.text = string.Format(format, targetPlayer.score);
    }
}