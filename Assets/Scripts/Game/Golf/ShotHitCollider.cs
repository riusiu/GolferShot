using UnityEngine;                                  // Unityの基本機能

/// <summary>
/// インパクト時だけ有効化されるショット判定。
/// ・当たったRigidbodyを「プレイヤーの左方向」に飛ばす（カメラは使わない）
/// ・Lofted / Straight を PlayerController から取得
/// ・誰が打ったか（所有者）を ShotOwnership に記録
/// ・タグの許可は TargetTypeCatalog（またはフォールバック配列）で判定
/// </summary>
public class ShotHitCollider : MonoBehaviour
{
    [Header("参照")]
    public PlayerController owner;                   // 打つプレイヤー（Lofted判定＆所有者記録に使う）
    public TargetTypeCatalog catalog;                // 許可タグのカタログ（Entryball）※任意
    public string[] allowedTagsFallback;             // カタログが無い時用の許可タグ一覧 ※任意（空/未設定なら全許可）

    [Header("Straight（直線）設定")]
    public float straightPower  = 15f;               // 直線ショットの強さ
    public float straightUpward = 0.05f;             // 直線用の上向き補正（ほぼ0でもOK）

    [Header("Lofted（山なり）設定")]
    public float loftedPower    = 15f;               // 山なりショットの強さ
    public float loftedUpward   = 0.3f;              // 山なり用の上向き補正

    [Header("共通")]
    public float extraSpinDampen = 1f;               // 発射前の回転をどのくらい抑えるか（=1で完全0に）

    private void OnTriggerEnter(Collider other)      // コライダーが触れた瞬間（インパクト時だけ有効化される想定）
    {
        // 1) 許可タグのチェック（カタログ or フォールバック。どちらも未設定なら全許可）
        if (!IsAllowed(other.tag)) return;           // 許可されていなければスキップ

        // 2) Rigidbody を取得（親についている場合にも対応）
        Rigidbody rb = other.attachedRigidbody;      // 親階層にRigidbodyがあっても拾える
        if (rb == null) return;                      // 無ければ飛ばせないので終了

        // 3) 発射方向（プレイヤーの左）を算出
        //    ・左 = -right。高さは別途Upwardで付与するので水平成分に限定
        Vector3 left = owner ? -owner.transform.right : -transform.right; // プレイヤー基準の左（保険で自分の左）
        left.y = 0f;                                   // 高さ成分を消す（水平のみ）
        if (left.sqrMagnitude < 0.0001f)               // 万一ゼロになったときの保険
            left = Vector3.left;                       // ワールド左で代用
        left.Normalize();                               // 長さ1に正規化

        // 4) ショット種別（Lofted / Straight）でパワー・上向き補正を決定
        bool isLofted = owner != null && owner.IsLofted(); // 現在Loftedか？
        float power   = isLofted ? loftedPower  : straightPower;   // パワー
        float up      = isLofted ? loftedUpward : straightUpward;  // 上向き補正

        // 5) 事前状態のリセット（回転・速度）
        rb.velocity = Vector3.zero;                   // 直前の移動速度をリセット
        rb.angularVelocity *= (1f - Mathf.Clamp01(extraSpinDampen)); // 回転も抑える（1なら完全停止）

        // 6) 最終ショット方向 = 「左の水平」 + 「上向き補正」
        Vector3 shotDir = left + Vector3.up * up;     // 合成
        shotDir.Normalize();                          // 正規化

        // 7) 所有者マーキング（HoleGoalで“誰に点を入れるか”に使う）
        ShotOwnership own = rb.GetComponent<ShotOwnership>(); // 既に付いているか確認
        if (own == null) own = rb.gameObject.AddComponent<ShotOwnership>(); // 無ければ付与
        own.SetShooter(owner);                        // 「最後に打ったプレイヤー」を記録

        // 8) インパルスを付与して発射！
        rb.AddForce(shotDir * power, ForceMode.Impulse); // 力を一気に加える（瞬発）

        // 9) デバッグ（向き確認用の赤いライン）
        Debug.Log($"[ShotHitCollider] Shoot LEFT / Target={rb.name} / Mode={(isLofted ? "Lofted" : "Straight")} / Power={power} / Up={up}");
        Debug.DrawRay(other.transform.position, shotDir * power, Color.red, 1.5f); // 可視化
    }

    // ===== 許可タグの判定ユーティリティ =====
    private bool IsAllowed(string tagName)           // 当たり対象が許可タグかどうか
    {
        // カタログがあればそちらを優先
        if (catalog != null) return catalog.Contains(tagName); // Entryballの一覧に載っていればOK

        // フォールバック配列が設定されていれば、その中に含まれているか
        if (allowedTagsFallback != null && allowedTagsFallback.Length > 0)
        {
            for (int i = 0; i < allowedTagsFallback.Length; i++)
                if (allowedTagsFallback[i] == tagName) return true;
            return false;
        }

        // どちらも未設定なら全許可（制限しない）
        return true;
    }
}
