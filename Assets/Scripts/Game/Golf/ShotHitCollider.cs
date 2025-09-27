using UnityEngine;                                      // Unityの基本機能

public class ShotHitCollider : MonoBehaviour
{
    public PlayerController owner;                      // 打つプレイヤー（倍率・クラブ種を読む）

    [Header("Straight（直線）設定")]
    public float straightPower  = 15f;                  // 直線ショットの強さ
    public float straightUpward = 0.05f;                // 直線用の上向き補正（ほぼ0でもOK）

    [Header("Lofted（山なり）設定")]
    public float loftedPower    = 15f;                  // 山なりショットの強さ
    public float loftedUpward   = 0.3f;                 // 山なり用の上向き補正

    [Header("共通")]
    public float extraSpinDampen = 1f;                  // 発射前の回転をどのくらい抑えるか（=1で完全0に）
    public LayerMask hittableLayers = ~0;               // 当てたいレイヤー（AimOnlyは含めない）

    private void OnTriggerEnter(Collider other)
    {
        // レイヤーフィルタ：対象でなければ無視
        if (((1 << other.gameObject.layer) & hittableLayers) == 0) return;

        // Rigidbody取得（親でも拾える）
        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        // ===== 所有者マーキング（Hole用スコア） =====
        var own = rb.GetComponent<ShotOwnership>();                     // 打ち手記録
        if (own == null) own = rb.gameObject.AddComponent<ShotOwnership>();
        own.SetShooter(owner);                                          // 今回の打ち手を記録

        // ===== Kinematic管理：ショット直前にDynamic化する =====
        var autoKin = rb.GetComponent<AutoKinematicOnStop>();           // ★追加：自動Kinematic制御
        if (autoKin != null)                                            // スクリプトが付いていれば
        {
            autoKin.ActivateDynamicOnShot();                            // ★追加：Dynamicに切替＆監視開始
        }
        else                                                             // 付いていない場合の保険
        {
            rb.isKinematic = false;                                     // ★追加：とりあえずDynamic化
            rb.WakeUp();                                                // ★追加：スリープ解除
        }

        // ===== 速度と回転をリセット =====
        rb.velocity = Vector3.zero;                                     // 速度リセット
        rb.angularVelocity *= (1f - Mathf.Clamp01(extraSpinDampen));    // 回転も抑える（1なら完全停止）

        // ===== 方向ベクトル：プレイヤー左（水平）＋ 上向き補正 =====
        Vector3 leftDir = -(owner.transform.right);                     // 左 = -right
        leftDir.y = 0f;                                                 // 高さは別で付与するので水平成分だけ
        if (leftDir.sqrMagnitude < 0.0001f)                             // 万一ゼロなら
            leftDir = owner.transform.forward;                          // 保険で前方
        leftDir.Normalize();                                            // 正規化

        // ===== クラブ種類で分岐 =====
        bool isLofted = owner != null && owner.IsLofted();              // 現在の種類をプレイヤーから取得
        float basePower = isLofted ? loftedPower : straightPower;       // 基礎パワー
        float up        = isLofted ? loftedUpward : straightUpward;     // 上向き成分

        // ===== パワーゲージの倍率を掛ける =====
        float mul = owner ? owner.GetPowerMultiplier() : 1f;            // 0..1 → min..maxへ
        float finalPower = basePower * mul;                              // 最終パワー

        // ===== 最終ショット方向（水平左＋指定の高さ） =====
        Vector3 shotDir = (leftDir + Vector3.up * up).normalized;       // 山なり成分は別付与

        // ===== ボールに力を加える =====
        rb.AddForce(shotDir * finalPower, ForceMode.Impulse);            // インパルスで飛ばす

        // デバッグ可視化：ボール位置から飛ばす方向を表示
        Debug.DrawRay(other.transform.position, shotDir * finalPower, Color.red, 2f);
    }
}
