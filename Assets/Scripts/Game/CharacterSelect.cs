using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterSelect : MonoBehaviour
{
    public void RegisterPlayer(InputDevice device, CharacterType character)
    {
        // 新しいプレイヤー情報を作る
        var playerInfo = new PlayerInfo
        {
            Device    = device,
            Character = character
        };

        // 保存しておく
        PlayerDataStore.Instance.playerInfos.Add(playerInfo);
    }

    public void StartGame()
    {
        // 情報を保持したままゲームシーンへ移動！
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }
}
