using UnityEngine;  // Unityの基本機能

public class ShotHitCollider : MonoBehaviour
{
    [Header("参照")]
    public Transform cameraDirection;    // カメラの向き参照（★備考：本バージョンでは未使用でもOK）
    public PlayerController owner;       // 打つプレイヤー（クラブ種類を読むため）

    [Header("ターゲット判定（タグリスト）")]                                  // ★追加：許可タグの管理（前回のまま）
    public TargetTypeCatalog catalog;     // 共通のタグリスト（ScriptableObject / Entryball配列想定）
    public string[] allowedTagsFallback;  // カタログ未設定時のフォールバック（空なら "Ball" のみ可）

    [Header("Straight（直線）設定")]
    public float straightPower  = 15f;   // 直線ショットの強さ
    public float straightUpward = 0.05f; // 直線用の上向き補正（ほぼ0でもOK）

    [Header("Lofted（山なり）設定")]
    public float loftedPower    = 15f;   // 山なりショットの強さ
    public float loftedUpward   = 0.3f;  // 山なり用の上向き補正

    [Header("共通")]
    public float extraSpinDampen = 1f;   // 発射前の回転をどのくらい抑えるか（=1で完全0に）

    [Header("方向の基準")]
    // public bool usePlayerForwardFirst = true; // ★変更：前方優先は使わない
    [Tooltip("プレイヤーの“左側（-right）”を常に基準にします。カメラには依存しません。")] // ★追加
    public bool shootToPlayerLeft = true; // ★追加：左固定（true推奨）

    [Header("安全策（任意）")]
    public bool onlyFirstHitPerActivation = true; // このコライダー有効化から最初の1回だけ有効
    private bool _hitLockedThisActivation = false;

    private void OnEnable()
    {
        _hitLockedThisActivation = false;                  // 有効化のたびに解除
    }

    private void OnTriggerEnter(Collider other)
    {
        // ★変更：固定の"Ball"タグではなく、Catalog/フォールバックで判定
        if (!IsAllowedTarget(other.gameObject)) return;

        if (onlyFirstHitPerActivation && _hitLockedThisActivation) return;
        _hitLockedThisActivation = true;

        Rigidbody rb = other.attachedRigidbody;           // Rigidbody取得（親でも拾える）
        if (rb == null) return;

        // ====== 方向ベクトルを計算 ======
        Vector3 forward;

        // ★変更：ショット方向は“プレイヤーの左側（-owner.transform.right）”を基準に固定
        //        カメラの向きには依存しません（ブレ対策）
        if (shootToPlayerLeft && owner != null)
        {
            forward = -owner.transform.right;             // ★変更：プレイヤー左を“前”として採用
        }
        else if (owner != null)
        {
            forward = owner.transform.forward;            // 保険：左固定が無効なら前方
        }
        else
        {
            // オーナー不明時の保険（このコンポーネントの左）
            forward = -transform.right;
        }

        forward.y = 0f;                                   // 高さは別で付与するので水平成分だけ使う
        if (forward.sqrMagnitude < 0.0001f)               // 万一ゼロなら
            forward = (owner ? owner.transform.forward : transform.forward); // 保険
        forward.Normalize();                              // 正規化（長さ1に）

        // ====== クラブ種類で分岐（PlayerControllerのアクセサを利用） ======
        bool isLofted = owner != null && owner.IsLofted(); // 現在の種類をプレイヤーから取得
        float power   = isLofted ? loftedPower  : straightPower;
        float up      = isLofted ? loftedUpward : straightUpward;

        // ====== 速度と回転をリセット ======
        rb.velocity = Vector3.zero;                                        // 速度リセット
        rb.angularVelocity *= (1f - Mathf.Clamp01(extraSpinDampen));       // 回転も抑える（1なら完全停止）

        // ====== 最終ショット方向（“左”＋指定の高さ） ======
        Vector3 shotDir = forward + Vector3.up * up;                       // 上成分は別付与
        shotDir.Normalize();

        // ====== ボールに力を加える ======
        rb.AddForce(shotDir * power, ForceMode.Impulse);                   // インパルスで飛ばす

        Debug.Log($"発射: {other.name} (tag={other.tag}) / Dir=LEFT_OF_PLAYER / Mode={(isLofted ? "Lofted" : "Straight")} / Power={power} / Up={up}");

        // デバッグ可視化：対象位置から飛ばす方向を表示
        Debug.DrawRay(other.transform.position, shotDir * power, Color.red, 2f);
    }

    // ★追加：タグ判定の共通関数（Catalog優先 → フォールバック配列 → 最後に"Ball"）
    private bool IsAllowedTarget(GameObject go)
    {
        if (go == null) return false;
        string tagName = go.tag;

        if (catalog != null)
        {
            return catalog.Contains(tagName);             // Catalogに載っていればOK（Entry→Entryballどちらでも）
        }

        if (allowedTagsFallback != null && allowedTagsFallback.Length > 0)
        {
            for (int i = 0; i < allowedTagsFallback.Length; i++)
            {
                if (allowedTagsFallback[i] == tagName) return true;
            }
            return false;
        }

        return go.CompareTag("Ball");                     // 何も設定がなければ旧仕様
    }
}
