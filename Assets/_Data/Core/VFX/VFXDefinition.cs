using UnityEngine;

[CreateAssetMenu(fileName = "VFXDefinition", menuName = "Loot Knights/VFX/Definition")]
public class VFXDefinition : ScriptableObject
{
    public PoolObj Prefab;

    [Header("Transform")]
    public Vector3 Offset;
    [Min(0.01f)] public float Scale = 1f;
    public bool MirrorHorizontallyByDirection;
    public bool FlipX;
    public bool FlipY;
    public bool ParentToAnchor;

    public float EffectiveScale => Scale > 0f ? Scale : 1f;

    private void OnValidate()
    {
        if (Scale <= 0f)
            Scale = 1f;
    }
}
