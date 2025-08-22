using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePlayerPresenter
{
    private GamePlayerModel _gamePlayerModel;
    private GamePlayerLogic _gamePlayerLogic;

    // Start is called before the first frame update
    public void Initialize()
    {
        _gamePlayerModel = new GamePlayerModel();
        _gamePlayerLogic = new GamePlayerLogic();
    }

    // Update is called once per frame
    public void Update()
    {
        _gamePlayerModel.Update();
        _gamePlayerLogic.Update();
    }
}