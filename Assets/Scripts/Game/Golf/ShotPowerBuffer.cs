using System.Collections.Generic; // 辞書
using UnityEngine;                // Debug用（なくてもOK）

/// <summary>
/// 「誰のショット倍率がいくつか」を一時的に覚えておく静的バッファ。
/// ・PowerGaugeUI が Commit で Set
/// ・ShotHitCollider が Get で参照（使い終わったら自動 Clear でもOK）
/// </summary>
public static class ShotPowerBuffer
{
    private static readonly Dictionary<PlayerController, float> _map = new();

    public static void Set(PlayerController owner, float scale)
    {
        if (!owner) return;
        _map[owner] = scale;
    }

    public static float Get(PlayerController owner, float defaultScale = 1f, bool clearAfterGet = true)
    {
        if (!owner) return defaultScale;
        if (_map.TryGetValue(owner, out float s))
        {
            if (clearAfterGet) _map.Remove(owner);   // 取得後は消してワンショット化
            return s;
        }
        return defaultScale;
    }

    public static void Clear(PlayerController owner)
    {
        if (!owner) return;
        _map.Remove(owner);
    }
}