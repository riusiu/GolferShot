using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class ExitManager : MonoBehaviour
{
    public static ExitManager     Instance;
    public        List<PlayerData> playerList = new List<PlayerData>();
    
    private int readyCount = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterPlayer(GameObject playerObj)
    {
        PlayerData data = playerObj.GetComponent<PlayerData>();
        if (data == null)
        {
            Debug.LogError("Player に PlayerData が付いていません");
            return;
        }
        
        playerList.Add(data);

        //PlayerEntryUIManager.Instance.UpdateSlot(data.playerIndex, isActive: true);
    }

    public void PlayerReady(int index)
    {
        //PlayerEntryUIManager.Instance.SetPlayerReadyUI(index);
        readyCount++;

        if (readyCount == playerList.Count && playerList.Count >= 4)
        {
            // カウントダウンせずに即シーン遷移
            SceneManager.LoadScene("Title Scene");
        }
    }
}
