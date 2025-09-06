using UnityEngine;                                  // Unityの基本機能

/// <summary>
/// 「Destroyされたら一定時間後に同じ場所に復活」させたい物に付けるコンポーネント。
/// ・プレハブ参照（何を復活させるか）と復活位置/回転を保持
/// ・OnDestroyでRespawnManagerへ依頼（リザルト等で止めたいときはマネージャ側のスイッチで制御）
/// ・Destroyせず“テレポートで復帰”も可能（プレイヤーのスコア維持用）
/// </summary>
public class Respawnable : MonoBehaviour
{
    [Header("復活対象")]
    public GameObject respawnPrefab;                // ★復活時に生成するプレハブ（必ず割り当ててください）

    [Header("復活地点")]
    public Transform spawnPointOverride;            // ★ここが設定されていればこの位置/回転で復活
    public bool useCurrentAsFallback = true;        // ★上が未設定ならAwake時の現在位置/回転を使う

    [Header("復活タイミング")]
    public bool respawnOnDestroy = true;            // ★Destroyされたら自動で復活を予約する
    public float respawnDelaySeconds = 2f;          // ★復活までの遅延秒数

    [Header("プレイヤー向け（Destroyせず復帰したい時）")]
    public bool allowTeleportRecover = true;        // ★KillZoneからの「テレポート復帰」を許可する

    // 内部保存：フォールバックの復活位置/回転
    private Vector3 _fallbackPos;                   // Awake時の位置を保存
    private Quaternion _fallbackRot;                // Awake時の回転を保存

    void Awake()
    {
        // ▼Awake時点の位置/回転をフォールバック用に覚えておく
        _fallbackPos = transform.position;          // 現在位置を保存
        _fallbackRot = transform.rotation;          // 現在回転を保存
    }

    void OnDestroy()
    {
        // ▼エディタ停止/シーン遷移時などでも呼ばれるため、「プレイ中のみ」かつ「復活許可時のみ」動作させる
        if (!Application.isPlaying) return;         // 再生中でなければ何もしない
        if (!respawnOnDestroy) return;              // Destroy時に復活しない設定なら終了
        if (respawnPrefab == null) return;          // プレハブ未設定なら終了

        // ▼復活位置を決定（優先：Override > Awake時の位置）
        Vector3 pos = spawnPointOverride ? spawnPointOverride.position : _fallbackPos; // 復活位置
        Quaternion rot = spawnPointOverride ? spawnPointOverride.rotation : _fallbackRot; // 復活回転

        // ▼RespawnManagerへ「○秒後に生成して」と依頼
        RespawnManager.Instance.ScheduleRespawn(respawnPrefab, pos, rot, respawnDelaySeconds); // 生成予約
    }

    /// <summary>
    /// Destroyを使わず「瞬時に元の場所へ戻す」ためのAPI（プレイヤー向け）
    /// </summary>
    public void TeleportRecoverNow()
    {
        if (!allowTeleportRecover) return;          // 許可されていなければ無視
        Vector3 pos = spawnPointOverride ? spawnPointOverride.position : _fallbackPos; // 位置決定
        Quaternion rot = spawnPointOverride ? spawnPointOverride.rotation : _fallbackRot; // 回転決定

        // ▼物理を安全に止めつつワープ（Rigidbodyがあれば速度をゼロ）
        var rb = GetComponent<Rigidbody>();         // 剛体取得
        if (rb)
        {
            rb.velocity = Vector3.zero;             // 速度リセット
            rb.angularVelocity = Vector3.zero;      // 回転速度リセット
        }
        transform.SetPositionAndRotation(pos, rot); // 瞬時に座標復帰
    }

    /// <summary>
    /// Destroy前に「復活を予約」だけしてから自分を消す補助（手動Destroy用）
    /// </summary>
    public void ScheduleRespawnThenDestroy(float? delayOverride = null)
    {
        if (respawnPrefab == null) { Destroy(gameObject); return; } // プレハブ無ければ普通に消す

        Vector3 pos = spawnPointOverride ? spawnPointOverride.position : _fallbackPos; // 復活位置
        Quaternion rot = spawnPointOverride ? spawnPointOverride.rotation : _fallbackRot; // 復活回転
        float delay = delayOverride.HasValue ? delayOverride.Value : respawnDelaySeconds; // 遅延

        RespawnManager.Instance.ScheduleRespawn(respawnPrefab, pos, rot, delay); // 先に予約
        Destroy(gameObject);                                 // それから自分を消す
    }
}
