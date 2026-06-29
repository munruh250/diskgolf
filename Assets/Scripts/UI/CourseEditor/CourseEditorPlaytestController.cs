using DiskGolf.Core;

using DiskGolf.CourseEditor;

using DiskGolf.UI;

using UnityEngine;



namespace DiskGolf.UI.CourseEditor

{

    public sealed class CourseEditorPlaytestController : MonoBehaviour

    {

        CourseEditorSession session;

        GameObject editorHudRoot;

        GameObject gameplayHudRoot;

        CourseEditorPlaytestOverlay overlay;

        DiskGolf.Input.ThrowInputHandler throwInput;

        ThrowController throwController;

        HUDController gameplayHudController;

        CourseEditorHudController editorHudController;



        public void Configure(

            CourseEditorSession sessionRef,

            GameObject editorHudRootRef,

            GameObject gameplayHudRootRef,

            CourseEditorPlaytestOverlay overlayRef,

            DiskGolf.Input.ThrowInputHandler throwInputRef,

            ThrowController throwControllerRef,

            HUDController gameplayHudControllerRef,

            CourseEditorHudController editorHudControllerRef)

        {

            session = sessionRef;

            editorHudRoot = editorHudRootRef;

            gameplayHudRoot = gameplayHudRootRef;

            overlay = overlayRef;

            throwInput = throwInputRef;

            throwController = throwControllerRef;

            gameplayHudController = gameplayHudControllerRef;

            editorHudController = editorHudControllerRef;

        }



        void Update()

        {

            if (session == null || session.Mode != CourseEditorSessionMode.Playtesting)

                return;



            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))

                ExitPlaytest();

        }



        public void EnterPlaytest()

        {

            ResolveGameplayRefs();

            SetGameplayComponentsEnabled(true);



            if (editorHudRoot != null)

                editorHudRoot.SetActive(false);

            if (gameplayHudRoot != null)

                gameplayHudRoot.SetActive(true);



            HudLayout.ForceApplyCanonicalLayout();
            MinimapUI.NotifyCourseRebuilt();

            overlay?.Show();

        }



        public void ExitPlaytest()

        {

            ResolveGameplayRefs();

            SetGameplayComponentsEnabled(false);



            if (editorHudRoot != null)

                editorHudRoot.SetActive(true);

            if (gameplayHudRoot != null)

                gameplayHudRoot.SetActive(false);



            if (session != null)

                session.Mode = CourseEditorSessionMode.Editing;



            var editorCamera = FindFirstObjectByType<CourseEditorCameraController>();

            editorCamera?.SetEditorCameraActive(true);

            editorCamera?.SetOverviewPreset();



            var setup = FindFirstObjectByType<HoleSetup>(FindObjectsInactive.Include);

            if (setup?.Thrower != null)

                setup.Thrower.gameObject.SetActive(false);



            overlay?.Hide();

            editorHudController?.OnExitPlaytest();

        }



        void SetGameplayComponentsEnabled(bool enabled)

        {

            if (throwInput != null)

                throwInput.enabled = enabled;

            if (throwController != null)

                throwController.enabled = enabled;

            if (gameplayHudController != null)

                gameplayHudController.enabled = enabled;

        }



        void ResolveGameplayRefs()

        {

            throwInput ??= FindFirstObjectByType<DiskGolf.Input.ThrowInputHandler>(FindObjectsInactive.Include);

            throwController ??= FindFirstObjectByType<ThrowController>(FindObjectsInactive.Include);

            gameplayHudController ??= FindFirstObjectByType<HUDController>(FindObjectsInactive.Include);

            editorHudController ??= FindFirstObjectByType<CourseEditorHudController>(FindObjectsInactive.Include);

        }

    }

}


