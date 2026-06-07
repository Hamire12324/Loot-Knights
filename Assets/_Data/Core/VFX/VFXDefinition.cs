using UnityEngine;

[CreateAssetMenu(fileName = "VFXDefinition", menuName = "Loot Knights/VFX/Definition")]
public class VFXDefinition : ScriptableObject
{
    public PoolObj Prefab;

    [Header("Transform")]
    public Vector3 Offset;
    public bool MirrorHorizontallyByDirection;
    public bool FlipX;
    public bool FlipY;
    public bool ParentToAnchor;
}
