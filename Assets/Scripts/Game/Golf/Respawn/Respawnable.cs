using UnityEngine;                                  // Unityの基本機能

/// <summary>
/// Destroy（または明示呼び出し）で「安全に復活」させるためのコンポーネント。
/// - respawnPrefab は Project上のPrefabに限定（シーン実体を弾く）
/// - Editor で自動補完（Prefabインスタンスなら、自分の元Prefabを自動セット）
/// - ランダムスポーン：ポイント群 or エリア（Box/Sphere）から復活座標をサンプリング
/// - Destroy後の参照アクセス事故を防ぐため、予約→即return が基本
/// </summary>
public class Respawnable : MonoBehaviour
{
    [Header("復活対象（Project上のPrefabを指定）")]
    public GameObject respawnPrefab;                // ★ProjectのPrefabを割り当て（シーン実体は不可）

    [Header("復活遅延")]
    public bool respawnOnDestroy = true;            // Destroy時に自動復活するか
    public float respawnDelaySeconds = 2f;          // 復活までの遅延

    [Header("固定スポーン（指定があれば最優先）")]
    public Transform spawnPointOverride;            // 指定時はここに復活
    public bool useCurrentAsFallback = true;        // 指定なし時：Awakeの位置/回転を使う

    [Header("ランダムスポーン設定")]
    public RandomSpawnPoints pointsGroup;           // 複数ポイントからランダム
    public RandomSpawnArea   area;                  // エリア（Box/Sphere）からランダム
    public float clearanceRadius = 0.25f;           // 近接クリアランス（衝突しない半径）
    public int sampleTries = 12;                    // サンプリング試行回数（失敗時はフォールバック）

    [Header("テレポート復帰（プレイヤー用）")]
    public bool allowTeleportRecover = true;        // Destroyせず戻す選択肢

    // 内部：フォールバック座標
    private Vector3 _fallbackPos;
    private Quaternion _fallbackRot;

    void Awake()
    {
        _fallbackPos = transform.position;          // 起動時の位置を保存
        _fallbackRot = transform.rotation;          // 起動時の回転を保存
    }

    void OnDestroy()
    {
        if (!Application.isPlaying) return;         // 再生外では無視
        if (!respawnOnDestroy) return;              // 自動復活しない設定
        if (respawnPrefab == null) return;          // そもそも未設定

        // ★安全：シーン実体が紛れないようガード
        if (respawnPrefab.scene.IsValid())
        {
            Debug.LogError($"[Respawnable] respawnPrefab にシーン実体が割り当てられています。ProjectのPrefabを指定してください。({respawnPrefab.name})", this);
            return;
        }

        // 復活座標を決定（固定 → ランダムポイント → ランダムエリア → フォールバック）
        Pose pose = GetRespawnPose();

        // ★予約して終わり（Destroy後に自分や関連参照を触らない）
        RespawnManager.Instance.ScheduleRespawn(respawnPrefab, pose.position, pose.rotation, respawnDelaySeconds);
    }

    /// <summary>Destroyせず瞬時に元へ戻す（プレイヤー向け）</summary>
    public void TeleportRecoverNow()
    {
        if (!allowTeleportRecover) return;
        Pose p = GetRespawnPose();
        var rb = GetComponent<Rigidbody>();
        if (rb) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        transform.SetPositionAndRotation(p.position, p.rotation);
    }

    /// <summary>安全版：先に予約→それからDestroy。以降は参照を使わない。</summary>
    public void ScheduleRespawnThenDestroy(float? delayOverride = null)
    {
        if (respawnPrefab != null && !respawnPrefab.scene.IsValid())
        {
            Pose p = GetRespawnPose();
            float d = delayOverride.HasValue ? delayOverride.Value : respawnDelaySeconds;
            RespawnManager.Instance.ScheduleRespawn(respawnPrefab, p.position, p.rotation, d);
        }
        Destroy(gameObject);                         // 破棄（以降、この参照を触らない）
    }

    // ===== ここからユーティリティ =====

    private Pose GetRespawnPose()
    {
        // 1) 固定スポーンがあれば最優先
        if (spawnPointOverride != null)
            return new Pose(spawnPointOverride.position, spawnPointOverride.rotation);

        // 2) ランダムポイント群
        if (pointsGroup != null && pointsGroup.HasPoints())
        {
            if (pointsGroup.TryGetRandomPose(out var p, clearanceRadius))
                return p;
        }

        // 3) エリアからサンプル
        if (area != null)
        {
            if (area.TrySample(out var p, clearanceRadius, sampleTries))
                return p;
        }

        // 4) フォールバック（Awake時）
        if (useCurrentAsFallback)
            return new Pose(_fallbackPos, _fallbackRot);

        // 5) 最終手段：現状
        return new Pose(transform.position, transform.rotation);
    }

#if UNITY_EDITOR
    // ★Editorだけ：設定の自動補完とガード
    void OnValidate()
    {
        // シーン実体ガード
        if (respawnPrefab != null && respawnPrefab.scene.IsValid())
        {
            Debug.LogError($"[Respawnable] respawnPrefab にシーン実体が割り当てられています。Project の Prefab を指定してください。({respawnPrefab.name})", this);
            respawnPrefab = null;
        }

        // 自動補完：Prefabインスタンスなら「元Prefab」を自動割当
        if (respawnPrefab == null)
        {
            var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject);
            if (prefab != null && !prefab.scene.IsValid())
            {
                respawnPrefab = prefab;                                 // 元Prefabをセット
                UnityEditor.EditorUtility.SetDirty(this);               // 変更を保存
            }
        }
    }
#endif
}
