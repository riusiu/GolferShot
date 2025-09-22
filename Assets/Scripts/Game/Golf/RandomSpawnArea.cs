using UnityEngine;                                  // Unityの基本機能

/// <summary>
/// Box または Sphere の体積からランダムに点をサンプルしてスポーン位置を決める。
/// ・BoxCollider / SphereCollider を "isTrigger=true" でこのオブジェクトに付けてください
/// ・床へレイキャストして地面に置く（オプション）
/// ・空き判定(Physics.CheckSphere)でめり込み防止
/// </summary>
[RequireComponent(typeof(Collider))]
public class RandomSpawnArea : MonoBehaviour
{
    public enum Shape { Box, Sphere }
    [Header("形状")]
    public Shape shape = Shape.Box;                             // 形
    public bool alignToGround = true;                           // 足元に落とす（下方向Ray）
    public float groundRayDistance = 10f;                       // 探索距離
    public LayerMask groundMask = ~0;                           // 地面レイヤー

    [Header("衝突回避")]
    public LayerMask obstacleMask = ~0;                         // ここに当たるとNG
    public Vector3 upOffsetOnGround = Vector3.up * 0.05f;      // 置くときの微小浮かせ

    private Collider _col;

    void Awake()
    {
        _col = GetComponent<Collider>();
        _col.isTrigger = true;                                  // 領域として使う
    }

    public bool TrySample(out Pose pose, float clearanceRadius, int tries = 12)
    {
        pose = default;
        if (_col == null) return false;

        for (int i = 0; i < tries; i++)
        {
            Vector3 local = (shape == Shape.Box)
                ? RandomPointInBox()
                : RandomPointInSphere();

            Vector3 world = transform.TransformPoint(local);
            Quaternion rot = transform.rotation;

            // クリアランス
            bool blocked = Physics.CheckSphere(world, clearanceRadius, obstacleMask, QueryTriggerInteraction.Ignore);
            if (blocked) continue;

            // 地面に合わせる
            if (alignToGround)
            {
                Vector3 rayStart = world + Vector3.up * groundRayDistance * 0.5f;
                if (Physics.Raycast(rayStart, Vector3.down, out var hit, groundRayDistance, groundMask, QueryTriggerInteraction.Ignore))
                {
                    world = hit.point + upOffsetOnGround;
                    rot = Quaternion.LookRotation(transform.forward, hit.normal); // 上方向を地面法線に（任意）
                }
            }

            pose = new Pose(world, rot);
            return true;
        }
        return false;
    }

    private Vector3 RandomPointInBox()
    {
        // BoxCollider の bounds ではなくローカル軸の size を使う（回転対応）
        if (_col is BoxCollider box)
        {
            Vector3 half = box.size * 0.5f;
            return new Vector3(
                Random.Range(-half.x, half.x),
                Random.Range(-half.y, half.y),
                Random.Range(-half.z, half.z)
            ) + box.center;
        }
        // 想定外は原点
        return Vector3.zero;
    }

    private Vector3 RandomPointInSphere()
    {
        if (_col is SphereCollider sph)
        {
            // 球内一様サンプル（半径^3の立方根で距離を作る）
            Vector3 dir = Random.onUnitSphere;
            float r = Mathf.Pow(Random.value, 1f / 3f) * sph.radius;
            return dir * r + sph.center;
        }
        return Vector3.zero;
    }
}
