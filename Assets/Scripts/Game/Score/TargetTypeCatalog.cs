using UnityEngine;                           // Unityの基本機能

// 共通の「ターゲット定義リスト」を保持するScriptableObject
// 例：Ball, Pin, BunkerObject... などをタグで登録し、各タグにスコアや当たり判定余白を設定する
[CreateAssetMenu(menuName = "Golf/Target Type Catalog", fileName = "TargetTypeCatalog")]
public class TargetTypeCatalog : ScriptableObject
{
    // 各エントリ（タグごとの設定）
    [System.Serializable]
    public class Entryball
    {
        public string tag;                   // 対象のタグ名（例："Ball"）
        public int    score          = 0;    // 将来のスコア計算用（今は保持だけ）
        public float  padding        = 0.3f; // めり込み防止の余白（安全半径に加算する値）
        public bool   destroyOnScore = true; // ★追加：スコア後に消すかどうか
    }

    public Entryball[] entries;                  // 登録エントリの配列

    // 指定タグがリストに含まれるかどうか
    public bool Contains(string tag)
    {
        if (string.IsNullOrEmpty(tag) || entries == null) return false; // null安全
        foreach (var e in entries)                                      // 全件チェック
        {
            if (e != null && e.tag == tag) return true;                 // 一致したらOK
        }
        return false;                                                    // 見つからなければfalse
    }

    // 指定タグのエントリを取得（なければnull）
    public Entryball Get(string tag)
    {
        if (string.IsNullOrEmpty(tag) || entries == null) return null; // null安全
        foreach (var e in entries)                                     // 全件チェック
        {
            if (e != null && e.tag == tag) return e;                    // 一致したエントリを返す
        }
        return null;                                                     // なければnull
    }
}