using UnityEngine;                          // Unityの基本機能

/// <summary>
/// リザルトシーンで使う「表彰台用見た目（プレハブ）」を
/// プレイヤーごとに指定するための小さなアタッチスクリプト。
/// PlayerControllerは改造せず、同じオブジェクトにこれだけ付ければOK。
/// </summary>
public class ResultActorProvider : MonoBehaviour
{
    public GameObject resultActorPrefab;     // ★表彰台に並べるプレハブ（Animator付き推奨）
}