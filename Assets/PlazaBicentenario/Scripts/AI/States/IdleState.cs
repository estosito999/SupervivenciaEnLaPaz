// Adaptado e integrado para SupervivenciaEnLaPaz a partir de IdleState.cs de SanAndreasUnity.
using SupervivenciaEnLaPaz.Core;
using SupervivenciaEnLaPaz.Utilidades;
using UnityEngine;

namespace SupervivenciaEnLaPaz.AI.States
{
    /// <summary>
    /// Estado pasivo o de descanso temporal en la plaza. Observa el entorno,
    /// cede espacio si el jugador se acerca demasiado y reanuda el paseo al cumplirse la pausa.
    /// </summary>
    public sealed class IdleState : BaseState
    {
        private float finReposo;
        private float siguienteMirada;
        private bool mirandoJugador;
        private bool observandoPelea;
        private Vector3 centroPeleaObservada;

        public override void OnBecameActive()
        {
            base.OnBecameActive();

            if (ParameterForEnteringState is float pausaPersonalizada && pausaPersonalizada >= 0f)
            {
                finReposo = Time.time + pausaPersonalizada;
                observandoPelea = false;
            }
            else if (ParameterForEnteringState is Vector3 centroPelea)
            {
                observandoPelea = true;
                centroPeleaObservada = centroPelea;
                finReposo = Time.time + Random.Range(1.2f, 2.5f);
            }
            else
            {
                observandoPelea = false;
                finReposo = Time.time + Random.Range(_pedAI.ReposoMinimo, _pedAI.ReposoMaximo);
            }

            siguienteMirada = Time.time + Random.Range(0.5f, 1.5f);
            mirandoJugador = false;
        }

        public override void UpdateState()
        {
            if (observandoPelea)
            {
                Vector3 haciaPelea = centroPeleaObservada - _ped.transform.position;
                haciaPelea.y = 0f;
                if (haciaPelea.sqrMagnitude > 0.01f)
                    _ped.DireccionMirada = haciaPelea.normalized;

                if (!CombateArena.EnCombate && Time.time >= finReposo)
                {
                    observandoPelea = false;
                    _pedAI.StartWalkingAround();
                }
                return;
            }

            if (Time.time >= finReposo)
            {
                _pedAI.StartWalkingAround();
                return;
            }

            if (Time.time >= siguienteMirada)
            {
                siguienteMirada = Time.time + Random.Range(1.8f, 4.5f);
                Ped jugador = Ped.JugadorPrincipal;
                mirandoJugador = jugador != null &&
                    PedAI.DistanciaHorizontal(_ped.transform.position, jugador.transform.position) < 6f &&
                    Random.value < 0.6f;
            }

            if (mirandoJugador && Ped.JugadorPrincipal != null)
            {
                Vector3 mirada = Ped.JugadorPrincipal.transform.position - _ped.transform.position;
                mirada.y = 0f;
                if (mirada.sqrMagnitude > 0.01f)
                    _ped.DireccionMirada = mirada.normalized;
            }

            // Si el jugador invade el espacio personal, el peatón da un paso lateral amable.
            if (Ped.JugadorPrincipal != null)
            {
                Vector3 empuje = _pedAI.EmpujeDesde(Ped.JugadorPrincipal.transform.position, 1.05f);
                if (empuje.sqrMagnitude > 0.02f)
                {
                    Vector3 dir = empuje.normalized;
                    _ped.IsWalkOn = true;
                    _ped.EntradaMovimiento = dir * 0.75f;
                    _ped.DireccionMirada = dir;
                }
            }
        }

        protected internal override void OnMyPedDamaged(DamageInfo dmgInfo, float cantidadAplicada)
        {
            ManejarDanoPropio(dmgInfo);
        }

        public void ManejarDanoPropio(DamageInfo dmgInfo)
        {
            base.OnMyPedDamaged(dmgInfo, dmgInfo?.Cantidad ?? 0f);

            Ped atacante = dmgInfo?.Atacante != null ? dmgInfo.Atacante.GetComponent<Ped>() : null;
            if ((_pedAI.Rol.EsHostil() || _pedAI.Rol.EsAutoridad()) && atacante != null)
            {
                _pedAI.StartChasing(atacante);
            }
            else
            {
                Vector3 peligro = atacante != null ? atacante.transform.position : _ped.transform.position;
                _pedAI.StartEscaping(peligro, _pedAI.RadioReaccionCombate);
            }
        }

        protected internal override void OnCombateCercano(Vector3 centroPeligro, float radioReaccion)
        {
            ManejarCombateCercano(centroPeligro, radioReaccion);
        }

        public void ManejarCombateCercano(Vector3 centroPeligro, float radioReaccion)
        {
            if (PedAI.DistanciaHorizontal(_ped.transform.position, centroPeligro) > radioReaccion)
                return;

            if (_pedAI.Rol.EsHostil() || _pedAI.Rol.EsAutoridad())
            {
                if (Ped.JugadorPrincipal != null && _pedAI.Rol.EsHostil())
                    _pedAI.StartChasing(Ped.JugadorPrincipal);
                return;
            }

            _pedAI.StartEscaping(centroPeligro, radioReaccion);
        }

        protected internal override void OnCombateTerminado()
        {
            if (observandoPelea)
            {
                observandoPelea = false;
                finReposo = Time.time + Random.Range(0.6f, 2f);
            }
        }

        protected internal override void OnRecruit(Ped recruiterPed)
        {
            ManejarReclutamiento(recruiterPed);
        }

        public void ManejarReclutamiento(Ped recruiterPed)
        {
            if (recruiterPed == null || recruiterPed == _ped)
                return;

            _pedAI.StartFollowing(recruiterPed);
        }
    }
}
