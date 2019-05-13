using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Component {

    bool _registered = false;

    static T s_instance;
    public static T instance
    {
        get
        {
            //if (s_applicationIsQuitting)
            //{
            //    Debug.LogWarning("[Singleton] Instance '" + typeof(T) +
            //        "' already destroyed on application quit." +
            //        " Won't create again - returning null.");
            //    return null;
            //}

            {
                if (s_instance == null)
                {
                    s_instance = FindObjectOfType<T>();

                    if (FindObjectsOfType(typeof(T)).Length > 1)
                    {
                        Debug.LogError("[Singleton] Something went really wrong " +
                            " - there should never be more than 1 singleton!" +
                            " Reopening the scene might fix it.");
                        return s_instance;
                    }

                    //Debug.Log("[Singleton] '" + typeof(T) +
                    //    "' Doesn't exist!.");
                }

                return s_instance;
            }
        }
    }

    public virtual void Awake()
    {
        if (!_registered)
        {
            Register();
        }
    }

    public virtual void OnEnable()
    {
        if (!_registered)
        {
            Register();
        }
    }

    public virtual void Register()
    {
        if (s_instance != null)
        {
            Debug.LogWarning("Only One Singleton can exist!");
        }
        s_instance = this as T;
        _registered = true;
    }
}
