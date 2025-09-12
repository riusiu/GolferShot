using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeTest : MonoBehaviour
{
    private SePlayer _sePlayer;
    // Start is called before the first frame update
    void Start()
    {
        _sePlayer = GetComponent<SePlayer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            _sePlayer.PlaySe();
        }
    }
}
