using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OnEnableEvent : MonoBehaviour {

    public UnityEvent onEnable;
    public UnityEvent onDisable;

    public void EnableGameObject()
    {
        gameObject.SetActive(true);
    }

    public void DisableGameObject()
    {
        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (enabled) {
            //Debug.Log(name+" ON ENABLE EVENT");
            onEnable.Invoke();
        }
    }

    void OnDisable()
    {
        if (enabled)
        {
            onDisable.Invoke();
        }
    }
}
