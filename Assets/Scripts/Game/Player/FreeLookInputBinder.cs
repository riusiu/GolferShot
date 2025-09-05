using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;

public class FreeLookInputBinder : MonoBehaviour
{
    [SerializeField] private InputActionReference lookActionReference; // これをインスペクターで設定！

    private void Start()
    {
        var inputProvider = GetComponentInChildren<CinemachineInputProvider>();
        if (inputProvider == null)
        {
            Debug.LogError("CinemachineInputProvider が見つかりません！");
            return;
        }

        // InputActionReference を使って設定
        inputProvider.XYAxis = lookActionReference;

        Debug.Log("CinemachineInputProvider に Look アクションを設定しました");
    }
}