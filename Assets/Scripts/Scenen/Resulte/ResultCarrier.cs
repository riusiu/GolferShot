using System.Collections.Generic;                   // リスト用
using UnityEngine;                                  // Unityの基本機能
using UnityEngine.SceneManagement;                  // シーン切り替え

/// <summary>
/// ゲームシーンから「名前・スコア・表彰台用プレハブ」を集めて、
/// リザルトシーンへ運ぶための常駐キャリア（DontDestroyOnLoad）。
/// </summary>
public class ResultCarrier : MonoBehaviour
{
    // ===== シングルトン（1個だけ置く） =====
    private static ResultCarrier _instance;                          // 唯一のインスタンス参照
    public static ResultCarrier Instance                             // 外部からはここ経由でアクセス
    {
        get
        {
            if (_instance == null)                                   // まだ無ければ
            {
                var go = new GameObject("[ResultCarrier]");          // 空オブジェクト生成
                _instance = go.AddComponent<ResultCarrier>();        // このコンポーネントを付与
                DontDestroyOnLoad(go);                               // シーン切り替えでも残す
            }
            return _instance;                                        // 参照を返す
        }
    }

    // ===== リザルトに持っていく1人分のデータ =====
    [System.Serializable]                                            // インスペクタ表示用
    public class Entry
    {
        public string displayName;                                   // プレイヤー名など表示名
        public int    score;                                         // スコア
        public GameObject actorPrefab;                               // 表彰台で使うプレハブ（見た目）
    }

    public List<Entry> entries = new List<Entry>();                  // 全参加者の一覧

    /// <summary>
    /// 現在のシーンから PlayerController を集め、結果データを作る。
    /// </summary>
    public void CaptureFromCurrentScene()
    {
        entries.Clear();                                             // まず空にする

        var players = Object.FindObjectsOfType<PlayerController>();  // 全プレイヤーを探す
        foreach (var p in players)                                   // 1人ずつ
        {
            if (p == null) continue;                                 // 念のためnullガード

            // 表彰台用プレハブの提供者を同じオブジェクトから探す
            var provider = p.GetComponent<ResultActorProvider>();    // Provider取得
            GameObject prefab = provider ? provider.resultActorPrefab : null; // あれば使う

            // 1人分のEntryを作成
            var e = new Entry
            {
                displayName = p.name,                                // 名前はとりあえずオブジェクト名
                score       = p.score,                               // PlayerControllerのscoreを使用
                actorPrefab = prefab                                 // リザルト用見た目
            };
            entries.Add(e);                                          // リストに追加
        }
    }

    /// <summary>
    /// 収集してリザルトシーンに切り替えるユーティリティ。
    /// </summary>
    public void CaptureAndLoadResultScene(string sceneName)
    {
        CaptureFromCurrentScene();                                   // まず集める
        SceneManager.LoadScene(sceneName);                           // そのままロード
    }
}
