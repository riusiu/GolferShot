using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePlayerView : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer _bodyMaterial;
    [SerializeField] private SkinnedMeshRenderer _visorMaterial;

    [System.Serializable]
    private class PlayerMaterialPack
    {
        public Material BodyMaterial;
        public Material VisorMaterial;
    }
    [SerializeField] private List<PlayerMaterialPack> _playerMaterials = new List<PlayerMaterialPack>();
    private GamePlayerPresenter _gamePlayerPresenter;
    private int _index;

    // Start is called before the first frame update
    void Start()
    {
        _gamePlayerPresenter = new GamePlayerPresenter();
        Init(3);
    }

    public void Init(int index)
    {
        _index = index;
        _bodyMaterial.material = _playerMaterials[index].BodyMaterial;
        _visorMaterial.material = _playerMaterials[index].VisorMaterial;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
