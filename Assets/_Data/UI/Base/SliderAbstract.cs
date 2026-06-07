using UnityEngine;
using UnityEngine.UI;

public abstract class SliderAbstract : BaseMonoBehaviour
{
    [SerializeField] protected Slider slider;

    protected override void LoadComponents()
    {
        base.LoadComponents();
        LoadSlider();
    }

    protected virtual void LoadSlider()
    {
        if (slider != null) return;

        slider = GetComponent<Slider>();
    }

    public virtual void SetValue(float value)
    {
        if (slider == null) return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.value = Mathf.Clamp01(value);
    }
}
