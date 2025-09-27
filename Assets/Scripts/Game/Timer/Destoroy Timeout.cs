using System;                                           // そのまま
using System.Collections;                               // そのまま
using System.Collections.Generic;                       // そのまま
using UnityEngine;                                      // そのまま
using UnityEngine.UI;                                   // そのまま
using TMPro;                                            // そのまま

public class DestroyTimeout : MonoBehaviour
{
    public                   float           countdownTime = 3f;  // そのまま
    public                   TextMeshProUGUI countdownText;       // そのまま
    [SerializeField] private GameObject      BlackScreen;         // そのまま
    public GameObject Timer;                                      // そのまま

    // ★追加：プレイヤーロック用（必要なければインスペクタでOFFに）
    [Header("Player Lock（カウント中の操作禁止）")]
    [SerializeField] private bool lockDuringCountdown = true;     // ★追加
    [SerializeField] private bool autoFindPlayers     = true;     // ★追加
    [SerializeField] private PlayerController[] players;          // ★追加

    // Start is called before the first frame update
    void Start()
    {
        // ★変更：いきなりCountdownせず、まずロック＆1フレーム待機を挟む
        StartCoroutine(BootAndCountdown());                       // ★追加
    }

    // ★追加：Awake未実行のプレイヤーを避けるため1フレーム待ってからロック→元のカウントを実行
    private IEnumerator BootAndCountdown()
    {
        if (lockDuringCountdown)
        {
            // 1フレーム待つ：各プレイヤーのAwake/Startを先に走らせる
            yield return null;                                    // ★追加：ここ重要

            // プレイヤー検出（自動 or 手動）
            if (autoFindPlayers || players == null || players.Length == 0)
            {
                players = FindObjectsOfType<PlayerController>();   // ★追加：アクティブのみで十分
            }
            foreach (var p in players)
            {
                if (!p) continue;
                p.SetExternalActionLock(true);                    // ★追加：カウント中は操作禁止
            }
        }

        // 元のカウント処理を実行
        yield return StartCoroutine(Countdown());                 // ★追加：元メソッドをそのまま呼ぶ

        // GO! 後にロック解除（破棄前に必ず）
        if (lockDuringCountdown && players != null)
        {
            foreach (var p in players)
            {
                if (!p) continue;
                p.SetExternalActionLock(false);                   // ★追加：操作解禁
            }
        }
    }

    IEnumerator Countdown()
    {
        countdownText.text = $"{countdownTime}";
        yield return new WaitForSecondsRealtime(1f);
        countdownTime      = 2f;
        countdownText.text = $"{countdownTime}";
        yield return new WaitForSecondsRealtime(1f);
        countdownTime      = 1f;
        countdownText.text = $"{countdownTime}";
        yield return new WaitForSecondsRealtime(1f);
        countdownTime      = 0f;
        countdownText.text = $"{countdownTime}";
        yield return new WaitForSecondsRealtime(1f);
        countdownText.text = "GO!";
        yield return new WaitForSecondsRealtime(1f);
        if (BlackScreen != null) Destroy(BlackScreen);   // ★安全のためnullチェック
        Destroy(gameObject);
        if (Timer != null) Timer.SetActive(true);        // ★安全のためnullチェック
    }
}
