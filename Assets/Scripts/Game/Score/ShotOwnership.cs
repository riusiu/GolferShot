using UnityEngine;                                // Unityの基本機能

/// <summary>
/// 「このオブジェクトを最後に打ったプレイヤー」を記録する小さなマーカー。
/// ・ShotHitCollider がヒットさせた瞬間に SetShooter(...) を呼ぶ
/// ・HoleGoal は LastShooter を見て得点者を決定する
/// ・二重加点防止のため hasScored を持つ
/// </summary>
public class ShotOwnership : MonoBehaviour
{
    public PlayerController lastShooter;       // ★最後に打ったプレイヤー（加点者）
    public float            lastShotTime;      // ★打った時刻（任意：デバッグ/ルール用）
    public bool             hasScored = false; // ★もうスコア済みか（多重加点防止）

    public void SetShooter(PlayerController shooter) // ★打った人を記録する
    {
        lastShooter  = shooter;   // 記録：プレイヤー参照
        lastShotTime = Time.time; // 記録：ゲーム内時刻
        hasScored    = false;     // 新規ショットなのでスコア済みフラグを解除
    }
}