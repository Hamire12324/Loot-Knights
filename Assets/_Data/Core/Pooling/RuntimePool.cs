using System.Collections.Generic;
using UnityEngine;

internal class RuntimePool
{
    public PoolConfig Config;
    public Queue<PoolObj> InactiveObjects = new();
    public HashSet<PoolObj> ActiveObjects = new();
    public Transform Parent;

    public int TotalCount => InactiveObjects.Count + ActiveObjects.Count;
}
