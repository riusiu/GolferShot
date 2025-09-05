using System.Collections.Generic;
using UnityEngine;

public class PlayerDataStore : MonoBehaviour
{
    public static PlayerDataStore Instance;

    // 複数プレイヤーの情報をここに保存
    public List<PlayerInfo> playerInfos = new List<PlayerInfo>();

    private void Awake()
    {
        // シングルトンの設定（重複禁止）
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject); // シーンをまたいでも消えない
    }
}