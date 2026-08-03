using UnityEngine;

public class CharacterSkillCooldown
{
    private float lastCastTime = -999f;
    private float duration;

    public bool IsReady => Remaining <= 0f;
    public float Remaining => Mathf.Max(0f, lastCastTime + duration - Time.time);
    public float Normalized => duration <= 0f ? 0f : Mathf.Clamp01(Remaining / duration);

    public void Start(float cooldown)
    {
        duration = Mathf.Max(0f, cooldown);
        lastCastTime = Time.time;
    }

    public void Reset()
    {
        lastCastTime = -999f;
        duration = 0f;
    }

    public bool Reduce(float seconds)
    {
        if (seconds <= 0f || IsReady)
            return false;

        lastCastTime -= seconds;
        return true;
    }
}
