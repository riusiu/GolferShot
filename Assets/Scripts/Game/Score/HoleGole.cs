using UnityEngine;                                // Unityの基本機能

/// <summary>
/// 「ホール」。Trigger にしておき、ショットされたオブジェクトが入ると：
/// 1) ShotOwnership から「打った人」を特定
/// 2) TargetTypeCatalog（Entryball）から点数等を取得して加点
/// 3) 設定に応じてオブジェクトを破棄
/// </summary>
[RequireComponent(typeof(Collider))]               // ★Trigger コライダーが必須
public class HoleGoal : MonoBehaviour
{
    [Header("参照")]
    public TargetTypeCatalog targetCatalog;        // ★既存のカタログを使用

    [Header("受け付け設定")]
    public LayerMask acceptLayers = ~0;            // ★受け付けるレイヤー（任意で絞る）

    [Header("フォールバック")]
    public int  defaultPoints = 1;                 // ★Entryballにpointsが無い/見つからない場合のスコア
    public bool defaultDestroyOnScore = true;      // ★EntryballにdestroyOnScoreが無い場合の既定

    private Collider _col;                         // 自身のコライダー参照

    void Awake()
    {
        _col = GetComponent<Collider>();           // コライダー取得
        _col.isTrigger = true;                     // 必ず Trigger にする
    }

    void OnTriggerEnter(Collider other)            // ★何かが入った
    {
        // レイヤーフィルタ（不要なら受け付ける）
        if (((1 << other.gameObject.layer) & acceptLayers) == 0)
            return;                                // レイヤー対象外

        // ルート取得（Rigidbodyがあればそちらを優先）
        Transform root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;

        // 「誰が打ったか」の情報を取り出し（無ければスコア不可）
        var ownership = root.GetComponent<ShotOwnership>(); // ShotOwnership を探す
        if (ownership == null) return;              // ショットされていない（=自然落下など）は無効
        if (ownership.hasScored) return;            // 既に加点済みなら無視（多重防止）

        // 得点者の特定（無ければ無効）
        var shooter = ownership.lastShooter;        // 最後に打ったプレイヤー
        if (shooter == null) return;                // 誰にも属していない＝無効

        // タグから点数・破棄可否を決定（TargetTypeCatalog を使用）
        string tagName = root.gameObject.tag;       // ターゲットのタグ

        int points = defaultPoints;                 // 既定スコア
        bool doDestroy = defaultDestroyOnScore;     // 既定の破棄可否

        if (targetCatalog != null)
        {
            var entry = targetCatalog.Get(tagName); // ★あなたのカタログAPI（前回までと同じ）
            if (entry != null)
            {
                // ▼項目名が "points" でない場合（例: "score"）は下の行を書き換えてください
                points = entry.score;              // ← ここを entry.score; にしてもOK（プロジェクトの命名に合わせる）
                // ▼destroyOnScore を Entryball に用意していない場合はフォールバック値が使われます
                try { doDestroy = entry.destroyOnScore; } catch { /* 無ければ既定 */ }
            }
        }

        // プレイヤーへ加点
        shooter.AddScore(points);                   // ★PlayerController の AddScore(int) を呼ぶ

        // 多重防止フラグ
        ownership.hasScored = true;                 // もうスコア済み

        // オブジェクト破棄（設定に応じて）
        // //if (doDestroy)
        // {
        //     Destroy(root.gameObject);               // 当たったオブジェクトを削除
        // }

        // デバッグ表示
        Debug.Log($"[HoleGoal] {shooter.name} が {tagName} で +{points} 点");
    }
}
