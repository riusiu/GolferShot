using UnityEngine;                                      // Unityの基本機能

/// <summary>
/// エイム用に「安全半径（周回距離）」と「周回中心」を提供する小物。
/// - 子オブジェクトに付けて使う想定（Aim用Triggerコライダーと同じ所）
/// - 最小構成：このオブジェクトに SphereCollider(Capsule/Boxでも可) を isTrigger=true で付ける
/// - explicitRadius を指定しなければ、Colliderの形状から水平半径を推定する
/// - orbitPivot を設定すれば、その位置を“周回の中心”として使う（未指定なら自身の中心）
/// </summary>
[DisallowMultipleComponent]                            // 複数付与は防止
public class AimTargetProxy : MonoBehaviour
{
    [Header("周回中心（未指定なら自身の位置）")]
    public Transform orbitPivot;                        // 周回の中心にしたいTransform（未指定なら this.transform）

    [Header("半径の指定（未指定ならColliderから推定）")]
    public float explicitRadius = -1f;                  // 0以上ならこの値をそのまま採用（ワールド単位）
    public float extraPadding  = 0.5f;                  // 推定時に足す余白（めり込み防止）

    [Header("ターゲットの“スコア/所有”などの参照先（未指定なら最も近いRigidbodyのroot）")]
    public Transform targetRootOverride;                // スコア用のタグ、所有権等を持つroot

    // ====== 公開アクセサ ======

    public Transform GetOrbitPivot()                    // 周回中心Transformを返す
    {
        return orbitPivot != null ? orbitPivot : this.transform;   // 未設定なら自分
    }

    public Transform GetTargetRoot()                    // スコアや所有情報を持つrootを返す
    {
        if (targetRootOverride != null) return targetRootOverride; // 明示指定があればそれ
        var rb = GetComponentInParent<Rigidbody>();                 // 近いRigidbodyを探す
        return rb ? rb.transform : transform.root;                  // 見つかればそのTransform、無ければ最上位
    }

    public float GetSafeRadius()                        // 安全半径（水平）を返す
    {
        // 1) 明示半径が有効ならそれを返す
        if (explicitRadius >= 0f) return explicitRadius;

        // 2) Collider形状から推定（Sphere/Capsule/Box の順に対応）
        float r = 0.5f;                                 // デフォルト最小
        var col = GetComponent<Collider>();             // 同じオブジェクト上のコライダー

        if (col is SphereCollider sph)                  // 球：スケールを考慮
        {
            // 世界スケールに応じて半径を補正（最大軸で拡大）
            float s = Mathf.Max(transform.lossyScale.x, Mathf.Max(transform.lossyScale.y, transform.lossyScale.z));
            r = sph.radius * s;
        }
        else if (col is CapsuleCollider cap)            // カプセル：水平半径（X/Z の最大）を採用
        {
            float sx = transform.lossyScale.x;
            float sz = transform.lossyScale.z;
            r = cap.radius * Mathf.Max(sx, sz);
        }
        else if (col is BoxCollider box)                // ボックス：水平投影の半径（X/Z の extents の最大）
        {
            // Box は回転で形状が崩れるので、「bounds」ではなく local size×lossyScale から水平最大を推定
            Vector3 half = Vector3.Scale(box.size * 0.5f, transform.lossyScale); // ローカルsize×スケールの半分
            r = Mathf.Max(half.x, half.z);             // 水平で最大の半径
        }
        else                                            // その他：bounds ベースにフォールバック
        {
            Bounds b = col ? col.bounds : new Bounds(transform.position, Vector3.one * 0.5f);
            r = Mathf.Max(b.extents.x, b.extents.z);
        }

        return Mathf.Max(0.01f, r + extraPadding);      // 余白を足して下限を確保
    }
}
