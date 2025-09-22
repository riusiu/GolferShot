using UnityEngine;                                  // Unityの基本機能

/// <summary>
/// 「このオブジェクトを最後に打ったプレイヤー」を覚えておく超小物。
/// ・ShotHitCollider が SetShooter(...) で記録
/// ・HoleGoal が GetShooter() で取得してスコア加算
/// ・Destroyされても問題ない（必要ならまた打たれた時に付与/上書きされる）
/// </summary>
public class ShotOwnership : MonoBehaviour
{
    // 内部に保持する参照（最後に打った PlayerController）
    [SerializeField] private PlayerController _lastShooter;   // 最後に打った人（インスペクタで確認用にSerialize）

    /// <summary>
    /// 誰が打ったかを記録する（ShotHitCollider から呼ばれる）
    /// </summary>
    public void SetShooter(PlayerController shooter)          // shooter=打ったプレイヤー
    {
        _lastShooter = shooter;                               // 参照を保存（nullになることもある）
    }

    /// <summary>
    /// 最後に打ったプレイヤーを取得する（HoleGoal から呼ばれる）
    /// </summary>
    public PlayerController GetShooter()                      // 誰が打ったかを返す
    {
        return _lastShooter;                                  // 参照をそのまま返す（nullなら未記録）
    }

    /// <summary>
    /// 所有情報を消す（必要に応じて呼ぶ。通常は不要）
    /// </summary>
    public void ClearShooter()                                // 記録をリセット
    {
        _lastShooter = null;                                  // 参照を消す
    }
}