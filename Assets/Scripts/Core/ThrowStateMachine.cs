using System;

namespace DiskGolf.Core
{
    public class ThrowStateMachine
    {
        public ThrowPhase Phase { get; private set; } = ThrowPhase.Aiming;
        public event Action<ThrowPhase> PhaseChanged;

        public void TransitionTo(ThrowPhase phase)
        {
            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }

        public void Advance()
        {
            TransitionTo(Phase switch
            {
                ThrowPhase.Aiming => ThrowPhase.PowerMeter,
                ThrowPhase.PowerMeter => ThrowPhase.HeightMeter,
                ThrowPhase.HeightMeter => ThrowPhase.Throwing,
                ThrowPhase.Throwing => ThrowPhase.InFlight,
                ThrowPhase.InFlight => ThrowPhase.Landed,
                ThrowPhase.Landed => ThrowPhase.Resolve,
                ThrowPhase.Putting => ThrowPhase.Resolve,
                ThrowPhase.Resolve => ThrowPhase.Aiming,
                _ => ThrowPhase.Aiming
            });
        }

        public void EnterPutting() => TransitionTo(ThrowPhase.Putting);
    }
}
