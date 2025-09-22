using System.Collections.Generic;                   // リスト用
using UnityEngine;                                  // Unityの基本機能
using TMPro;                                        // 順位テキストを出すなら

/// <summary>
/// リザルトシーン側の管理。
/// ・ResultCarrierから結果を受け取り、スコア降順で並べ替え
/// ・各スポットに「その場で」プレハブを生成して即時配置（移動演出なし）
/// ・1位はWinトリガー、2〜4位はPlaceトリガーを発火
/// ・物理/当たり/入力は無効化して事故防止
/// </summary>
public class ResultSceneManager : MonoBehaviour
{
    [Header("表彰台スポット（1位→2位→3位→4位の順）")]
    public Transform[] podiumSpots;                 // 各順位の位置/向きを置いたTransform（シーンに配置）

    [Header("向き・見せ方")]
    public Transform lookAtTarget;                  // 観客/カメラの方向（設定されていればこちらを向く）
    public bool faceSpotForwardIfNoLookAt = true;   // lookAtTarget未設定ならスポットのforwardを向く

    [Header("アニメーションのトリガー名")]
    public string triggerWin   = "Win";             // 1位用トリガー（Animator側に用意）
    public string triggerPlace = "lose";           // 2〜4位用トリガー（共通）

    [Header("ランキングUI（任意）")]
    public TextMeshProUGUI rankingText;             // 左上などに順位テキストを出す場合に割り当て

    [Header("代替用の簡易モデル（任意）")]
    public GameObject fallbackActorPrefab;          // プレハブ未設定時に使う代替モデル

    private List<ResultCarrier.Entry> _sorted;      // スコア降順で並べ替えた結果

    void Start()                                     // シーン開始時
    {
        BuildRankingFromCarrier();                   // キャリアから結果を受け取り/並べ替え
        SpawnInstantOnPodium();                      // ★即時スポーン＆配置（移動演出なし）
        BuildRankingUI();                            // 任意：順位表テキスト
    }

    private void BuildRankingFromCarrier()           // 結果を取り出してスコア降順に並べる
    {
        _sorted = new List<ResultCarrier.Entry>();   // 空リスト用意
        if (ResultCarrier.Instance != null)          // キャリアが存在していれば
            _sorted.AddRange(ResultCarrier.Instance.entries); // データをコピー

        // スコア降順 → 同点は名前昇順で安定化
        _sorted.Sort((a, b) =>
        {
            int cmp = b.score.CompareTo(a.score);    // スコア降順
            if (cmp != 0) return cmp;                // 違えば決定
            return string.Compare(a.displayName, b.displayName, System.StringComparison.Ordinal); // 同点なら名前
        });
    }

    private void SpawnInstantOnPodium()              // ★その場でスポーン＆ポーズ
    {
        int count = Mathf.Min(_sorted.Count, podiumSpots.Length); // 並べる人数を決定

        for (int i = 0; i < count; i++)              // 1位→2位→…
        {
            var e = _sorted[i];                      // i位のデータ
            var spot = podiumSpots[i];               // i位のスポット
            if (spot == null) continue;              // スポット未設定ならスキップ

            // 使うプレハブを決定（未設定なら代替を使う）
            GameObject prefab = e.actorPrefab != null ? e.actorPrefab : fallbackActorPrefab;
            if (prefab == null) continue;            // 代替も無ければスキップ

            // ★スポットの位置・回転でいきなり生成（移動演出なし）
            var go = Instantiate(prefab, spot.position, spot.rotation);
            go.name = $"{i+1:00}_{e.displayName}";   // わかりやすい名前

            // 安全のため：入力/物理/コライダーを止める（転倒/押し合い防止）
            var pi = go.GetComponent<UnityEngine.InputSystem.PlayerInput>(); // 入力
            if (pi) pi.enabled = false;              // 入力OFF
            var rb = go.GetComponent<Rigidbody>();   // 物理
            if (rb) { rb.isKinematic = true; rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            foreach (var c in go.GetComponentsInChildren<Collider>()) if (c) c.enabled = false; // 当たりOFF

            // 向き調整：lookAtTarget優先／無ければスポットforward（設定次第）
            Vector3 faceDir;
            if (lookAtTarget != null)
                faceDir = (lookAtTarget.position - go.transform.position);
            else if (faceSpotForwardIfNoLookAt)
                faceDir = spot.forward;
            else
                faceDir = go.transform.forward;

            faceDir.y = 0f;                          // 水平のみ
            if (faceDir.sqrMagnitude > 0.0001f)
                go.transform.rotation = Quaternion.LookRotation(faceDir.normalized);

            // トリガー発火：1位はWin／2〜4位はPlace
            var anim = go.GetComponent<Animator>();  // Animator取得
            if (anim != null)                        // Animatorがあれば
            {
                if (i == 0 && !string.IsNullOrEmpty(triggerWin))
                    anim.SetTrigger(triggerWin);     // 1位モーション
                else if (!string.IsNullOrEmpty(triggerPlace))
                    anim.SetTrigger(triggerPlace);   // 2〜4位モーション
            }
        }
    }

    private void BuildRankingUI()                   // 任意：左上に順位/スコア一覧を表示
    {
        if (rankingText == null) return;            // UI未設定ならスキップ
        System.Text.StringBuilder sb = new System.Text.StringBuilder(); // 文字列バッファ
        for (int i = 0; i < _sorted.Count; i++)     // 全員分
        {
            var e = _sorted[i];                     // エントリ
            sb.AppendLine($"{i+1}. {e.displayName}  -  {e.score}"); // 1. 名前 - スコア
        }
        rankingText.text = sb.ToString();           // テキストに反映
    }
}
