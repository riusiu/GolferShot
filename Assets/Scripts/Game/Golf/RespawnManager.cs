using System.Collections;
using UnityEngine;                                  // Unityの基本機能

/// <summary>
/// 復活（Respawn）を司るマネージャ。シーンに1つあればOK。
/// ・Instantiateをまとめて実行
/// ・「今は復活させない」グローバルスイッチも持つ（例：リザルト遷移中）
/// </summary>
public class RespawnManager : MonoBehaviour
{
    // ==== シングルトン化（どこからでも使える & シーンを跨いで残す） ====
    private static RespawnManager _instance; // 唯一のインスタンス参照
    public static RespawnManager Instance    // 外部からの入口
    {
        get
        {
            if (_instance == null)                  // なければ
            {
                var go = new GameObject("[RespawnManager]");   // 管理用オブジェクトを作成
                _instance = go.AddComponent<RespawnManager>(); // 自分を付ける
                DontDestroyOnLoad(go);                         // シーン切り替えでも残す
            }
            return _instance;                       // 参照を返す
        }
    }

    [Header("グローバル設定")]
    public bool globallyEnabled = true;             // true=復活許可 / false=全復活を一時停止

    /// <summary>
    /// 外部から復活のON/OFFを切り替える（例：リザルトへ行く直前にOFFにする）
    /// </summary>
    public void SetGloballyEnabled(bool enabled)    // グローバルスイッチ
    {
        globallyEnabled = enabled;                  // 値を反映
    }

    /// <summary>
    /// 指定プレハブをdelay秒後にspawnPos/spawnRotで生成する
    /// </summary>
    public void ScheduleRespawn(GameObject prefab, Vector3 spawnPos, Quaternion spawnRot, float delaySeconds)
    {
        if (prefab == null) return;                                               // プレハブが無ければ何もしない
        StartCoroutine(RespawnRoutine(prefab, spawnPos, spawnRot, delaySeconds)); // コルーチンで待機→生成
    }

    // 内部：待ってから生成するだけの処理
    private IEnumerator RespawnRoutine(GameObject prefab, Vector3 pos, Quaternion rot, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay); // 指定秒数待つ
        if (!globallyEnabled) yield break;                      // その間にグローバルOFFなら復活しない
        Instantiate(prefab, pos, rot);                          // 生成（親が必要なら第4引数にTransformを渡す実装に拡張可）
    }
}