// Adaptado e integrado para SupervivenciaEnLaPaz a partir de RollState.cs de SanAndreasUnity.
using UnityEngine;

namespace SupervivenciaEnLaPaz.Movimiento.States
{
    /// <summary>
    /// Giro evasivo lateral estilo GTA (Q izquierda / E derecha).
    /// Bloquea la orientación y aplica desplazamiento lateral controlado.
    /// </summary>
    public sealed class RollState : BaseMovementState
    {
        private bool rodarIzquierda;
        private float tiempoTranscurrido;
        private Vector3 direccionLateral;

        public override float TiempoMinimoSalida => 0f;
        public override float TiempoMinimoEntrada => 0.15f;
        public override bool PermiteGiro => false;
        public override bool PermiteAtaque => false;
        public override bool PermiteEntradaMovimiento => false;
        public override RecorridoPlaza.EstadoLocomocion EstadoLegacy => RecorridoPlaza.EstadoLocomocion.Rodar;
        public override string NombreEstadoHud => rodarIzquierda ? "Rodar Izq" : "Rodar Der";

        public bool PuedeRodar()
        {
            if (IsActiveState || m_ped == null || !m_ped.EnSuelo || m_ped.IsCrouchOn)
                return false;

            return m_ped.Resistencia >= m_ped.GestorConfig.consumoRodar * 0.5f;
        }

        public bool IniciarRodar(bool izquierda)
        {
            if (!PuedeRodar())
                return false;

            rodarIzquierda = izquierda;
            m_ped.ConsumirResistencia(m_ped.GestorConfig.consumoRodar);
            m_ped.CambiarEstadoConParametro<RollState>(izquierda);
            return true;
        }

        public override void OnBecameActive()
        {
            base.OnBecameActive();
            if (ParameterForEnteringState is bool izq)
                rodarIzquierda = izq;

            tiempoTranscurrido = 0f;
            if (m_ped != null)
            {
                Vector3 derecha = m_ped.transform.right;
                derecha.y = 0f;
                direccionLateral = (rodarIzquierda ? -derecha : derecha).normalized;
                m_ped.EstablecerInvulnerabilidadTemporal(m_ped.GestorConfig.duracionRodar * 0.72f);
                m_ped.EstablecerDesplazamientoDirecto(direccionLateral * m_ped.GestorConfig.velocidadRodar);
                m_model?.DispararRodar(rodarIzquierda ? -1f : 1f);
            }
        }

        public override void OnBecameInactive()
        {
            m_model?.LimpiarPoseTemporal();
            base.OnBecameInactive();
        }

        public override float CalcularVelocidadObjetivo() =>
            m_ped != null ? m_ped.GestorConfig.velocidadRodar : 4.2f;

        protected override void EvaluarTransiciones()
        {
            if (m_ped == null)
                return;

            float duracion = Mathf.Max(0.15f, m_ped.GestorConfig.duracionRodar);
            tiempoTranscurrido += Time.deltaTime;
            float progreso = Mathf.Clamp01(tiempoTranscurrido / duracion);

            // Curva suave: impulso fuerte al inicio y desaceleración al cierre.
            float factorVelocidad = Mathf.Sin((1f - progreso * 0.55f) * Mathf.PI * 0.5f);
            m_ped.EstablecerDesplazamientoDirecto(direccionLateral * (m_ped.GestorConfig.velocidadRodar * factorVelocidad));

            if (tiempoTranscurrido >= duracion)
            {
                System.Type siguiente = ObtenerEstadoSiguienteSegunEntrada(m_ped);
                if (siguiente == typeof(RollState))
                    siguiente = typeof(StandState);
                m_ped.CambiarEstado(siguiente);
            }
        }

        protected override void ActualizarAnimaciones()
        {
            base.ActualizarAnimaciones();
            if (m_ped == null || m_model == null)
                return;

            float duracion = Mathf.Max(0.15f, m_ped.GestorConfig.duracionRodar);
            float progreso = Mathf.Clamp01(tiempoTranscurrido / duracion);
            m_model.AplicarGiroRodar(progreso, rodarIzquierda ? 1f : -1f);
        }

        public override void AlPulsarSalto() { }
        public override void AlPulsarAgacharse() { }
        public override void AlPulsarRodar(bool izquierda) { }
    }
}
