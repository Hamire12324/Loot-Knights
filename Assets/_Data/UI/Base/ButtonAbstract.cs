using UnityEngine;
using UnityEngine.UI;

public abstract class ButtonAbstract : BaseMonoBehaviour
{
    [SerializeField] protected Button button;
    protected override void Start()
    {
        base.Start();
        this.AddOnClickEvent();
    }
    protected abstract void OnClick();

    protected override void LoadComponents()
    {
        base.LoadComponents();
        this.LoadButton();
    }
    protected virtual void LoadButton()
    {
        if (button != null) return;
        this.button = transform.GetComponent<Button>();
        if (button != null)
        {
            Debug.Log(transform.name + ": LoadButton", gameObject);
        }
    }
    protected virtual void AddOnClickEvent()
    {
        if (this.button == null)
        {
            Debug.LogError(transform.name + ": Missing Button component.", gameObject);
            return;
        }

        this.button.onClick.RemoveListener(this.OnClick);
        this.button.onClick.AddListener(this.OnClick);
    }
}
