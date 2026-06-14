using System;
using System.Collections;
using UnityEngine;

namespace DiskGolf.UI.Callouts
{
    public static class CalloutLifecycle
    {
        public static void BringToFront(Transform target)
        {
            if (target != null)
                target.SetAsLastSibling();
        }

        public static Coroutine ShowBriefly(
            MonoBehaviour runner,
            Coroutine existing,
            float seconds,
            Action show,
            Action hide)
        {
            if (existing != null)
                runner.StopCoroutine(existing);

            show?.Invoke();
            return runner.StartCoroutine(HideAfter(seconds, hide));
        }

        static IEnumerator HideAfter(float seconds, Action hide)
        {
            yield return new WaitForSeconds(seconds);
            hide?.Invoke();
        }

        public static void Cancel(ref Coroutine routine, MonoBehaviour runner)
        {
            if (routine == null)
                return;

            runner.StopCoroutine(routine);
            routine = null;
        }
    }
}
