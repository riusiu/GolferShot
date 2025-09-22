using UnityEngine;                // Unityの基本機能
using System.Collections.Generic; // リスト用

/// <summary>
/// あらかじめ用意した複数のスポーン地点（Transform）からランダムに1つ選ぶだけの簡単クラス。
/// ・Physics.CheckSphere で空きチェック（軽い衝突防止）
/// </summary>
public class RandomSpawnPoints : MonoBehaviour
{
    [Header("スポーン地点（任意個）")]
    public List<Transform> points = new List<Transform>();      // ここにPointを登録

    [Header("衝突チェック")]
    public LayerMask obstacleMask = ~0;                         // ここに当たるとNG（床を除外したい場合は設定）

    public bool HasPoints() => points != null && points.Count > 0; // 1つ以上あるか

    public bool TryGetRandomPose(out Pose pose, float clearanceRadius = 0.25f) // 位置と回転を返す
    {
        pose = default;                 // 失敗時用に初期化
        if (!HasPoints()) return false; // そもそも無ければ終了

        int tries = Mathf.Min(points.Count, 8); // 最大8回まで試す
        for (int i = 0; i < tries; i++)         // ランダムにいくつか試行
        {
            var t = points[Random.Range(0, points.Count)]; // ランダムに1つ
            if (t == null) continue;                       // nullは無視

            Vector3    pos = t.position; // 候補位置
            Quaternion rot = t.rotation; // 候補回転

            bool blocked = Physics.CheckSphere(                  // 周囲の空き判定（簡易）
                pos, clearanceRadius, obstacleMask, QueryTriggerInteraction.Ignore);
            if (!blocked)                                        // 当たっていなければ
            {
                pose = new Pose(pos, rot); // 採用
                return true;               // 成功
            }
        }
        return false;                                            // 全滅なら失敗
    }
}