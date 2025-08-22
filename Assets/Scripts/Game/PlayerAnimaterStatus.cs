using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimaterStatus : MonoBehaviour
{
    private System.Action _jumpEndAction;
    private System.Action _shotEndAction;
    private System.Action _stanEndAction;
    private System.Action _formEndAction;

    public void Init(System.Action jumpEndAction,
                     System.Action shotEndAction,
                     System.Action stanEndAction,
                     System.Action formEndAction)
    {
        _jumpEndAction = jumpEndAction;
        _shotEndAction = shotEndAction;
        _stanEndAction = stanEndAction;
        _formEndAction = formEndAction;
    }

    private void OnJumpEndAction()
    {
        _jumpEndAction?.Invoke();
    }
    private void OnShotEndAction()
    {
        _shotEndAction?.Invoke();
    }
    private void OnStanEndAction()
    {
        _stanEndAction?.Invoke();
    }
    private void OnFormEndAction()
    {
        _formEndAction?.Invoke();
    }
}
