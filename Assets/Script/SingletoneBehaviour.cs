using System;
using UnityEngine;

public abstract class SingletonBehaviour<T> : MonoBehaviour where T : SingletonBehaviour<T>
{
    public static bool InstanceExist => instance != null;
    protected static T instance;
    public static T Instance
    {
        get
        {
            CreateInstance();
            return instance;
        }
    }

    void OnDestroy()
    {
        instance = null;
        Destroy();
    }

    public virtual void Destroy()
    {
    }

    public static void CreateInstance()
    {
        if (instance != null)
            return;
        var t = FindObjectOfType<T>();
        if (t != null)
        {
            DontDestroyOnLoad(t.gameObject);
            instance = t;
            instance.Init();
            return;
        }
        var obj = new GameObject(typeof(T).Name);
        instance = obj.AddComponent<T>();
        instance.Init();
        DontDestroyOnLoad(obj);
    }

    public abstract void Init();
}

