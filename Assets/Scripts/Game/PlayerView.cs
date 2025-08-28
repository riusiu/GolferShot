using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerView : MonoBehaviour
{
    [SerializeField] public GameObject  playerPrefab;
    public                  Transform[] spawnPoints;  // スポーン位置（エディタでアサイン）
    [SerializeField] private SkinnedMeshRenderer _bodyMaterial;
    [SerializeField] private SkinnedMeshRenderer _visorMaterial;

    [System.Serializable]
    private class PlayerMaterialPack
    {
        public Material BodyMaterial;
        public Material VisorMaterial;
    }
    [SerializeField] private List<PlayerMaterialPack> _playerMaterials = new List<PlayerMaterialPack>();
    private                  GamePlayerPresenter      _gamePlayerPresenter;
    private                  int                      _index;

    // Start is called before the first frame update
    void Start()
    {
        SpawnPlayers();
    }

    public void Init(int index)
    {
        _index                  = index;
        _bodyMaterial.material  = _playerMaterials[index].BodyMaterial;
        _visorMaterial.material = _playerMaterials[index].VisorMaterial;
    }

    private void SpawnPlayers()
    {
        if (EntryManager.Instance == null || EntryManager.Instance.playerList.Count == 0)
        {
            Debug.Log("EntryManagerが見つからない、またはプレイヤー情報が未登録です");
            return;
        }

        var players = EntryManager.Instance.playerList;

        for (int i = 0; i < players.Count; i++)
        {
            if (i >= spawnPoints.Length)
            {
                Debug.LogWarning("スポーンポイントが足りません");
                break;
            }

            PlayerData data = players[i];

            // // プレイヤー生成
            // GameObject player = Instantiate(playerPrefab, spawnPoints[i].position, Quaternion.identity);
            //
            //
            var character = PlayerInput.Instantiate(
                prefab: playerPrefab,
                playerIndex: i,
                pairWithDevice: PlayerJoinManager.joinedDevices[i]
            );
            
            character.gameObject.transform.position = spawnPoints[i].transform.position;
        }
    }
}