using Cinemachine;
using DiskGolf.Camera;
using DiskGolf.CourseEditor;
using UnityEngine;

namespace DiskGolf.UI.CourseEditor
{
    [DefaultExecutionOrder(100)]
    public sealed class CourseEditorCameraController : MonoBehaviour
    {
        const float OrbitSpeed = 3.2f;
        const float PanSpeed = 0.03f;
        const float ZoomSpeed = 12f;
        const float MinPitch = 10f;
        const float MaxPitch = 88f;
        const float MinDistance = 8f;
        const float MaxDistance = 280f;
        const float DefaultDistance = 70f;

        Transform cameraTransform;
        CinemachineBrain cinemachineBrain;
        CameraDirector cameraDirector;
        CinemachineVirtualCamera[] virtualCameras;
        float yaw;
        float pitch = 35f;
        float distance = DefaultDistance;
        Vector3 pivot = Vector3.zero;
        CourseEditorSession session;
        bool editorCameraActive = true;

        void Awake()
        {
            session = CourseEditorSession.Instance;
            CacheCameraRefs();
            SetEditorCameraActive(true);
            SetOverviewPreset();
        }

        void Start()
        {
            CacheCameraRefs();
            SetEditorCameraActive(true);
            ApplyCameraPose();
        }

        void CacheCameraRefs()
        {
            var main = UnityEngine.Camera.main;
            if (main != null)
                cameraTransform = main.transform;

            cinemachineBrain ??= main != null ? main.GetComponent<CinemachineBrain>() : null;
            cameraDirector ??= FindFirstObjectByType<CameraDirector>();
            if (virtualCameras == null || virtualCameras.Length == 0)
                virtualCameras = FindObjectsByType<CinemachineVirtualCamera>(FindObjectsSortMode.None);
        }

        void Update()
        {
            if (!editorCameraActive || session?.Mode == CourseEditorSessionMode.Playtesting)
                return;

            if (cameraTransform == null)
            {
                CacheCameraRefs();
                if (cameraTransform == null)
                    return;
            }

            HandleZoom();
            HandleOrbitOrPan();
        }

        void LateUpdate()
        {
            if (!editorCameraActive || session?.Mode == CourseEditorSessionMode.Playtesting)
                return;

            ApplyCameraPose();
        }

        public void SetEditorCameraActive(bool active)
        {
            editorCameraActive = active;

            if (cinemachineBrain != null)
                cinemachineBrain.enabled = !active;

            if (cameraDirector != null)
                cameraDirector.enabled = !active;

            if (virtualCameras == null || virtualCameras.Length == 0)
                virtualCameras = FindObjectsByType<CinemachineVirtualCamera>(FindObjectsSortMode.None);

            foreach (var vcam in virtualCameras)
            {
                if (vcam != null)
                    vcam.enabled = !active;
            }

            if (active)
                ApplyCameraPose();
        }

        public void SetOverviewPreset()
        {
            SetEditorCameraActive(true);
            pivot = ComputeCourseCenter();
            pitch = 42f;
            yaw = ComputeHoleYaw();
            distance = Mathf.Clamp(ComputeHoleLength() * 0.35f, 50f, 200f);
            ApplyCameraPose();
        }

        public void SetTeePreset()
        {
            SetEditorCameraActive(true);
            var hole = session?.Hole;
            if (hole == null)
            {
                SetOverviewPreset();
                return;
            }

            pivot = MarkerWorld(hole.Hole.Tee);
            yaw = ComputeYawToward(pivot, MarkerWorld(hole.Hole.Basket));
            pitch = 28f;
            distance = 32f;
            ApplyCameraPose();
        }

        public void SetBasketPreset()
        {
            SetEditorCameraActive(true);
            var hole = session?.Hole;
            if (hole == null)
            {
                SetOverviewPreset();
                return;
            }

            pivot = MarkerWorld(hole.Hole.Basket);
            yaw = ComputeYawToward(pivot, MarkerWorld(hole.Hole.Tee));
            pitch = 22f;
            distance = 28f;
            ApplyCameraPose();
        }

        public void SetTopDownPreset()
        {
            SetEditorCameraActive(true);
            pivot = ComputeCourseCenter();
            pitch = 88f;
            yaw = 0f;
            distance = Mathf.Clamp(ComputeHoleLength() * 0.45f, 60f, 220f);
            ApplyCameraPose();
        }

        float ComputeHoleYaw()
        {
            var hole = session?.Hole;
            if (hole == null)
                return 0f;

            return ComputeYawToward(MarkerWorld(hole.Hole.Tee), MarkerWorld(hole.Hole.Basket));
        }

        static float ComputeYawToward(Vector3 from, Vector3 toward)
        {
            var flat = toward - from;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.01f)
                return 0f;

            return Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
        }

        float ComputeHoleLength()
        {
            var hole = session?.Hole;
            if (hole == null)
                return DefaultDistance;

            var a = MarkerWorld(hole.Hole.Tee);
            var b = MarkerWorld(hole.Hole.Basket);
            return Vector3.Distance(a, b);
        }

        void HandleZoom()
        {
            float wheel = UnityEngine.Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) < Mathf.Epsilon)
                return;

            distance = Mathf.Clamp(distance - wheel * ZoomSpeed, MinDistance, MaxDistance);
        }

        void HandleOrbitOrPan()
        {
            if (!UnityEngine.Input.GetMouseButton(2))
                return;

            if (UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift))
            {
                Pan(UnityEngine.Input.GetAxis("Mouse X"), UnityEngine.Input.GetAxis("Mouse Y"));
                return;
            }

            yaw += UnityEngine.Input.GetAxis("Mouse X") * OrbitSpeed;
            pitch -= UnityEngine.Input.GetAxis("Mouse Y") * OrbitSpeed;
            pitch = Mathf.Clamp(pitch, MinPitch, MaxPitch);
        }

        void Pan(float mouseX, float mouseY)
        {
            if (cameraTransform == null)
                return;

            Vector3 right = cameraTransform.right;
            right.y = 0f;
            right.Normalize();

            Vector3 forward = Vector3.Cross(Vector3.up, right).normalized;
            float panScale = Mathf.Max(1f, distance * PanSpeed);
            pivot += (-right * mouseX + -forward * mouseY) * panScale;
        }

        void ApplyCameraPose()
        {
            if (cameraTransform == null)
                return;

            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            cameraTransform.position = pivot - rotation * Vector3.forward * distance;
            cameraTransform.rotation = rotation;
        }

        Vector3 ComputeCourseCenter()
        {
            var hole = session?.Hole;
            if (hole == null)
                return Vector3.zero;

            if (hole.Hole.Tee != Vector2.zero && hole.Hole.Basket != Vector2.zero)
            {
                var tee = MarkerWorld(hole.Hole.Tee);
                var basket = MarkerWorld(hole.Hole.Basket);
                return (tee + basket) * 0.5f;
            }

            var bounds = hole.ComputeEditorWorldBounds();
            float y = HeightGridSampler.SampleWorldY(hole, bounds.center.x, bounds.center.z);
            return new Vector3(bounds.center.x, y, bounds.center.z);
        }

        Vector3 MarkerWorld(Vector2 marker)
        {
            var hole = session?.Hole;
            if (hole == null)
                return Vector3.zero;

            float y = HeightGridSampler.SampleWorldY(hole, marker.x, marker.y);
            return new Vector3(marker.x, y, marker.y);
        }
    }
}
