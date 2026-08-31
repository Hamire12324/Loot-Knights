using TMPro;
using UnityEngine;

public sealed class SkillTreeTextView : TextAbstract
{
    public string Value
    {
        get => text != null ? text.text : string.Empty;
        set { if (text != null) text.text = value ?? string.Empty; }
    }

    public void SetColor(Color color)
    {
        if (text != null) text.color = color;
    }

    public static SkillTreeTextView GetOrAdd(Transform target)
    {
        if (target == null)
            return null;

        SkillTreeTextView view = target.GetComponent<SkillTreeTextView>();
        return view != null ? view : target.gameObject.AddComponent<SkillTreeTextView>();
    }
}
