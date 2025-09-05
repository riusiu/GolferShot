using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// キャラクターの種類（Blue, Red, Green, Yellow）
/// </summary>
public enum CharacterType
{
    Blue,
    Red,
    Green,
    Yellow
}

/// <summary>
/// プレイヤー1人分のデータ（使用デバイス・選んだキャラ）
/// </summary>
[System.Serializable]
public class PlayerInfo
{
    public InputDevice   Device;    // 使用デバイス
    public CharacterType Character; // キャラクター
}