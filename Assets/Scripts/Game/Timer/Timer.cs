using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class TimeCounter : MonoBehaviour
{ 
    [SerializeField] private GameObject TimeText;
    public                   int        countdownMinutes = 3;
    private                  float      countdownSeconds;
    private                  TextMeshProUGUI       timeText;

    private void Start()
    {
        Time.timeScale   = 0;
        timeText         = GetComponent<TextMeshProUGUI>();
        countdownSeconds = countdownMinutes * 60;
        //StartCoroutine(Countdown());
    }

    void Update()
    {
        countdownSeconds -= Time.deltaTime;
        var span = new TimeSpan(0, 0, (int)countdownSeconds);
        timeText.text = span.ToString(@"mm\:ss");

        if (countdownSeconds <= 0)
        {
            // 0秒になったときの処理
            Debug.Log("Game Over");
            SceneManager.LoadScene("ResultScene");
        }
    }

    // IEnumerator Countdown()
    // {
    //     yield return new WaitForSecondsRealtime(3f);
    //     TimeText.SetActive(true);
    //     Time.timeScale   = 1;
    // }
}