using UnityEngine;

public class InGameSpawnStarter : MonoBehaviour
{
    [SerializeField] private InGameSpawner spawner;

    private void Start()
    {
        Debug.Log("🌟 InGameSpawnStarter: Start() 実行");

        if (spawner == null)
        {
            Debug.LogError("❌ spawner が設定されていません！");
            return;
        }

        if (PlayerDataStore.Instance == null)
        {
            Debug.LogError("❌ PlayerDataStore.Instance が null です！");
            return;
        }

        var data = PlayerDataStore.Instance.playerInfos.ToArray();
        Debug.Log($"📦 データ数: {data.Length}");

        spawner.SetInformation(data);
    }
}