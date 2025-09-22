using UnityEngine;                                  // Unityの基本機能
using System.Collections.Generic;                   // 辞書用

/// <summary>
/// ステージ外などに置く「落下処理」専用トリガー。
/// ・Destroy→Respawn は必ず「予約→Destroy」の順で安全に実行
/// ・TeleportRecoverNow() も選択可（Destroyしない）
/// ・同一オブジェクトの連続ヒットを短時間無視して二重実行を防止
/// </summary>
[RequireComponent(typeof(Collider))]                 // 必ずコライダーが必要
public class KillZone : MonoBehaviour
{
    public enum Mode { DestroyAndRespawn, TeleportRecover } // 動作モード

    [Header("動作モード")]
    public Mode mode = Mode.DestroyAndRespawn;       // 既定は Destroy→Respawn

    [Header("適用レイヤー")]
    public LayerMask targetLayers = ~0;              // 対象レイヤーのみ処理

    [Header("二重発火の抑制")]
    public float cooldownSeconds = 0.15f;            // 同じ物が短時間で再入したら無視

    // 内部：最後に処理した時刻（ID→時刻）
    private readonly Dictionary<int, float> _lastHitTime = new Dictionary<int, float>();

    void Awake()
    {
        var col = GetComponent<Collider>();          // 自分のコライダー
        col.isTrigger = true;                        // Trigger にする
    }

    void OnTriggerEnter(Collider other)
    {
        // レイヤーフィルタ
        if (((1 << other.gameObject.layer) & targetLayers) == 0) return;

        // ルートTransform（Rigidbody優先）
        Transform root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
        int id = root.GetInstanceID();               // 一意ID

        // 二重発火防止：クールダウン中は無視
        if (_lastHitTime.TryGetValue(id, out float last) && Time.time - last < cooldownSeconds) return;
        _lastHitTime[id] = Time.time;                // 今の時刻を記録

        // 対象のRespawnableを取得（無ければ何もしない）
        var resp = root.GetComponent<Respawnable>();
        if (resp == null) return;                    // 設定が無い物はスルー

        // モードに応じて処理
        if (mode == Mode.DestroyAndRespawn)
        {
            resp.ScheduleRespawnThenDestroy();       // 予約→Destroy（安全）
            return;                                   // Destroy後に触らない
        }
        else // TeleportRecover
        {
            resp.TeleportRecoverNow();               // その場で復帰（プレイヤーなどに）
        }
    }
}
