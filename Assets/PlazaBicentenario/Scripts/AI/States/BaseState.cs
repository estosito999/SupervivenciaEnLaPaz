// Adaptado e integrado para SupervivenciaEnLaPaz a partir de BaseState.cs de SanAndreasUnity.
using System;
using System.Collections.Generic;
using SupervivenciaEnLaPaz.Core;
using SupervivenciaEnLaPaz.Utilidades;
using UnityEngine;

namespace SupervivenciaEnLaPaz.AI.States
{
    /// <summary>
    /// Clase base para los estados de decisión de la IA de peatones (<see cref="PedAI"/>).
    /// </summary>
    public abstract class BaseState : IState
    {
        protected PedAI _pedAI { get; private set; }
        protected Ped _ped => _pedAI.MyPed;
        protected Ped MyPed => _pedAI.MyPed;
        protected List<Ped> _enemyPeds => _pedAI.EnemyPeds;

        public object ParameterForEnteringState { get; set; }
        public double LastTimeWhenActivated { get; set; }
        public double LastTimeWhenDeactivated { get; set; }

        protected internal virtual void OnAwake(PedAI pedAI)
        {
            _pedAI = pedAI;
        }

        public virtual void OnBecameActive() { }
        public virtual void OnBecameInactive() { }

        public bool RepresentsState(Type type)
        {
            if (type == null) return false;
            Type myType = GetType();
            return myType == type || myType.IsSubclassOf(type);
        }

        public bool RepresentsState<T>() where T : IState => RepresentsState(typeof(T));

        public virtual void UpdateState() { }
        public virtual void LateUpdateState() { }
        public virtual void FixedUpdateState() { }
        public virtual void UpdateState2Seconds() { }

        protected internal virtual void OnMyPedDamaged(DamageInfo dmgInfo, float cantidadAplicada)
        {
            if (dmgInfo?.Atacante != null && dmgInfo.Atacante.TryGetComponent(out Ped atacantePed))
                _enemyPeds.AddIfNotPresent(atacantePed);
        }

        protected internal virtual void OnOtherPedDamaged(Ped damagedPed, DamageInfo dmgInfo) { }

        protected internal virtual void OnCombateCercano(Vector3 centroPeligro, float radioReaccion) { }

        protected internal virtual void OnCombateTerminado() { }

        protected internal virtual void OnRecruit(Ped recruiterPed) { }

        protected internal virtual void OnDrawGizmosSelected() { }
    }
}
