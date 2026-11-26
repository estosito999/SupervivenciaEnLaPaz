// Adaptado e integrado para SupervivenciaEnLaPaz a partir de WalkAroundState.cs de SanAndreasUnity.
using SupervivenciaEnLaPaz.Core;
using SupervivenciaEnLaPaz.Utilidades;
using UnityEngine;

namespace SupervivenciaEnLaPaz.AI.States
{
    /// <summary>
    /// Estado de paseo autónomo por la malla de navegación de la Plaza del Bicentenario.
    /// Elige destinos válidos en NavMesh, esquiva obstáculos y otros personajes y descansa al llegar.
    /// </summary>
    public sealed class WalkAroundState : BaseState, IPathMovementState
    {
        private readonly PathMovementData _pathMovementData = new();
        public PathMovementData PathMovementData => _pathMovementData;

        public override void OnBecameActive()
        {
            base.OnBecameActive();

            if (ParameterForEnteringState is Vector3 destinoExplicito)
            {
                _pathMovementData.Limpiar();
                _pathMovementData.DestinoActual = destinoExplicito;
                _pathMovementData.CalcularRuta(_ped.transform.position, destinoExplicito, _pedAI.Navegacion);
            }
            else
            {
                _pathMovementData.Limpiar();
                ElegirNuevoDestino();
            }
        }

        public override void OnBecameInactive()
        {
            _pathMovementData.Limpiar();
            base.OnBecameInactive();
        }

        public override void UpdateState()
        {
            _enemyPeds.RemoveNullOrDestroyed();
            if (_enemyPeds.Count > 0 && (_pedAI.Rol.EsHostil() || _pedAI.Rol.EsAutoridad()))
            {
                _pedAI.StartChasing(_enemyPeds[0]);
                return;
            }

            if (!_pathMovementData.TieneRuta || !_pathMovementData.DestinoActual.HasValue || Time.time >= _pathMovementData.ProximoRecalculo)
            {
                if (_pathMovementData.DestinoActual.HasValue)
                {
                    if (!_pathMovementData.CalcularRuta(_ped.transform.position, _pathMovementData.DestinoActual.Value, _pedAI.Navegacion))
                    {
                        if (Time.time >= _pathMovementData.TiempoUltimoIntentoDestino)
                        {
                            _pathMovementData.TiempoUltimoIntentoDestino = Time.time + 0.8f;
                            if (!ElegirNuevoDestino())
                            {
                                _pedAI.StartIdling(Random.Range(0.8f, 1.8f));
                                return;
                            }
                        }
                    }
                }
                else if (!ElegirNuevoDestino())
                {
                    _pedAI.StartIdling(Random.Range(0.8f, 1.8f));
                    return;
                }
            }

            Vector3 direccion = _pathMovementData.ObtenerDireccionHorizontal(_ped.transform.position, _pedAI.DistanciaLlegada);
            if (direccion.sqrMagnitude < 0.0001f)
            {
                _pedAI.StartIdling(Random.Range(_pedAI.ReposoMinimo, _pedAI.ReposoMaximo));
                return;
            }

            Vector3 separacion = _pedAI.CalcularSeparacion();
            if (separacion.sqrMagnitude > 0.0001f)
                direccion = (direccion.normalized + separacion * _pedAI.EmpujeSeparacion).normalized;

            _ped.IsWalkOn = true;
            _ped.EntradaMovimiento = direccion;
            _ped.DireccionMirada = direccion;

            ComprobarAtasco();
        }

        private bool ElegirNuevoDestino()
        {
            if (_pedAI.BuscarDestinoPaseo(out Vector3 destino))
            {
                if (_pathMovementData.CalcularRuta(_ped.transform.position, destino, _pedAI.Navegacion))
                    return true;
            }

            return false;
        }

        private void ComprobarAtasco()
        {
            if (Time.time < _pathMovementData.ProximoChequeoAtasco)
                return;

            float avance = PedAI.DistanciaHorizontal(_ped.transform.position, _pathMovementData.UltimaPosicionAtasco);
            if (avance < 0.18f)
            {
                _pathMovementData.IntentosAtasco++;
                if (_pathMovementData.IntentosAtasco >= 3)
                {
                    _pathMovementData.IntentosAtasco = 0;
                    _pedAI.SolicitarReubicacion();
                }
                else
                {
                    ElegirNuevoDestino();
                }
            }
            else
            {
                _pathMovementData.IntentosAtasco = 0;
            }

            _pathMovementData.UltimaPosicionAtasco = _ped.transform.position;
            _pathMovementData.ProximoChequeoAtasco = Time.time + 1.2f;
        }

        protected internal override void OnMyPedDamaged(DamageInfo dmgInfo, float cantidadAplicada)
        {
            _pedAI.StateContainer.GetStateOrThrow<IdleState>().ManejarDanoPropio(dmgInfo);
        }

        protected internal override void OnCombateCercano(Vector3 centroPeligro, float radioReaccion)
        {
            _pedAI.StateContainer.GetStateOrThrow<IdleState>().ManejarCombateCercano(centroPeligro, radioReaccion);
        }

        protected internal override void OnRecruit(Ped recruiterPed)
        {
            _pedAI.StateContainer.GetStateOrThrow<IdleState>().ManejarReclutamiento(recruiterPed);
        }

        protected internal override void OnDrawGizmosSelected()
        {
            if (_pathMovementData.TieneRuta && _pathMovementData.DestinoActual.HasValue)
            {
                Gizmos.color = new Color(0.4f, 0.9f, 0.7f);
                Gizmos.DrawLine(_ped.transform.position, _pathMovementData.DestinoActual.Value);
                Gizmos.DrawWireSphere(_pathMovementData.DestinoActual.Value, 0.35f);
            }
        }
    }
}
