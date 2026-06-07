using System;
using UnityEngine;

[Serializable]
public class PoolConfig
{
    public string Key;
    public PoolObj Prefab;
    [Min(0)] public int PreloadAmount = 5;
    [Min(1)] public int MaxSize = 30;
    public bool CanExpand = true;
    public Transform Parent;
}
