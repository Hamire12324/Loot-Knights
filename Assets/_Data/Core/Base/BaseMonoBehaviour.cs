using UnityEngine;

public class BaseMonoBehaviour : MonoBehaviour
{
    protected virtual void Reset()
    {
        this.LoadComponents();
        this.ResetValue();
    }
    protected virtual void Awake()
    {
        this.LoadComponents();
    }
    protected virtual void OnEnable()
    {
        // For override
    }
    protected virtual void Start()
    {
        // For override
    }
    protected virtual void FixedUpdate()
    {
        // For override
    }
    protected virtual void Update()
    {
        // For override
    }
    protected virtual void LateUpdate()
    {
        // For override
    }
    protected virtual void OnDisable()
    {
        // For override
    }
    protected virtual void OnDestroy()
    {
        // For override
    }
    protected virtual void LoadComponents()
    {
        // For override
    }
    protected virtual void ResetValue()
    {
        // For override
    }

    public virtual void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }
}
