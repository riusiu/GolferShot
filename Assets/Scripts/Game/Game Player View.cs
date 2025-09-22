using System.Collections.Generic;
using UnityEngine;

public class GamePlayerView : MonoBehaviour
{
    private enum PlayerStatus
    {
        walking,
        form,
        swing,
        jump,
        stun,
    }

    [SerializeField] private SkinnedMeshRenderer _bodyMaterial;
    [SerializeField] private SkinnedMeshRenderer _visorMaterial;

    [System.Serializable]
    private class PlayerMaterialPack
    {
        public Material BodyMaterial;
        public Material VisorMaterial;
    }
    [SerializeField] private List<PlayerMaterialPack> _playerMaterials = new List<PlayerMaterialPack>();
    [SerializeField] private PlayerAnimaterStatus _playerAnimaterStatus;
    private GamePlayerPresenter _gamePlayerPresenter;
    private int _index;
    private PlayerStatus _playerStatus = PlayerStatus.walking;

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
        _playerAnimaterStatus.Init(JumpEndAction, ShotEndAction, StanEndAction, FormEndAction);
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void JumpEndAction()
    {
        Debug.Log("Jump End");
    }
    private void ShotEndAction()
    {
        Debug.Log("Shot End");
    }
    private void StanEndAction()
    {
        Debug.Log("Stan End");
    }
    private void FormEndAction()
    {
        Debug.Log("Form End");
    }
}
