using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private List<SceneChangeButton> _sceneChangeButtons = new List<SceneChangeButton>();

    // Start is called before the first frame update
    void Start()
    {
        foreach (var (button, index) in _sceneChangeButtons.Select((button, index) => (button, index)))
        {
            button.SetMouseActionCallback(index, ButtonActiveChange);
        }
    }

    private void ButtonActiveChange(int index)
    {
        _sceneChangeButtons[1 - index].MouseExit();
    }
}
