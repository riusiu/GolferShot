using UnityEngine;                                  // Unityの基本機能

/// <summary>
/// ステージ外（奈落）などに置くトリガー領域。
/// ・触れた物体にRespawnableがあれば、Destroy復活 or テレポート復帰を行う。
/// ・プレイヤーはテレポート、オブジェクトはDestroy復活…のように使い分け可能。
/// </summary>
[RequireComponent(typeof(Collider))]                 // 必ずColliderが必要
public class KillZone : MonoBehaviour
{
    [Header("動作モード")]
    public bool destroyAndRespawn = true;            // true=Destroy→復活 / false=テレポート復帰（Respawnable設定に従う）

    [Header("適用レイヤー")]
    public LayerMask targetLayers = ~0;              // このレイヤーに含まれる物だけ対象

    void Awake()
    {
        var col = GetComponent<Collider>(); // 自身のコライダー取得
        col.isTrigger = true;               // 触れたら発火したいのでTriggerに
    }

    void OnTriggerEnter(Collider other)
    {
        // ▼レイヤーフィルタ：対象外なら何もしない
        if (((1 << other.gameObject.layer) & targetLayers) == 0) return; // レイヤー不一致

        // ▼Rigidbodyが付いている一番上のTransformを取得（箱→親にRigidbodyがあるケース）
        Transform root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform; // ルート

        // ▼Respawnableが付いているか確認（付いていない物はノータッチ）
        var resp = root.GetComponent<Respawnable>(); // 復活設定の有無
        if (resp == null) return;                    // 無ければ何もしない（安全）

        if (destroyAndRespawn)                       // Destroy→復活させたい場合
        {
            // Destroy時のOnDestroyで自動予約されますが、「確実に予約してから消す」版で安全に
            resp.ScheduleRespawnThenDestroy();       // 予約してから破棄
        }
        else                                         // テレポート復帰（Destroyしない）
        {
            resp.TeleportRecoverNow();               // その場で復帰（プレイヤーに推奨）
        }
    }
}