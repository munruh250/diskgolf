using DiskGolf.Flight;
using UnityEngine;

namespace DiskGolf.Input
{
    public class ThrowInputHandler : MonoBehaviour
    {
        public ReleaseAngle ReleaseAngle { get; private set; } = ReleaseAngle.Flat;

        public ThrowHeight ArcHeight { get; private set; } = ThrowHeight.Nice;

        public void ResetArcHeight() => ArcHeight = ThrowHeight.Nice;

        public void CycleArcHeight()
        {
            ArcHeight = ArcHeight switch
            {
                ThrowHeight.Nice => ThrowHeight.High,
                ThrowHeight.High => ThrowHeight.Low,
                _ => ThrowHeight.Nice,
            };
        }

        public void CycleReleaseAngle()
        {
            ReleaseAngle = ReleaseAngle switch
            {
                ReleaseAngle.Flat => ReleaseAngle.Hyzer,
                ReleaseAngle.Hyzer => ReleaseAngle.Anhyzer,
                _ => ReleaseAngle.Flat,
            };
        }

        void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Z)) ReleaseAngle = ReleaseAngle.Hyzer;

            if (UnityEngine.Input.GetKeyDown(KeyCode.X)) ReleaseAngle = ReleaseAngle.Flat;

            if (UnityEngine.Input.GetKeyDown(KeyCode.C)) ReleaseAngle = ReleaseAngle.Anhyzer;
        }

        public bool ConfirmPressed => UnityEngine.Input.GetKeyDown(KeyCode.Space);

        public bool ResetPressed => UnityEngine.Input.GetKeyDown(KeyCode.R);

        public bool AimLeft => UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow);

        public bool AimRight => UnityEngine.Input.GetKeyDown(KeyCode.RightArrow);

        public bool AimUp => UnityEngine.Input.GetKeyDown(KeyCode.UpArrow);

        public bool AimDown => UnityEngine.Input.GetKeyDown(KeyCode.DownArrow);

        public int DiscHotkey =>
            UnityEngine.Input.GetKeyDown(KeyCode.Alpha1) ? 0 :
            UnityEngine.Input.GetKeyDown(KeyCode.Alpha2) ? 1 :
            UnityEngine.Input.GetKeyDown(KeyCode.Alpha3) ? 2 :
            UnityEngine.Input.GetKeyDown(KeyCode.Alpha4) ? 3 : -1;

        public bool CycleNext => UnityEngine.Input.GetKeyDown(KeyCode.E);

        public bool CyclePrev => UnityEngine.Input.GetKeyDown(KeyCode.Q);
    }
}
