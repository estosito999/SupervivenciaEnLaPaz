using UnityEngine;

namespace SupervivenciaEnLaPaz.Movimiento.States
{
    /// <summary>
    /// Esquiva rápida lateral o hacia atrás para combate cuerpo a cuerpo y exploración.
    /// Otorga ventana breve de invulnerabilidad real frente a golpes rivales.
    /// </summary>
    public sealed class DodgeState : BaseMovementState
    {
        private Vector3 direccionEsquiva;
        private float tiempoTranscurrido;

        public override float TiempoMinimoSalida => 0f;
        public override float TiempoMinimoEntrada => 0.14f;
        public override bool PermiteGiro => false;
        public override bool PermiteAtaque => false;
        public override bool PermiteEntradaMovimiento => false;
        public override RecorridoPlaza.EstadoLocomocion EstadoLegacy => RecorridoPlaza.EstadoLocomocion.Esquivar;
        public override string NombreEstadoHud => "Esquivar";

        public bool PuedeEsquivar()
        {
            if (IsActiveState || m_ped == null || !m_ped.EnSuelo)
                return false;

            return m_ped.Resistencia >= m_ped.GestorConfig.consumoEsquiva * 0.5f;
        }

        public bool IniciarEsquiva(Vector3 direccionMundo)
        {
            if (!PuedeEsquivar())
                return false;

            m_ped.ConsumirResistencia(m_ped.GestorConfig.consumoEsquiva);
            m_ped.CambiarEstadoConParametro<DodgeState>(direccionMundo);
            return true;
        }

        public override void OnBecameActive()
        {
            base.OnBecameActive();
            tiempoTranscurrido = 0f;

            if (m_ped == null)
                return;

            Vector3 dir = ParameterForEnteringState is Vector3 v ? v : Vector3.zero;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f)
                dir = -m_ped.transform.forward;
            direccionEsquiva = dir.normalized;

            float duracion = m_ped.GestorConfig.duracionEsquiva;
            m_ped.EstablecerInvulnerabilidadTemporal(duracion * 0.85f);
            m_ped.EstablecerDesplazamientoDirecto(direccionEsquiva * m_ped.GestorConfig.velocidadEsquiva);
            m_model?.DispararEsquiva();
        }

        public override void OnBecameInactive()
        {
            m_model?.LimpiarPoseTemporal();
            base.OnBecameInactive();
        }

        public override float CalcularVelocidadObjetivo() =>
            m_ped != null ? m_ped.GestorConfig.velocidadEsquiva : 4.8f;

        protected override void EvaluarTransiciones()
        {
            if (m_ped == null)
                return;

            float duracion = Mathf.Max(0.12f, m_ped.GestorConfig.duracionEsquiva);
            tiempoTranscurrido += Time.deltaTime;
            float progreso = Mathf.Clamp01(tiempoTranscurrido / duracion);
            float factor = 1f - progreso * 0.5f;
            m_ped.EstablecerDesplazamientoDirecto(direccionEsquiva * (m_ped.GestorConfig.velocidadEsquiva * factor));

            if (tiempoTranscurrido >= duracion)
            {
                System.Type siguiente = ObtenerEstadoSiguienteSegunEntrada(m_ped);
                if (siguiente == typeof(DodgeState))
                    siguiente = typeof(StandState);
                m_ped.CambiarEstado(siguiente);
            }
        }

        protected override void ActualizarAnimaciones()
        {
            base.ActualizarAnimaciones();
            if (m_ped == null || m_model == null)
                return;

            float duracion = Mathf.Max(0.12f, m_ped.GestorConfig.duracionEsquiva);
            float progreso = Mathf.Clamp01(tiempoTranscurrido / duracion);
            Vector3 local = m_ped.transform.InverseTransformDirection(direccionEsquiva);
            m_model.AplicarInclinacionEsquiva(progreso, local);
        }

        public override void AlPulsarSalto() { }
        public override void AlPulsarRodar(bool izquierda) { }
    }
}
