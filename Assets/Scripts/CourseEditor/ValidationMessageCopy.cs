using DiskGolf.CourseEditor.Authoring;

namespace DiskGolf.CourseEditor
{
    public enum CourseEditorCameraPreset
    {
        None,
        Overview,
        Tee,
        Basket,
        TopDown
    }

    public readonly struct ValidationFocusHint
    {
        public readonly CourseAuthoringTool? SelectTool;
        public readonly SurfaceTileType? SelectBrush;
        public readonly CourseEditorCameraPreset CameraPreset;

        public ValidationFocusHint(
            CourseAuthoringTool? selectTool = null,
            SurfaceTileType? selectBrush = null,
            CourseEditorCameraPreset cameraPreset = CourseEditorCameraPreset.None)
        {
            SelectTool = selectTool;
            SelectBrush = selectBrush;
            CameraPreset = cameraPreset;
        }

        public static ValidationFocusHint None => default;
    }

    public static class ValidationMessageCopy
    {
        public static string ForCode(string code)
        {
            return code switch
            {
                "E001" => "Place the basket on the course.",
                "E002" => "Place the tee pad.",
                "E003" => "Paint some fairway (or rough) tiles first.",
                "E004" => "Move the basket onto the green or fairway.",
                "W001" => "The tee isn't on a tee or fairway tile.",
                "W003" => "Fix the hazard outline — lines can't cross.",
                "W004" => "Some decorations are using placeholder art.",
                _ => "Something needs attention before you can playtest."
            };
        }

        public static ValidationFocusHint GetFocusHint(string code)
        {
            return code switch
            {
                "E001" => new ValidationFocusHint(
                    CourseAuthoringTool.HoleBasket,
                    cameraPreset: CourseEditorCameraPreset.Overview),
                "E002" => new ValidationFocusHint(CourseAuthoringTool.HoleTee),
                "E003" => new ValidationFocusHint(
                    CourseAuthoringTool.Paint,
                    SurfaceTileType.Fairway),
                "E004" => new ValidationFocusHint(
                    cameraPreset: CourseEditorCameraPreset.Basket),
                "W001" => new ValidationFocusHint(
                    cameraPreset: CourseEditorCameraPreset.Tee),
                "W003" => new ValidationFocusHint(
                    CourseAuthoringTool.Hazard,
                    cameraPreset: CourseEditorCameraPreset.TopDown),
                "W004" => ValidationFocusHint.None,
                _ => ValidationFocusHint.None
            };
        }
    }
}
