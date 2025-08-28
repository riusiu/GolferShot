using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DestroyTimeout : MonoBehaviour
{
    public                   float           countdownTime = 3f;
    public                   TextMeshProUGUI countdownText;
    [SerializeField] private GameObject      BlackScreen;
    public GameObject Timer;
    
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Countdown());
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
        Destroy(BlackScreen);
        Destroy(gameObject);
        Timer.SetActive(true);
    }
}