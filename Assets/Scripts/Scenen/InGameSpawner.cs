using UnityEngine;
using UnityEngine.InputSystem;

public class InGameSpawner : MonoBehaviour
{
    public GameObject bluePlayerPrefab;
    public GameObject redPlayerPrefab;
    public GameObject greenPlayerPrefab;
    public GameObject yellowPlayerPrefab;
    
    private PlayerInfo[] _playerInfos = default;
    
    public void SetInformation(PlayerInfo[] playerInfos)
    {
        Debug.Log("🚀 SetInformation() 呼ばれた");
    
        this._playerInfos = playerInfos;
        
        if (_playerInfos == null || _playerInfos.Length == 0)
        {
            Debug.LogWarning("⚠ プレイヤー情報が空です！");
            return;
        }
        
        // 例えばここで生成
        CreateCharacter();
    }


    private void CreateCharacter()
    {
        Debug.Log("🎮 CreateCharacter() 呼ばれた");
        var players = PlayerDataStore.Instance.playerInfos;

        for (int i = 0; i < players.Count; i++)
        {
            Debug.Log($"🧍 プレイヤー{i} を生成開始: {players[i].Character}");
            GameObject prefabToSpawn = null;

            // キャラクタータイプに応じてプレハブを選ぶ
            switch (players[i].Character)
            {
                case CharacterType.Blue:
                    prefabToSpawn = bluePlayerPrefab;
                    break;
                case CharacterType.Red:
                    prefabToSpawn = redPlayerPrefab;
                    break;
                case CharacterType.Green:
                    prefabToSpawn = greenPlayerPrefab;
                    break;
                case CharacterType.Yellow:
                    prefabToSpawn = yellowPlayerPrefab;
                    break;
            }

            // プレイヤーを生成（InputSystemとデバイスを結びつける）
            PlayerInput playerInput = PlayerInput.Instantiate(
                prefab: prefabToSpawn,
                playerIndex: i,
                pairWithDevice: players[i].Device
            );

            // 生成されたプレイヤーの子から Camera を取得して Viewport 設定！
            Camera[] cameras = playerInput.GetComponentsInChildren<Camera>();
            foreach (var cam in cameras)
            {
                cam.rect = GetViewportForPlayer(i);
            }

            // Canvas があるなら同様に設定（UIも分割画面に合わせる）
            Canvas[] canvases = playerInput.GetComponentsInChildren<Canvas>();
            foreach (var canvas in canvases)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cameras.Length > 0 ? cameras[0] : null;
            }
        }
    }

    /// <summary>
    /// プレイヤーIndexに対応する分割画面のViewportを返す
    /// </summary>
    private Rect GetViewportForPlayer(int index)
    {
        switch (index)
        {
            case 0: return new Rect(0f,   0.5f, 0.5f, 0.5f); // 左上
            case 1: return new Rect(0.5f, 0.5f, 0.5f, 0.5f); // 右上
            case 2: return new Rect(0f,   0f,   0.5f, 0.5f); // 左下
            case 3: return new Rect(0.5f, 0f,   0.5f, 0.5f); // 右下
            default: return new Rect(0f, 0f, 1f, 1f);        // 念のため全画面
        }
    }
}
