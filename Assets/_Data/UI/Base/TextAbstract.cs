using TMPro;
using UnityEngine;

public class TextAbstract : BaseMonoBehaviour
{
    [SerializeField] protected TMP_Text text;

    public TMP_Text Text => text;

    protected override void LoadComponents()
    {
        base.LoadComponents();

        this.LoadText();
    }

    protected virtual void LoadText()
    {
        if (text != null) return;

        text = GetComponent<TMP_Text>();

        if (text != null) return;

        text = GetComponentInChildren<TMP_Text>(true);
    }
}
