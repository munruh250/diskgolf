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
            ref Coroutine routine,
            float seconds,
            Action show,
            Action hide)
        {
            if (routine != null)
                runner.StopCoroutine(routine);

            show?.Invoke();
            routine = runner.StartCoroutine(HideAfter(runner, seconds, hide, () => routine = null));
            return routine;
        }

        static IEnumerator HideAfter(MonoBehaviour runner, float seconds, Action hide, Action clearRoutine)
        {
            yield return new WaitForSeconds(seconds);
            hide?.Invoke();
            clearRoutine?.Invoke();
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
