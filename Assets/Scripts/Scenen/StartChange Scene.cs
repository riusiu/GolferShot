using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class StartchangeScene : MonoBehaviour
{
    public void OnClickStartButton()
    {
        SceneManager.LoadScene("EntryScene");
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}