using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using LitMotion;
using LitMotion.Extensions;
using Cysharp.Threading.Tasks;
using System.Threading;

public class TimerView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;

    private float _timer = 3f*60f;
    private bool _isTimerStart = false;

    private System.Action _endCallback;

    // Update is called once per frame
    void Update()
    {
        if(_isTimerStart is false && Input.GetMouseButtonDown(0))
        {
            _isTimerStart = true;
            TimerDown().Forget();
        }
    }

    private async UniTask TimerDown()
    {
        while(_timer > 0)
        {
            _timer -= Time.deltaTime;
            _timer = Mathf.Max(_timer, 0f);
            var min = Mathf.Floor(_timer / 60f);
            var sec = Mathf.Floor(_timer - min * 60);
            var msec = (_timer - (min * 60 + sec)) * 100;
            _text.text = min.ToString() + ":" + sec.ToString("00") + ":" + msec .ToString("00");
            await UniTask.Yield();
        }
        _endCallback?.Invoke();
    }


}
