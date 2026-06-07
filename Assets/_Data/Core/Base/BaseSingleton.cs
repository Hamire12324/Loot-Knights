using UnityEngine;

public abstract class BaseSingleton<T> : BaseMonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    public static T Instance
    {
        get
        {
            if (_instance == null)
                Debug.LogError($"{typeof(T)} Singleton not initialized!");
            return _instance;
        }
    }
    public static bool HasInstance => _instance != null;
    public static T InstanceOrNull => _instance;

    protected override void Awake()
    {
        base.Awake();

        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_instance == this)
            _instance = null;
    }
}
