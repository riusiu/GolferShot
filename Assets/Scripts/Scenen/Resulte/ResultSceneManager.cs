using System.Collections.Generic;                   // リスト用
using UnityEngine;                                  // Unityの基本機能
using TMPro;                                        // 順位テキストを出すなら
using UnityEngine.InputSystem;                      // ★追加：PlayerInput検出用

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
    public string triggerPlace = "Place";           // 2〜4位用トリガー（共通）

    [Header("ランキングUI（任意）")]
    public TextMeshProUGUI rankingText;             // 左上などに順位テキストを出す場合に割り当て

    [Header("代替用の簡易モデル（任意）")]
    public GameObject fallbackActorPrefab;          // プレハブ未設定時に使う代替モデル

    private List<ResultCarrier.Entry> _sorted;      // スコア降順で並べ替えた結果

    void Start()                                     // シーン開始時
    {
        BuildRankingFromCarrier();                   // キャリアから結果を受け取り/並べ替え
        SpawnInstantOnPodium_Safe();                 // ★修正：安全生成版（OnEnable回避）
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

        // ★追加：何も来ていない場合の警告
        if (_sorted.Count == 0)
        {
            Debug.LogWarning("[ResultScene] ResultCarrier からエントリが届いていません。ゲーム側で CaptureAndLoadResultScene を呼べているか確認してください。");
        }
    }

    // ★修正：生成時の OnEnable を踏ませない“非アクティブ親”トリックで安全に出す
    private void SpawnInstantOnPodium_Safe()
    {
        if (podiumSpots == null || podiumSpots.Length == 0)
        {
            Debug.LogError("[ResultScene] podiumSpots が未設定です。Rank1/Rank2... のTransformを割り当ててください。");
            return;
        }

        int count = Mathf.Min(_sorted.Count, podiumSpots.Length); // 並べる人数を決定
        if (count == 0)
        {
            // ★最低限、空気にならないようフォールバック1体を出す（任意）
            if (fallbackActorPrefab != null)
            {
                var spot = podiumSpots[0];
                var container = new GameObject("FallbackContainer"); // 非アクティブ親
                container.transform.SetPositionAndRotation(spot.position, spot.rotation);
                container.SetActive(false);

                var actor = Instantiate(fallbackActorPrefab, container.transform); // 子として生成（OnEnableまだ走らない）
                StripGameplayComponents(actor);              // 入力/物理などを無効化
                container.SetActive(true);                   // まとめて表示

                var anim = actor.GetComponent<Animator>();
                if (anim) anim.SetTrigger(triggerWin);
            }
            return;
        }

        for (int i = 0; i < count; i++)              // 1位→2位→…
        {
            var e = _sorted[i];                      // i位のデータ
            var spot = podiumSpots[i];               // i位のスポット
            if (spot == null) { Debug.LogWarning($"[ResultScene] podiumSpots[{i}] が未設定です。スキップします。"); continue; }

            // 使うプレハブを決定（未設定なら代替を使う／それも無ければカプセルを生成）
            GameObject prefab = e.actorPrefab != null ? e.actorPrefab : fallbackActorPrefab;
            if (prefab == null)
            {
                prefab = GameObject.CreatePrimitive(PrimitiveType.Capsule); // 超簡易
                Destroy(prefab.GetComponent<Collider>());                   // 当たりは不要
                prefab.name = "CapsulePlaceholder";
            }

            // ★非アクティブ親を先に作る（子の OnEnable を抑止）
            var container = new GameObject($"{i+1:00}_{e.displayName}_Container");
            container.transform.SetPositionAndRotation(spot.position, spot.rotation);
            container.SetActive(false);                                     // まず親を非アクティブに

            // ★子として生成（この時点では OnEnable は走らない）
            var actor = Instantiate(prefab, container.transform, false);
            actor.name = $"{i+1:00}_{e.displayName}";

            // ★ゲームプレイ用の要素を完全に止める（入力/物理/当たり/自作Controller等）
            StripGameplayComponents(actor);

            // 向き調整：lookAtTarget優先／無ければスポットforward（設定次第）
            Vector3 faceDir;
            if (lookAtTarget != null)
                faceDir = (lookAtTarget.position - container.transform.position);
            else if (faceSpotForwardIfNoLookAt)
                faceDir = spot.forward;
            else
                faceDir = container.transform.forward;

            faceDir.y = 0f;
            if (faceDir.sqrMagnitude > 0.0001f)
                container.transform.rotation = Quaternion.LookRotation(faceDir.normalized);

            // ★最後にまとめて有効化（ここで初めて OnEnable が走るが、危険要素は既に無効）
            container.SetActive(true);

            // トリガー発火：1位はWin／2〜4位はPlace
            var anim = actor.GetComponent<Animator>();
            if (anim != null)
            {
                if (i == 0 && !string.IsNullOrEmpty(triggerWin))
                    anim.SetTrigger(triggerWin);
                else if (!string.IsNullOrEmpty(triggerPlace))
                    anim.SetTrigger(triggerPlace);
            }
        }
    }

    // ★追加：ゲームプレイ用コンポーネントを無効化/破棄するヘルパ
    private void StripGameplayComponents(GameObject actor)
    {
        // 入力
        foreach (var pi in actor.GetComponentsInChildren<PlayerInput>(true))
            if (pi) pi.enabled = false;                                   // Disableで十分（Destroyでも可）

        // 自作のプレイヤー制御（歩行/ジャンプ等）は破棄 or Disable
        foreach (var pc in actor.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (pc == null) continue;                                     // Missing Script対策
            var type = pc.GetType().Name;
            if (type == "PlayerController")                                // ★あなたのコントローラ名に合わせる
                Destroy(pc);                                               // リザルトでは不要なので破棄
        }

        // 物理
        foreach (var rb in actor.GetComponentsInChildren<Rigidbody>(true))
            if (rb) { rb.isKinematic = true; rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }

        // 当たり
        foreach (var col in actor.GetComponentsInChildren<Collider>(true))
            if (col) col.enabled = false;

        // NavMesh系やオーディオ、パーティクル等も必要に応じて停止
    }

    private void BuildRankingUI()                   // 任意：左上に順位/スコア一覧を表示
    {
        if (rankingText == null) return;            // UI未設定ならスキップ
        if (_sorted == null) return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < _sorted.Count; i++)
        {
            var e = _sorted[i];
            sb.AppendLine($"{i+1}. {e.displayName}  -  {e.score}");
        }
        rankingText.text = sb.ToString();
    }
}
