using System.Collections.Generic;                   // リスト/セット用
using UnityEngine;                                  // Unityの基本機能

// 構え中だけでなく常時ONの検出トリガーとして使える、汎用Aim検出（最小版）
// ・Triggerに入っている対象のうち、TargetTypeCatalog(Entryball)に載っているタグだけを候補にする
// ・最も近い対象と「安全半径」を計算して保持（めり込み防止）：bounds(X/Z最大) + catalog.padding
// ・ロックAPI：TryLockTarget() で“確定”し、LockedTarget / LockedDistanceXZ を参照
// ・ReleaseLock() でロック解除＆再取得クールダウン（暴発/ワープ防止）
// ※ Proxy は一切使いません（検出・半径ともに本体のみ）
[RequireComponent(typeof(Collider))]                 // 必ずTriggerコライダーが必要
public class AimDetector : MonoBehaviour
{
    [Header("参照")]
    public PlayerController owner;                  // 検出結果を渡す相手（プレイヤー）
    public TargetTypeCatalog catalog;               // 共通ターゲット定義（タグ＋スコア＋余白）

    [Header("設定")]
    public LayerMask targetMask = ~0;               // レイヤーフィルタ（本体レイヤーのみ/AimOnlyは入れない）
    public bool pushWhileAiming = true;             // 構え中に現在候補をownerへ自動プッシュするか
    public float minOrbitRadius = 0.5f;             // 安全半径の下限
    public float maxOrbitRadius = 8f;               // 安全半径の上限

    // 内部状態（検出候補）
    private readonly HashSet<Collider> candidates = new HashSet<Collider>(); // いま範囲内の候補
    private Transform currentTarget;                // 最も近い対象（周回中心＝対象Transform）
    private float    currentSafeRadius;             // その対象の安全半径（めり込み防止用）

    // ロック状態（構え確定時に固定）
    private Transform lockedTarget;                 // ロック中の対象（null可）
    private float    lockedDistanceXZ;              // ロック時の水平距離（固定半径）
    private float    reacquireBlockUntil = 0f;      // ロック解除後の再取得ブロック時刻（暴発/ワープ防止）

    // プロパティ（プレイヤー側から参照）
    public Transform CurrentTarget    => currentTarget;     // 現在の対象（null可）
    public float    CurrentSafeRadius => currentSafeRadius; // 現在の安全半径
    public Transform LockedTarget     => lockedTarget;      // ロック中の対象（null可）
    public float    LockedDistanceXZ  => lockedDistanceXZ;  // ロック中の固定半径

    private Collider triggerCol;                   // 自分のトリガー

    void Awake()
    {
        triggerCol = GetComponent<Collider>();     // コライダー取得
        triggerCol.isTrigger = true;               // 必ずTriggerにする
    }

    void Update()
    {
        // 再取得ブロック時間が過ぎていれば「現在候補」を更新
        if (Time.time >= reacquireBlockUntil)
        {
            SelectNearestAndComputeRadius();       // 近傍の最良候補を選ぶ
        }

        // 構え中：ownerへ現在の対象情報をプッシュ（ロック前はcurrent／ロック後はlocked）
        if (pushWhileAiming && owner != null && IsOwnerAiming())
        {
            if (lockedTarget != null)
            {
                owner.SetCurrentAimTarget(lockedTarget);
                owner.SetCurrentAimOrbitRadius(lockedDistanceXZ);
            }
            else
            {
                owner.SetCurrentAimTarget(currentTarget);
                owner.SetCurrentAimOrbitRadius(currentSafeRadius);
            }
        }
    }

    private bool IsOwnerAiming()                   // オーナーが構え中かを確認
    {
        // PlayerControllerに public bool IsAiming() がある想定
        try { return owner != null && owner.IsAiming(); } catch { return false; }
    }

    private void OnTriggerEnter(Collider other)    // 候補が入ってきた
    {
        if (!IsValidTarget(other)) return;         // タグ/レイヤー不一致なら無視
        candidates.Add(other);                     // 候補に追加
    }

    private void OnTriggerExit(Collider other)     // 候補が出ていった
    {
        candidates.Remove(other);                  // 候補から除外
        if (currentTarget != null && other.transform == currentTarget)
            currentTarget = null;                  // 現在対象だったら外す
    }

    private bool IsValidTarget(Collider col)       // タグとレイヤーの判定
    {
        if (catalog == null) return false;                         // カタログ未設定なら無効
        if (((1 << col.gameObject.layer) & targetMask) == 0)       // レイヤーマスク外
            return false;
        string tag = col.gameObject.tag;                           // オブジェクトのタグ
        return catalog.Contains(tag);                              // カタログに載っていればOK
    }

    private void SelectNearestAndComputeRadius()   // 最寄りの対象と半径を更新
    {
        float bestSqr = float.PositiveInfinity;    // 最短距離（自乗）初期化
        Transform bestT = null;                    // ベストターゲット
        float bestRadius = minOrbitRadius;         // ベスト半径

        Vector3 origin = owner ? owner.transform.position : transform.position; // 基準位置

        foreach (var col in candidates)            // 範囲内の候補を走査
        {
            if (col == null) continue;             // 破棄済みはスキップ
            if (!IsValidTarget(col)) continue;     // 動的に無効になる場合もある

            Vector3 to = col.bounds.center - origin; // 中心へのベクトル
            float sqr = to.sqrMagnitude;             // 距離の二乗（高速）

            if (sqr < bestSqr)                       // より近ければ更新
            {
                bestSqr = sqr;
                bestT   = col.transform;
                bestRadius = ComputeSafeRadius(col); // 半径を計算
            }
        }

        currentTarget    = bestT;                    // 結果を保存
        currentSafeRadius = Mathf.Clamp(bestRadius,  // 半径を下限/上限で制限
                                        minOrbitRadius, maxOrbitRadius);
    }

    private float ComputeSafeRadius(Collider col)   // めり込み防止の「安全半径」
    {
        // コライダーの境界ボックス（ワールド）のX/Z最大半径をベース
        Bounds b = col.bounds;
        Vector3 e = b.extents;
        float r = Mathf.Max(e.x, e.z);

        // タグごとの余白（padding）をカタログから取得して足す
        float pad = 0f;
        if (catalog != null)
        {
            var entry = catalog.Get(col.gameObject.tag); // Entryball
            if (entry != null) pad = Mathf.Max(0f, entry.padding);
        }
        return r + pad;
    }

    // =======================
    // ロックAPI（PlayerController から使用）
    // =======================

    // 構え開始時に呼ぶ：現在候補が条件を満たせばロック確定し、以降はロック値で固定運用
    public bool TryLockTarget(float maxEnterDistance, float minClearance, float selfRadius)
    {
        // 再取得ブロック中ならロック不可
        if (Time.time < reacquireBlockUntil) return false;

        // 念のため最新化
        SelectNearestAndComputeRadius();
        if (currentTarget == null) return false;

        // 水平距離で判定
        Vector3 origin = owner ? owner.transform.position : transform.position;
        Vector3 a = new Vector3(origin.x, 0f, origin.z);
        Vector3 b = new Vector3(currentTarget.position.x, 0f, currentTarget.position.z);
        float distXZ = Vector3.Distance(a, b);
        if (distXZ > maxEnterDistance) return false; // 遠すぎる

        // クリアランス（自分の水平半径＋余白）より対象の安全半径が小さければNG
        float needRadius = Mathf.Max(0f, selfRadius) + Mathf.Max(0f, minClearance);
        if (currentSafeRadius < needRadius) return false;

        // ロック確定
        lockedTarget     = currentTarget;
        lockedDistanceXZ = distXZ;
        return true;
    }

    // ショット終端などで呼び、ロック解除＋一定時間は再取得をブロック
    public void ReleaseLock(float reacquireCooldownSeconds)
    {
        lockedTarget      = null;
        lockedDistanceXZ  = 0f;
        reacquireBlockUntil = Time.time + Mathf.Max(0f, reacquireCooldownSeconds);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()             // デバッグ可視化（任意）
    {
        Gizmos.color = Color.cyan;
        Vector3 origin = owner ? owner.transform.position : transform.position;
        Gizmos.DrawWireSphere(origin, 0.2f);
        if (currentTarget)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(currentTarget.position, currentSafeRadius);
        }
        if (lockedTarget)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(lockedTarget.position, lockedDistanceXZ);
        }
    }
#endif
}
