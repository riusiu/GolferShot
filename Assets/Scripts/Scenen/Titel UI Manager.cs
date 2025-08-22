using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TitleUIManager : MonoBehaviour
{
    public GameObject firstSelectedButton;

    void Start()
    {
        EventSystem.current.SetSelectedGameObject(firstSelectedButton);
    }
}