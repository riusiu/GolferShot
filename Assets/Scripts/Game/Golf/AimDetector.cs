using System.Collections.Generic;                   // リスト/セット用
using UnityEngine;                                  // Unityの基本機能

// 常時ONの検出トリガー：範囲内にある「許可タグ（TargetTypeCatalog）」のオブジェクトを追跡し、
// 最も近いものと、その「安全半径（Colliderの水平サイズ由来＋余白）」を求める。
// 構え中（owner.IsAiming()==true）の間は、PlayerController に現在ターゲットと半径を毎フレーム通知する。
[RequireComponent(typeof(Collider))]                 // 必ずTriggerコライダーが必要
public class AimDetector : MonoBehaviour
{
    [Header("参照")]
    public PlayerController owner;                  // 検出結果を渡す相手（プレイヤー）
    public TargetTypeCatalog catalog;               // 共通ターゲット定義（Entry→Entryball に変更済み想定）

    [Header("設定")]
    public LayerMask targetMask = ~0;               // レイヤーフィルタ（必要なら絞る）
    public bool pushWhileAiming = true;             // 構え中は自動でownerへ結果をプッシュするか
    public float extraPadding = 0.5f;               // ★追加：安全半径に加える固定余白（めり込み防止）

    public float minOrbitRadius = 0.5f;             // 安全半径の下限（極端に小さい対象のため）
    public float maxOrbitRadius = 8f;               // 安全半径の上限（極端に大きい対象のため）

    // 内部状態
    private readonly HashSet<Collider> candidates = new HashSet<Collider>(); // いま範囲内の候補
    private Transform  currentTarget;               // 最も近い対象
    private float      currentSafeRadius;           // その対象の安全半径（めり込み防止用）

    // プロパティ（プレイヤー側から参照したい時に使える）
    public Transform CurrentTarget => currentTarget;        // 現在の対象（null可）
    public float    CurrentSafeRadius => currentSafeRadius; // 現在の安全半径

    private Collider triggerCol;                    // 自分のトリガー

    void Awake()
    {
        triggerCol = GetComponent<Collider>();      // コライダー取得
        triggerCol.isTrigger = true;                // 必ずTriggerにする
    }

    void Update()
    {
        // ★近傍の最良候補を選ぶ：毎フレーム計算（負荷が気になる場合は間引きOK）
        SelectNearestAndComputeRadius();

        // ★構え中だけ自動でプレイヤーに通知
        if (pushWhileAiming && owner != null && owner.IsAiming())
        {
            owner.SetCurrentAimTarget(currentTarget);              // 対象を渡す（null可）
            owner.SetCurrentAimOrbitRadius(currentSafeRadius);     // 安全半径を渡す
        }
    }

    private void OnTriggerEnter(Collider other)                    // 候補が入ってきた
    {
        if (!IsValidTarget(other)) return;                         // タグ/レイヤー不一致なら無視
        candidates.Add(other);                                     // 候補に追加
    }

    private void OnTriggerExit(Collider other)                     // 候補が出ていった
    {
        candidates.Remove(other);                                  // 候補から除外
        if (currentTarget != null && other.transform == currentTarget)
        {
            currentTarget = null;                                  // 現在対象だったら外す
        }
    }

    private bool IsValidTarget(Collider col)                       // タグとレイヤーの判定
    {
        // レイヤーマスク一致チェック
        if (((1 << col.gameObject.layer) & targetMask) == 0) return false;

        // カタログがなければ全部OKにするか、任意で弾く運用に切り替え可
        if (catalog == null) return true;

        // ★Entry→Entryballに変更されていても、Catalog側に Contains(tag) があればここはそのまま動作
        string tagName = col.gameObject.tag;
        return catalog.Contains(tagName);
    }

    private void SelectNearestAndComputeRadius()                   // 最寄りの対象と半径を更新
    {
        float bestSqr = float.PositiveInfinity;                    // 最短距離（自乗）初期化
        Transform bestT = null;                                    // ベストターゲット
        float bestRadius = minOrbitRadius;                         // ベスト半径

        Vector3 origin = owner ? owner.transform.position          // 基準位置：プレイヤー
                               : transform.position;               // いなければ自分

        foreach (var col in candidates)                            // 範囲内の候補を走査
        {
            if (col == null) continue;                             // 破棄済みはスキップ
            if (!IsValidTarget(col)) continue;                     // 動的に無効になる場合もある

            // 距離を計算（水平距離にしたい場合は y を合わせてから距離をとってもOK）
            Vector3 to = col.bounds.center - origin;               // 中心へのベクトル
            float sqr = to.sqrMagnitude;                           // 距離の二乗（高速）

            if (sqr < bestSqr)                                     // より近ければ更新
            {
                bestSqr = sqr;                                     // 最短距離更新
                bestT   = col.transform;                           // ターゲット更新
                bestRadius = ComputeSafeRadius(col);               // 半径を計算
            }
        }

        currentTarget = bestT;                                     // 結果を保存
        currentSafeRadius = Mathf.Clamp(bestRadius,                // 半径を下限/上限で制限
                                        minOrbitRadius, maxOrbitRadius);
    }

    private float ComputeSafeRadius(Collider col)                  // ★安全半径：Colliderから自動算出＋余白
    {
        // bounds.extents はワールド空間での半サイズ。水平面(XZ)で大きい方を半径ベースに採用
        Bounds b = col.bounds;
        Vector3 e = b.extents;
        float baseRadius = Mathf.Max(e.x, e.z);                    // 水平最大半径

        // 形状によるバラつきへの簡易対応：
        // Sphere/Capsule/Box/Mesh いずれでも bounds で安全側に取れる（少し大きめ）
        // そこに固定余白（extraPadding）を足して、めり込みをさらに防ぐ
        return baseRadius + Mathf.Max(0f, extraPadding);
    }
}
