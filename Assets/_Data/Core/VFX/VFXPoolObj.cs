using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class VFXPoolObj : PoolObj
{
    [Header("Playback")]
    [SerializeField] private bool playOnSpawn = true;
    [SerializeField] private bool restartParticles = true;

    [Header("Return To Pool")]
    [SerializeField] private bool autoReturnToPool = true;
    [SerializeField, Min(-1f)] private float lifetime = -1f;
    [SerializeField, Min(0f)] private float fallbackLifetime = 1f;
    [SerializeField] private bool clearParticlesOnReturn = true;

    [Header("Audio")]
    [SerializeField] private bool restartAudio = false;
    [SerializeField] private bool stopAudioOnReturn = false;
    [SerializeField] private bool forceAudio2DOneShot = false;

    [Header("Cached Components")]
    [SerializeField] private ParticleSystem[] particleSystems;
    [SerializeField] private ParticleSystemRenderer[] particleRenderers;
    [SerializeField] private AudioSource[] audioSources;

    private Coroutine returnCoroutine;
    private bool suppressAutoReturn;

    protected override void OnEnable()
    {
        base.OnEnable();

        if (playOnSpawn && !IsInPool)
            Restart();
    }

    protected override void OnDisable()
    {
        StopReturnCoroutine();
        base.OnDisable();
    }

    public override void OnSpawnedFromPool()
    {
        base.OnSpawnedFromPool();

        if (playOnSpawn)
            Restart();
    }

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadParticleSystems();
        LoadParticleRenderers();
    }
    public void Restart()
    {
        if (!isActiveAndEnabled)
            return;

        StopReturnCoroutine();
        LoadParticleSystems();
        LoadAudioSources();
        PlayParticles();
        PlayAudio();
        ScheduleReturn();
    }

    public void RestartIfPlayOnSpawn()
    {
        if (playOnSpawn)
            Restart();
    }
    private void LoadParticleSystems()
    {
        if (particleSystems != null && particleSystems.Length > 0)
            return;

        particleSystems = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void LoadParticleRenderers()
    {
        if (particleRenderers != null && particleRenderers.Length > 0)
            return;

        particleRenderers = GetComponentsInChildren<ParticleSystemRenderer>(true);
    }

    private void LoadAudioSources()
    {
        if (audioSources != null && audioSources.Length > 0)
            return;

        audioSources = GetComponentsInChildren<AudioSource>(true);
    }
    public override void OnReturnedToPool()
    {
        StopReturnCoroutine();
        StopParticles();
        StopAudio();
        suppressAutoReturn = false;

        base.OnReturnedToPool();
    }

    private void StopReturnCoroutine()
    {
        if (returnCoroutine == null)
            return;

        StopCoroutine(returnCoroutine);
        returnCoroutine = null;
    }

    private void ScheduleReturn()
    {
        if (!autoReturnToPool || suppressAutoReturn)
            return;

        returnCoroutine = StartCoroutine(ReturnAfterPlayback(lifetime));
    }

    public void SetAutoReturnToPool(bool enabled)
    {
        suppressAutoReturn = !enabled;

        if (!enabled)
            StopReturnCoroutine();
        else if (isActiveAndEnabled && returnCoroutine == null)
            ScheduleReturn();
    }

    private IEnumerator ReturnAfterPlayback(float activeLifetime)
    {
        if (activeLifetime > 0f)
        {
            yield return new WaitForSeconds(activeLifetime);
        }
        else if (activeLifetime <= 0f && AnyParticleLooping())
        {
            yield return new WaitForSeconds(fallbackLifetime);
        }
        else if (particleSystems != null && particleSystems.Length > 0)
        {
            yield return null;

            while (AnyParticleAlive())
                yield return null;
        }
        else
        {
            yield return new WaitForSeconds(fallbackLifetime);
        }

        returnCoroutine = null;
        ReturnToPool();
    }

    private bool AnyParticleAlive()
    {
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];
            if (particleSystem != null && particleSystem.IsAlive(true))
                return true;
        }

        return false;
    }

    private bool AnyParticleLooping()
    {
        if (particleSystems == null)
            return false;

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];
            if (particleSystem == null)
                continue;

            ParticleSystem.MainModule main = particleSystem.main;
            if (main.loop)
                return true;
        }

        return false;
    }

    private void PlayParticles()
    {
        if (particleSystems == null)
            return;

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];
            if (particleSystem == null) continue;

            if (restartParticles)
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            particleSystem.Play(true);
        }
    }

    private void PlayAudio()
    {
        if (!restartAudio || audioSources == null)
            return;

        for (int i = 0; i < audioSources.Length; i++)
        {
            AudioSource audioSource = audioSources[i];
            if (audioSource == null || audioSource.clip == null) continue;

            if (forceAudio2DOneShot)
            {
                audioSource.loop = false;
                audioSource.spatialBlend = 0f;
                audioSource.dopplerLevel = 0f;
                audioSource.spatialize = false;
            }

            audioSource.Stop();
            audioSource.time = 0f;
            audioSource.Play();
        }
    }

    private void StopParticles()
    {
        if (particleSystems == null)
            return;

        ParticleSystemStopBehavior stopBehavior = clearParticlesOnReturn
            ? ParticleSystemStopBehavior.StopEmittingAndClear
            : ParticleSystemStopBehavior.StopEmitting;

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];
            if (particleSystem == null) continue;
            particleSystem.Stop(true, stopBehavior);
        }
    }

    private void StopAudio()
    {
        if (!stopAudioOnReturn || audioSources == null)
            return;

        for (int i = 0; i < audioSources.Length; i++)
        {
            AudioSource audioSource = audioSources[i];
            if (audioSource == null) continue;
            audioSource.Stop();
        }
    }

    public void SetRendererFlip(bool flipX, bool flipY)
    {
        LoadParticleRenderers();

        for (int i = 0; i < particleRenderers.Length; i++)
        {
            ParticleSystemRenderer particleRenderer = particleRenderers[i];
            if (particleRenderer == null) continue;

            Vector3 flip = particleRenderer.flip;
            flip.x = flipX ? 1f : 0f;
            flip.y = flipY ? 1f : 0f;
            particleRenderer.flip = flip;
        }
    }
}
