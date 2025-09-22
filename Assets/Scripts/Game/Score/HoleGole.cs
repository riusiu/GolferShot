using UnityEngine;                                  // Unityの基本機能

/// <summary>
/// ホール到達時の処理（Destroy は絶対にしない）
/// ・ShotOwnership から打ったプレイヤーを特定してスコア加算
/// ・同じオブジェクトの二重加点を防止
/// ・KillZone はホールの「さらに下」に置く運用
/// </summary>
[RequireComponent(typeof(Collider))]                 // 必ずトリガーコライダーを付ける
public class HoleGoal : MonoBehaviour
{
    [Header("スコア定義（タグ→点数）")]
    public TargetTypeCatalog catalog;                // 既存のカタログ（Entryball）を使用
    public int defaultScore = 1;                     // カタログ未登録の時の点数

    [Header("適用レイヤー")]
    public LayerMask targetLayers = ~0;              // これに含まれるレイヤーのみ有効

    [Header("二重加点防止")]
    public float sameObjectCooldown = 1.0f;          // 同じオブジェクトが連続で入った時の冷却秒

    // 内部：最近スコア化したオブジェクトの時刻を記録（ID→時刻）
    private readonly System.Collections.Generic.Dictionary<int, float> _lastScoredTime
        = new System.Collections.Generic.Dictionary<int, float>();

    void Awake()
    {
        var col = GetComponent<Collider>();          // 自分のコライダー取得
        col.isTrigger = true;                        // トリガーにする
    }

    void OnTriggerEnter(Collider other)              // 何かが入った
    {
        // レイヤーフィルタ
        if (((1 << other.gameObject.layer) & targetLayers) == 0) return; // 対象外

        // ルート（Rigidbody基準）を確定
        Transform root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;

        // 二重加点防止：直近でスコアにしていればスキップ
        int id = root.GetInstanceID();               // 一意のID
        if (_lastScoredTime.TryGetValue(id, out float t) && Time.time - t < sameObjectCooldown)
            return;                                  // 冷却中

        _lastScoredTime[id] = Time.time;             // 記録を更新

        // 誰の得点か：ShotOwnership から最後に打ったプレイヤーを取得
        var own = root.GetComponent<ShotOwnership>(); // 所有情報
        var shooter = own ? own.GetShooter() : null;  // PlayerController 参照

        // 点数を決める：タグ→カタログ、無ければdefault
        string tagName = root.tag;                   // 対象のタグ
        int add = defaultScore;                      // 初期値
        if (catalog != null)                         // カタログがあれば
        {
            var e = catalog.Get(tagName);            // 該当エントリ（Entryball想定）
            if (e != null) add = Mathf.Max(0, e.score); // 負数は防止
        }

        // スコア加算（PlayerControllerにscoreがある前提。無ければ適宜合わせてください）
        if (shooter != null)
        {
            shooter.score += add;                    // 直接加算（AddScore関数があるならそちらで）
            Debug.Log($"[HoleGoal] +{add} to {shooter.name} by {tagName}");
        }
        else
        {
            Debug.Log($"[HoleGoal] +{add} (no shooter) by {tagName}");
        }

        // 重要：ここで Destroy はしない。落下して下の KillZone が処理する想定。
    }
}
