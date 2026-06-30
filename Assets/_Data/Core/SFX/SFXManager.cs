using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXManager : BaseSingleton<SFXManager>
{
    [Header("Pool")]
    [SerializeField, Min(1)] private int initialPoolSize = 12;
    [SerializeField, Min(1)] private int maxVoices = 32;

    [Header("Global")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;

    private readonly Queue<AudioSource> availableSources = new();
    private readonly HashSet<AudioSource> activeSources = new();
    private readonly Dictionary<AudioSource, float> sourceBaseVolumes = new();

    public float MasterVolume
    {
        get => masterVolume;
        set
        {
            masterVolume = Mathf.Clamp01(value);

            foreach (AudioSource source in activeSources)
            {
                if (source == null) continue;

                if (sourceBaseVolumes.TryGetValue(source, out float baseVolume))
                {
                    source.volume = baseVolume * masterVolume;
                }
            }
        }
    }

    protected override void Awake()
    {
        base.Awake();

        if (InstanceOrNull != this)
            return;

        MasterVolume = GameSettingsData.EffectVolume;
        PreloadSources();
    }

    public static AudioSource Play(SFXDefinition definition, Vector3 position)
    {
        SFXManager manager = HasInstance ? InstanceOrNull : CreateRuntimeManager();
        return manager != null ? manager.PlayInternal(definition, position) : null;
    }

    private static SFXManager CreateRuntimeManager()
    {
        GameObject obj = new("SFXManager");
        return obj.AddComponent<SFXManager>();
    }

    private void PreloadSources()
    {
        for (int i = availableSources.Count + activeSources.Count; i < initialPoolSize; i++)
            availableSources.Enqueue(CreateSource());
    }

    private AudioSource PlayInternal(SFXDefinition definition, Vector3 position)
    {
        if (definition == null)
            return null;

        AudioClip clip = definition.GetClip();
        if (clip == null)
            return null;

        AudioSource source = GetSource();
        if (source == null)
            return null;

        float pitch = Mathf.Max(0.01f, definition.GetPitch());
        ConfigureSource(source, definition, clip, pitch, position);

        activeSources.Add(source);
        source.gameObject.SetActive(true);
        source.Play();

        StartCoroutine(ReturnAfterPlayback(source, clip.length / pitch));
        return source;
    }

    private AudioSource GetSource()
    {
        while (availableSources.Count > 0)
        {
            AudioSource source = availableSources.Dequeue();
            if (source != null)
                return source;
        }

        if (activeSources.Count >= maxVoices)
            return null;

        return CreateSource();
    }

    private AudioSource CreateSource()
    {
        GameObject obj = new("SFXSource");
        obj.transform.SetParent(transform);
        obj.SetActive(false);

        AudioSource source = obj.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        return source;
    }

    private void ConfigureSource(
        AudioSource source,
        SFXDefinition definition,
        AudioClip clip,
        float pitch,
        Vector3 position)
    {
        Transform sourceTransform = source.transform;
        sourceTransform.position = position;

        source.outputAudioMixerGroup = definition.Output;
        source.clip = clip;
        sourceBaseVolumes[source] = definition.Volume;
        source.volume = definition.Volume * masterVolume;
        source.pitch = pitch;
        source.priority = definition.Priority;
        source.loop = false;
        source.playOnAwake = false;
        source.spatialBlend = definition.SpatialBlend;
        source.reverbZoneMix = definition.ReverbZoneMix;
        source.dopplerLevel = definition.DopplerLevel;
        source.minDistance = definition.MinDistance;
        source.maxDistance = definition.MaxDistance;
    }

    private IEnumerator ReturnAfterPlayback(AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration);

        if (source == null)
            yield break;

        source.Stop();
        source.clip = null;
        source.gameObject.SetActive(false);
        sourceBaseVolumes.Remove(source);

        if (activeSources.Remove(source))
            availableSources.Enqueue(source);
    }
}
