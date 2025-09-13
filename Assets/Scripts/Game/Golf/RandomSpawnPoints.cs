using UnityEngine;                // Unityの基本機能
using System.Collections.Generic; // リスト用

/// <summary>
/// 任意個のスポーン地点（Transform）からランダムに1つ選ぶ。
/// ・各Transformの位置/回転をそのまま使う
/// ・clearanceRadius で空きがあるか（Physics.CheckSphere）簡易チェック可能
/// </summary>
public class RandomSpawnPoints : MonoBehaviour
{
    [Header("スポーン地点")]
    public List<Transform> points = new List<Transform>();    // ここに好きなだけ登録

    [Header("レイヤー衝突判定")]
    public LayerMask obstacleMask = ~0;                       // ここに当たるとNG（床レイヤーを除外しても可）

    public bool HasPoints() => points != null && points.Count > 0;

    public bool TryGetRandomPose(out Pose pose, float clearanceRadius = 0.25f)
    {
        pose = default;
        if (!HasPoints()) return false;

        // シャッフル風に数回試行
        int tries = Mathf.Min(points.Count, 8);
        for (int i = 0; i < tries; i++)
        {
            var t = points[Random.Range(0, points.Count)];
            if (t == null) continue;

            Vector3    pos = t.position;
            Quaternion rot = t.rotation;

            // クリアランス：真上から球判定（床を除外したければ obstacleMask を調整）
            bool blocked = Physics.CheckSphere(pos, clearanceRadius, obstacleMask, QueryTriggerInteraction.Ignore);
            if (!blocked)
            {
                pose = new Pose(pos, rot);
                return true;
            }
        }
        // ダメだった
        return false;
    }
}