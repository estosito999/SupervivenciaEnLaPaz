using SupervivenciaEnLaPaz.Core;
using UnityEngine;

namespace SupervivenciaEnLaPaz.Movimiento.States
{
    /// <summary>
    /// Estado de retroceso e impacto cuando el personaje recibe un golpe real.
    /// </summary>
    public sealed class HitReactionState : BaseMovementState
    {
        private Vector3 direccionRetroceso;
        private float tiempoTranscurrido;

        public override float TiempoMinimoSalida => 0f;
        public override float TiempoMinimoEntrada => 0f;
        public override bool PermiteGiro => false;
        public override bool PermiteAtaque => false;
        public override bool PermiteEntradaMovimiento => false;
        public override RecorridoPlaza.EstadoLocomocion EstadoLegacy => RecorridoPlaza.EstadoLocomocion.Golpeado;
        public override string NombreEstadoHud => "Impacto";

        public override void OnBecameActive()
        {
            base.OnBecameActive();
            tiempoTranscurrido = 0f;

            if (m_ped == null)
                return;

            Vector3 dir = -m_ped.transform.forward;
            if (ParameterForEnteringState is DamageInfo info && info.DireccionImpacto.sqrMagnitude > 0.001f)
            {
                dir = info.DireccionImpacto;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    dir.Normalize();
                else
                    dir = -m_ped.transform.forward;
            }

            direccionRetroceso = dir;
            m_ped.EstablecerDesplazamientoDirecto(direccionRetroceso * 2.1f);
            m_model?.DispararImpacto();
        }

        public override void OnBecameInactive()
        {
            m_model?.LimpiarPoseTemporal();
            base.OnBecameInactive();
        }

        public override float CalcularVelocidadObjetivo() => 0f;

        protected override void EvaluarTransiciones()
        {
            if (m_ped == null)
                return;

            float duracion = Mathf.Max(0.12f, m_ped.GestorConfig.duracionReaccionImpacto);
            tiempoTranscurrido += Time.deltaTime;
            float progreso = Mathf.Clamp01(tiempoTranscurrido / duracion);
            m_ped.EstablecerDesplazamientoDirecto(direccionRetroceso * Mathf.Lerp(2.1f, 0f, progreso));

            if (tiempoTranscurrido >= duracion)
            {
                System.Type siguiente = ObtenerEstadoSiguienteSegunEntrada(m_ped);
                if (siguiente == typeof(HitReactionState))
                    siguiente = typeof(StandState);
                m_ped.CambiarEstado(siguiente);
            }
        }

        protected override void ActualizarAnimaciones()
        {
            base.ActualizarAnimaciones();
            if (m_model == null || m_ped == null)
                return;

            float duracion = Mathf.Max(0.12f, m_ped.GestorConfig.duracionReaccionImpacto);
            float progreso = Mathf.Clamp01(tiempoTranscurrido / duracion);
            m_model.AplicarRetrocesoImpacto(progreso);
        }

        public override void AlRecibirDano(DamageInfo info) { }
        public override void AlPulsarSalto() { }
        public override void AlPulsarRodar(bool izquierda) { }
    }
}
