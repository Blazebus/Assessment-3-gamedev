using System;
using System.Collections.Generic;
using UnityEngine;

public class Tweener : MonoBehaviour
{
    private List<Tween> activeTweens = new List<Tween>();

    private void Update()
    {
        for (int i = activeTweens.Count - 1; i >= 0; i--)
        {
            Tween tween = activeTweens[i];

            if (tween == null || tween.Target == null)
            {
                activeTweens.RemoveAt(i);
                continue;
            }

            if (tween.Duration > 0f)
            {
                tween.Progress += Time.deltaTime / tween.Duration;
            }
            else
            {
                tween.Progress = 1f;
            }

            if (tween.Progress < 1f)
            {
                tween.Target.position = Vector3.Lerp(tween.StartPos, tween.EndPos, tween.Progress);
            }
            else
            {
                tween.Target.position = tween.EndPos;
                Action callback = tween.OnComplete;
                activeTweens.RemoveAt(i);
                callback?.Invoke();
            }
        }
    }

    public bool TweenExists(Transform target)
    {
        if (target == null) return false;
        foreach (Tween tween in activeTweens)
        {
            if (tween != null && tween.Target == target)
            {
                return true;
            }
        }
        return false;
    }

    public bool AddTween(Transform targetObject, Vector3 startPos, Vector3 endPos, float duration, Action onComplete = null)
    {
        if (targetObject == null) return false;

        if (!TweenExists(targetObject))
        {
            activeTweens.Add(new Tween(targetObject, startPos, endPos, Time.time, duration, onComplete));
            return true;
        }
        return false;
    }

    public void CancelTween(Transform target)
    {
        if (target == null) return;
        for (int i = activeTweens.Count - 1; i >= 0; i--)
        {
            if (activeTweens[i] != null && activeTweens[i].Target == target)
            {
                activeTweens.RemoveAt(i);
            }
        }
    }
}
