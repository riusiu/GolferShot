using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class BackchangeScene : MonoBehaviour
{
    public void OnClickBackTitle()
    {
        SceneManager.LoadScene("TitleScene");
    }
}