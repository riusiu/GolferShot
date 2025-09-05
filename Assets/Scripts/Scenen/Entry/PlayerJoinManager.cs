using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerJoinManager : MonoBehaviour
{
    [SerializeField] private InputAction playerJoinInputAction = default; // 入室用アクション
    [SerializeField] private PlayerInput playerPrefab          = default; // PlayerInput付きプレハブ
    [SerializeField] private int         maxPlayerCount        = default; // 最大プレイヤー数

    private InputDevice[] joinedDevices      = default; // 登録済みデバイス
    private int           currentPlayerCount = 0;       // 現在の参加数

    // ▼ 画像切り替え用（追加）
    [Header("UI Images")]
    [SerializeField] private GameObject[] readyImages; // 「準備OK？」画像（初期表示）

    [SerializeField] private GameObject[] okImages; // 「OK!!」画像（切り替え後）

    private void Awake()
    {
        joinedDevices = new InputDevice[maxPlayerCount];

        playerJoinInputAction.Enable();
        playerJoinInputAction.performed += OnJoin;

        // 画像の初期化（全部「準備OK？」表示に）
        for (int i = 0; i < maxPlayerCount; i++)
        {
            if (readyImages != null && i < readyImages.Length && readyImages[i] != null)
                readyImages[i].SetActive(true);

            if (okImages != null && i < okImages.Length && okImages[i] != null)
                okImages[i].SetActive(false);
        }
    }

    private void OnDestroy()
    {
        playerJoinInputAction.Dispose();
    }

    private void OnJoin(InputAction.CallbackContext context)
    {
        // 最大数に達していたら無視
        if (currentPlayerCount >= maxPlayerCount)
            return;

        // 同じデバイスがすでに登録されているなら無視
        foreach (var device in joinedDevices)
        {
            if (context.control.device == device)
                return;
        }

        // プレイヤー生成
        PlayerInput.Instantiate(
            prefab: playerPrefab.gameObject,
            playerIndex: currentPlayerCount,
            pairWithDevice: context.control.device
        );
        
        // ★★★ プレイヤーデータを保存（ここが重要！）★★★
        PlayerDataStore.Instance.playerInfos.Add(new PlayerInfo
        {
            Character = (CharacterType)currentPlayerCount, // ここは仮にインデックスに応じた色を割り当てています
            Device    = context.control.device
        });


        // デバイス記録
        joinedDevices[currentPlayerCount] = context.control.device;

        // ▼ 画像切り替え（追加）
        if (readyImages != null && currentPlayerCount < readyImages.Length)
            if (readyImages[currentPlayerCount] != null)
                readyImages[currentPlayerCount].SetActive(false);

        if (okImages != null && currentPlayerCount < okImages.Length)
            if (okImages[currentPlayerCount] != null)
                okImages[currentPlayerCount].SetActive(true);

        // カウント追加
        currentPlayerCount++;

        // 参加人数が上限に達したら、1秒後にシーン遷移
        if (currentPlayerCount == maxPlayerCount)
        {
            Debug.Log("全プレイヤーが参加しました。ゲーム画面へ移動します。");
            Invoke(nameof(LoadGameScene), 1f); // 1秒待ってからシーンをロード
        }

    }
    
    private void LoadGameScene()
    {
        SceneManager.LoadScene("GameScene"); // ← あなたのゲーム画面のシーン名に変更してください
    }
}
