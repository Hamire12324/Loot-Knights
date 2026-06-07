using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "SFXDefinition", menuName = "Loot Knights/SFX/Definition")]
public class SFXDefinition : ScriptableObject
{
    [Header("Clips")]
    public AudioClip[] Clips;

    [Header("Mix")]
    public AudioMixerGroup Output;
    [Range(0f, 1f)] public float Volume = 1f;
    [Range(0f, 1f)] public float SpatialBlend = 0f;
    [Range(0f, 1f)] public float ReverbZoneMix = 0f;
    [Range(0f, 5f)] public float DopplerLevel = 0f;

    [Header("Pitch")]
    public Vector2 PitchRange = Vector2.one;

    [Header("Distance")]
    [Min(0f)] public float MinDistance = 1f;
    [Min(0.01f)] public float MaxDistance = 20f;

    [Header("Voice")]
    [Range(0, 256)] public int Priority = 128;

    public AudioClip GetClip()
    {
        if (Clips == null || Clips.Length == 0)
            return null;

        int index = Random.Range(0, Clips.Length);
        return Clips[index];
    }

    public float GetPitch()
    {
        float min = Mathf.Min(PitchRange.x, PitchRange.y);
        float max = Mathf.Max(PitchRange.x, PitchRange.y);
        return Mathf.Approximately(min, max) ? min : Random.Range(min, max);
    }
}
