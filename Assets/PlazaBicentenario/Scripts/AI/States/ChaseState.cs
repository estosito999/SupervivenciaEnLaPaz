// Adaptado e integrado para SupervivenciaEnLaPaz a partir de ChaseState.cs de SanAndreasUnity.
using SupervivenciaEnLaPaz.Utilidades;
using UnityEngine;

namespace SupervivenciaEnLaPaz.AI.States
{
    /// <summary>
    /// Estado de persecución sobre el NavMesh hacia un objetivo (jugador o rival).
    /// </summary>
    public sealed class ChaseState : BaseState, IPathMovementState
    {
        private readonly PathMovementData _pathMovementData = new();
        public PathMovementData PathMovementData => _pathMovementData;

        public Ped TargetPed { get; private set; }
        public float DistanciaParada { get; set; } = 1.2f;
        public float DistanciaAbandono { get; set; } = 28f;

        public override void OnBecameActive()
        {
            base.OnBecameActive();
            _pathMovementData.Limpiar();

            if (ParameterForEnteringState is Ped target)
            {
                TargetPed = target;
                _enemyPeds.AddIfNotPresent(target);
            }
            else
            {
                TargetPed = ObtenerSiguienteObjetivo();
            }
        }

        public override void OnBecameInactive()
        {
            TargetPed = null;
            _pathMovementData.Limpiar();
            base.OnBecameInactive();
        }

        public override void UpdateState2Seconds()
        {
            _enemyPeds.RemoveNullOrDestroyed();
            if (TargetPed == null || !TargetPed.gameObject.activeInHierarchy)
                TargetPed = ObtenerSiguienteObjetivo();
        }

        public override void UpdateState()
        {
            if (TargetPed == null || !TargetPed.gameObject.activeInHierarchy)
            {
                _pedAI.StartWalkingAround();
                return;
            }

            float distancia = PedAI.DistanciaHorizontal(_ped.transform.position, TargetPed.transform.position);
            if (distancia > DistanciaAbandono)
            {
                _enemyPeds.Remove(TargetPed);
                TargetPed = null;
                _pedAI.StartWalkingAround();
                return;
            }

            Vector3 haciaObjetivo = TargetPed.transform.position - _ped.transform.position;
            haciaObjetivo.y = 0f;

            if (distancia <= DistanciaParada)
            {
                if (haciaObjetivo.sqrMagnitude > 0.001f)
                    _ped.DireccionMirada = haciaObjetivo.normalized;
                return;
            }

            if (!_pathMovementData.TieneRuta || Time.time >= _pathMovementData.ProximoRecalculo || _pathMovementData.ProximoRecalculo <= 0f)
            {
                _pathMovementData.CalcularRuta(_ped.transform.position, TargetPed.transform.position, _pedAI.Navegacion);
                _pathMovementData.ProximoRecalculo = Time.time + 0.35f;
            }

            Vector3 dir = _pathMovementData.ObtenerDireccionHorizontal(_ped.transform.position, 0.25f);
            if (dir.sqrMagnitude < 0.0001f && haciaObjetivo.sqrMagnitude > 0.001f)
                dir = haciaObjetivo.normalized;

            Vector3 separacion = _pedAI.CalcularSeparacion();
            if (separacion.sqrMagnitude > 0.0001f)
                dir = (dir + separacion * (_pedAI.EmpujeSeparacion * 0.6f)).normalized;

            _ped.IsRunOn = true;
            _ped.EntradaMovimiento = dir;
            _ped.DireccionMirada = dir;
        }

        private Ped ObtenerSiguienteObjetivo()
        {
            _enemyPeds.RemoveNullOrDestroyed();
            Ped mejor = null;
            float menorDist = float.PositiveInfinity;
            for (int i = 0; i < _enemyPeds.Count; i++)
            {
                Ped p = _enemyPeds[i];
                if (p == null || !p.gameObject.activeInHierarchy)
                    continue;

                float d = (_ped.transform.position - p.transform.position).sqrMagnitude;
                if (d < menorDist)
                {
                    menorDist = d;
                    mejor = p;
                }
            }
            return mejor;
        }
    }
}
