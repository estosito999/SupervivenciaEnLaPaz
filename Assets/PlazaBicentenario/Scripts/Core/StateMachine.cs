// Adaptado e integrado para SupervivenciaEnLaPaz a partir de la arquitectura de estados de SanAndreasUnity.
using System;
using UnityEngine;

namespace SupervivenciaEnLaPaz.Core
{
    /// <summary>
    /// Máquina de estados finitos con seguimiento temporal de activación/desactivación.
    /// </summary>
    public sealed class StateMachine
    {
        public IState CurrentState { get; private set; }
        public IState PreviousState { get; private set; }

        public event Action<IState, IState> OnStateChanged;

        public bool SwitchState(IState newState)
        {
            return SwitchStateWithParameter(newState, null);
        }

        public bool SwitchStateWithParameter(IState newState, object parameter)
        {
            if (newState == null || ReferenceEquals(CurrentState, newState))
                return false;

            IState oldState = CurrentState;
            double now = Time.timeAsDouble;

            if (oldState != null)
            {
                oldState.LastTimeWhenDeactivated = now;
                oldState.OnBecameInactive();
            }

            PreviousState = oldState;
            CurrentState = newState;
            newState.ParameterForEnteringState = parameter;
            newState.LastTimeWhenActivated = now;
            newState.OnBecameActive();

            OnStateChanged?.Invoke(oldState, newState);
            return true;
        }
    }
}
