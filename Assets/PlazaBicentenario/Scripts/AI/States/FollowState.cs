// Adaptado e integrado para SupervivenciaEnLaPaz a partir de FollowState.cs de SanAndreasUnity.
using SupervivenciaEnLaPaz.Core;
using UnityEngine;

namespace SupervivenciaEnLaPaz.AI.States
{
    /// <summary>
    /// Estado de acompañamiento/escolta para seguir a otro personaje manteniendo distancia segura.
    /// </summary>
    public sealed class FollowState : BaseState, IPathMovementState
    {
        private readonly PathMovementData _pathMovementData = new();
        public PathMovementData PathMovementData => _pathMovementData;

        public Ped LeaderPed { get; private set; }
        public float DistanciaParada { get; set; } = 2.6f;
        public float DistanciaCarrera { get; set; } = 6.0f;

        public override void OnBecameActive()
        {
            base.OnBecameActive();
            _pathMovementData.Limpiar();
            LeaderPed = ParameterForEnteringState as Ped;
        }

        public override void OnBecameInactive()
        {
            LeaderPed = null;
            _pathMovementData.Limpiar();
            base.OnBecameInactive();
        }

        public override void UpdateState()
        {
            if (LeaderPed == null || !LeaderPed.gameObject.activeInHierarchy)
            {
                _pedAI.StartIdling();
                return;
            }

            float distancia = PedAI.DistanciaHorizontal(_ped.transform.position, LeaderPed.transform.position);
            Vector3 haciaLider = LeaderPed.transform.position - _ped.transform.position;
            haciaLider.y = 0f;

            if (distancia <= DistanciaParada)
            {
                if (haciaLider.sqrMagnitude > 0.001f)
                    _ped.DireccionMirada = haciaLider.normalized;
                return;
            }

            if (!_pathMovementData.TieneRuta || Time.time >= _pathMovementData.ProximoRecalculo)
            {
                _pathMovementData.CalcularRuta(_ped.transform.position, LeaderPed.transform.position, _pedAI.Navegacion);
                _pathMovementData.ProximoRecalculo = Time.time + 0.45f;
            }

            Vector3 dir = _pathMovementData.ObtenerDireccionHorizontal(_ped.transform.position, 0.35f);
            if (dir.sqrMagnitude < 0.0001f && haciaLider.sqrMagnitude > 0.001f)
                dir = haciaLider.normalized;

            Vector3 separacion = _pedAI.CalcularSeparacion();
            if (separacion.sqrMagnitude > 0.0001f)
                dir = (dir + separacion * _pedAI.EmpujeSeparacion).normalized;

            if (distancia > DistanciaCarrera)
                _ped.IsRunOn = true;
            else
                _ped.IsWalkOn = true;

            _ped.EntradaMovimiento = dir;
            _ped.DireccionMirada = dir;
        }

        protected internal override void OnMyPedDamaged(DamageInfo dmgInfo, float cantidadAplicada)
        {
            Ped atacante = dmgInfo?.Atacante != null ? dmgInfo.Atacante.GetComponent<Ped>() : null;
            if (atacante == LeaderPed)
            {
                LeaderPed = null;
                _pedAI.StartEscaping(atacante != null ? atacante.transform.position : _ped.transform.position, _pedAI.RadioReaccionCombate);
            }
        }

        protected internal override void OnRecruit(Ped recruiterPed)
        {
            if (LeaderPed == recruiterPed)
            {
                LeaderPed = null;
                _pedAI.StartWalkingAround();
            }
        }
    }
}
