using UnityEngine;                                  // Unityの基本機能

/// <summary>
/// Destroy されたら安全にリスポーンさせるための最小コンポーネント（ポイント方式専用）。
/// ・respawnPrefab は Project の Prefab 必須（シーン実体NG）
/// ・spawnPointOverride > pointsGroup > Awake位置 の順で復帰地点を決定
/// ・ScheduleRespawnThenDestroy() を使うと「予約→Destroy」で安全
/// </summary>
public class Respawnable : MonoBehaviour
{
    [Header("復活対象（ProjectのPrefab）")]
    public GameObject respawnPrefab;                // 復活時に生成するPrefab（必ず Project 上のPrefabを割当）

    [Header("復活遅延")]
    public bool  respawnOnDestroy     = true;       // Destroy時に自動復活するか
    public float respawnDelaySeconds  = 1.0f;       // 復活までの待機秒

    [Header("スポーンポイント（優先: Override → Group）")]
    public Transform          spawnPointOverride;   // ここがあれば必ずここに出す
    public RandomSpawnPoints  pointsGroup;          // 複数候補からランダム（物理的な空き判定付き）

    [Header("フォールバック")]
    public bool useCurrentAsFallback = true;        // どちらも無ければ Awake の座標に戻る

    // 内部：Awake 時点の座標を保存（フォールバック用）
    private Vector3   _fallbackPos;                 // 位置
    private Quaternion _fallbackRot;                // 回転

    void Awake()                                     // 起動時に1回だけ呼ばれる
    {
        _fallbackPos = transform.position;           // 位置を保存
        _fallbackRot = transform.rotation;           // 回転を保存
    }

    void OnDestroy()                                  // このオブジェクトが破棄された時
    {
        if (!Application.isPlaying) return;          // 再生外は無視
        if (!respawnOnDestroy) return;               // 自動復活しない設定なら何もしない
        if (respawnPrefab == null) return;           // Prefab 未設定なら復活不能

        // ★安全：respawnPrefab がシーン実体なら中止（Editorで気づけるようログ）
        if (respawnPrefab.scene.IsValid())           
        {
            Debug.LogError($"[Respawnable] respawnPrefab にシーン実体が割り当てられています。Project の Prefab を指定してください。({respawnPrefab.name})", this);
            return;                                   // 予約しない
        }

        // 復活座標を決定
        Pose pose = GetRespawnPose();                 // 位置と回転のペア

        // 復活を予約（遅延後に Instantiate）。Destroy 後に自分へ触らないのが鉄則。
        RespawnManager.Instance.ScheduleRespawn(respawnPrefab, pose.position, pose.rotation, respawnDelaySeconds);
    }

    /// <summary>Destroy せず元の地点へ瞬時に戻す（プレイヤー用の保険）。</summary>
    public void TeleportRecoverNow()
    {
        Pose p = GetRespawnPose();                    // 復帰先を決定
        var rb = GetComponent<Rigidbody>();           // 物理があれば速度を止める
        if (rb) { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
        transform.SetPositionAndRotation(p.position, p.rotation); // 位置・回転を即反映
    }

    /// <summary>安全版：先に復活予約→そのあと自分をDestroy。</summary>
    public void ScheduleRespawnThenDestroy(float? delayOverride = null)
    {
        if (respawnPrefab != null && !respawnPrefab.scene.IsValid()) // Project Prefab だけ許可
        {
            Pose p = GetRespawnPose();               // 復活座標
            float delay = delayOverride ?? respawnDelaySeconds; // 遅延秒
            RespawnManager.Instance.ScheduleRespawn(respawnPrefab, p.position, p.rotation, delay); // 予約
        }
        Destroy(gameObject);                         // それから安全に破棄（以降、この参照を触らない）
    }

    // ===== 復活座標の決定ロジック（簡潔） =====
    private Pose GetRespawnPose()
    {
        if (spawnPointOverride != null)               // 1) 明示のポイント
            return new Pose(spawnPointOverride.position, spawnPointOverride.rotation);

        if (pointsGroup != null && pointsGroup.TryGetRandomPose(out var p, 0.25f)) // 2) ポイント群からランダム
            return p;

        if (useCurrentAsFallback)                     // 3) フォールバック（Awake時）
            return new Pose(_fallbackPos, _fallbackRot);

        return new Pose(transform.position, transform.rotation); // 4) 最終手段：現状
    }

#if UNITY_EDITOR
    void OnValidate()                                  // エディタで値変更時に呼ばれる
    {
        // シーン実体が入ったら無効化して注意喚起
        if (respawnPrefab != null && respawnPrefab.scene.IsValid())
        {
            Debug.LogError($"[Respawnable] respawnPrefab には必ず Project の Prefab を割り当ててください。({respawnPrefab.name})", this);
            respawnPrefab = null;                     // 消して事故防止
        }

        // Prefab インスタンスに貼った場合は「元Prefab」を自動で補完
#if UNITY_EDITOR
        if (respawnPrefab == null)
        {
            var prefab = UnityEditor.PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject);
            if (prefab != null && !prefab.scene.IsValid())
            {
                respawnPrefab = prefab;               // 元Prefabを自動設定
                UnityEditor.EditorUtility.SetDirty(this); // 変更を保存
            }
        }
#endif
    }
#endif
}
