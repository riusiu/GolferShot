using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Animations;
using TMPro;

public class Result : MonoBehaviour
{
    [SerializeField] private Text ScoreText;
    PlayerData                      playerData;
    // Start is called before the first frame update
    void Start()
    {
        int score = playerData.score;

        ScoreText.text = string.Format("score");
        
        if (score > score)
        {
            Animator animator = gameObject.GetComponent<Animator>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
