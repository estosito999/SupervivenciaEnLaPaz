// Adaptado e integrado para SupervivenciaEnLaPaz a partir de EscapeState.cs de SanAndreasUnity.
using UnityEngine;

namespace SupervivenciaEnLaPaz.AI.States
{
    /// <summary>
    /// Estado de huida en pánico cuando el peatón detecta combate o peligro cercano.
    /// Usa NavMesh para alejarse del foco de peligro y recupera su rutina cuando pasa la amenaza.
    /// </summary>
    public sealed class EscapeState : BaseState, IPathMovementState
    {
        private readonly PathMovementData _pathMovementData = new();
        public PathMovementData PathMovementData => _pathMovementData;

        private float finHuida;
        private bool combateActivo;

        public override void OnBecameActive()
        {
            base.OnBecameActive();
            _pathMovementData.Limpiar();

            Vector3 peligro = ParameterForEnteringState is Vector3 p ? p : _ped.transform.position;
            _pathMovementData.CentroPeligro = peligro;
            combateActivo = CombateArena.EnCombate;
            finHuida = Time.time + _pedAI.DuracionHuida;

            if (!PrepararRutaHuida(peligro))
            {
                _pedAI.SwitchStateWithParameter<IdleState>(peligro);
            }
        }

        public override void OnBecameInactive()
        {
            _pathMovementData.Limpiar();
            base.OnBecameInactive();
        }

        private bool PrepararRutaHuida(Vector3 peligro)
        {
            if (_pedAI.BuscarPuntoHuida(peligro, out Vector3 destino))
                return _pathMovementData.CalcularRuta(_ped.transform.position, destino, _pedAI.Navegacion);

            Vector3 alejamiento = _ped.transform.position - peligro;
            alejamiento.y = 0f;
            if (alejamiento.sqrMagnitude < 0.01f)
                alejamiento = Random.insideUnitSphere;

            Vector3 candidato = peligro + alejamiento.normalized * (_pedAI.RadioReaccionCombate + 4f);
            if (_pedAI.Navegacion != null && _pedAI.Navegacion.Muestrear(candidato, out Vector3 muestreado, 4f))
                return _pathMovementData.CalcularRuta(_ped.transform.position, muestreado, _pedAI.Navegacion);

            return false;
        }

        public override void UpdateState()
        {
            if (Time.time >= finHuida)
            {
                if (combateActivo)
                    _pedAI.SwitchStateWithParameter<IdleState>(_pathMovementData.CentroPeligro);
                else
                    _pedAI.StartIdling(Random.Range(1.0f, 2.5f));
                return;
            }

            if (!_pathMovementData.TieneRuta || (_pathMovementData.DestinoActual.HasValue && Time.time >= _pathMovementData.ProximoRecalculo))
            {
                if (_pathMovementData.DestinoActual.HasValue)
                    _pathMovementData.CalcularRuta(_ped.transform.position, _pathMovementData.DestinoActual.Value, _pedAI.Navegacion);
            }

            Vector3 direccion = _pathMovementData.ObtenerDireccionHorizontal(_ped.transform.position, _pedAI.DistanciaLlegada);
            if (direccion.sqrMagnitude < 0.0001f)
            {
                if (combateActivo)
                    _pedAI.SwitchStateWithParameter<IdleState>(_pathMovementData.CentroPeligro);
                else
                    _pedAI.StartIdling(Random.Range(1.0f, 2.5f));
                return;
            }

            Vector3 separacion = _pedAI.CalcularSeparacion();
            if (separacion.sqrMagnitude > 0.0001f)
                direccion = (direccion.normalized + separacion * _pedAI.EmpujeSeparacion).normalized;

            _ped.IsPanicOn = true;
            _ped.EntradaMovimiento = direccion;
            _ped.DireccionMirada = direccion;
        }

        protected internal override void OnCombateTerminado()
        {
            combateActivo = false;
            finHuida = Mathf.Min(finHuida, Time.time + 0.6f);
        }

        protected internal override void OnDrawGizmosSelected()
        {
            if (_pathMovementData.TieneRuta && _pathMovementData.DestinoActual.HasValue)
            {
                Gizmos.color = new Color(1f, 0.45f, 0.35f);
                Gizmos.DrawLine(_ped.transform.position, _pathMovementData.DestinoActual.Value);
                Gizmos.DrawWireSphere(_pathMovementData.DestinoActual.Value, 0.35f);
            }
        }
    }
}
