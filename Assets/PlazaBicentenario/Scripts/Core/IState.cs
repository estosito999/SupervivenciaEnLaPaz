// Adaptado e integrado para SupervivenciaEnLaPaz a partir de la arquitectura de estados de SanAndreasUnity.
using System;

namespace SupervivenciaEnLaPaz.Core
{
    /// <summary>
    /// Contrato base para estados manejados por <see cref="StateMachine"/>.
    /// </summary>
    public interface IState
    {
        object ParameterForEnteringState { get; set; }
        double LastTimeWhenActivated { get; set; }
        double LastTimeWhenDeactivated { get; set; }

        void OnBecameActive();
        void OnBecameInactive();

        bool RepresentsState(Type type);
        bool RepresentsState<T>() where T : IState;

        void UpdateState();
        void LateUpdateState();
        void FixedUpdateState();
    }
}
