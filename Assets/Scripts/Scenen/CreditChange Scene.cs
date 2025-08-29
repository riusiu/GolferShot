using UnityEngine;
using UnityEngine.SceneManagement;

public partial class CreditchangeScene : MonoBehaviour
{
    public void OnClickCreditButton()
    {
        SceneManager.LoadScene("CREDITScene");
    }
}