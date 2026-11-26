// Adaptado e integrado para SupervivenciaEnLaPaz a partir de JumpState.cs de SanAndreasUnity.
using UnityEngine;

namespace SupervivenciaEnLaPaz.Movimiento.States
{
    /// <summary>
    /// Estado de salto, planeo aéreo y aterrizaje controlado.
    /// </summary>
    public sealed class JumpState : BaseMovementState
    {
        public enum FaseSalto { Lanzamiento, Caida, Aterrizaje }

        [SerializeField, Range(0f, 0.35f)] private float duracionAterrizaje = 0.14f;
        [SerializeField, Range(0.2f, 1f)] private float multiplicadorVelocidadAterrizaje = 0.65f;

        private float tiempoAterrizajeRestante;
        private bool saltoIniciadoPorEstado;

        public FaseSalto FaseActual { get; private set; }
        public override float TiempoMinimoEntrada => m_ped != null ? m_ped.GestorConfig.tiempoReentradaSalto : 0.25f;
        public override float TiempoMinimoSalida => 0f;
        public override bool PermiteGiro => FaseActual != FaseSalto.Lanzamiento;
        public override bool PermiteAtaque => false;

        public override RecorridoPlaza.EstadoLocomocion EstadoLegacy =>
            m_ped != null && m_ped.VelocidadVertical > 0.05f
                ? RecorridoPlaza.EstadoLocomocion.Saltar
                : RecorridoPlaza.EstadoLocomocion.Caer;

        public override string NombreEstadoHud =>
            FaseActual == FaseSalto.Aterrizaje
                ? "Aterrizar"
                : (m_ped != null && m_ped.VelocidadVertical > 0.05f ? "Saltar" : "Caer");

        public bool PuedeSaltar()
        {
            if (m_ped == null) return false;
            return m_ped.EnSuelo && !m_ped.EstaEnCombate && !m_ped.IsCrouchOn;
        }

        public override void OnBecameActive()
        {
            base.OnBecameActive();
            tiempoAterrizajeRestante = 0f;
            saltoIniciadoPorEstado = false;

            if (m_ped != null && m_ped.EnSuelo && m_ped.IsJumpPressedThisFrame)
            {
                saltoIniciadoPorEstado = true;
                FaseActual = FaseSalto.Lanzamiento;
                m_ped.AplicarImpulsoSalto();
                m_model?.DispararSalto();
            }
            else
            {
                FaseActual = FaseSalto.Caida;
            }
        }

        public override void OnBecameInactive()
        {
            m_model?.LimpiarPoseTemporal();
            base.OnBecameInactive();
        }

        public override float CalcularVelocidadObjetivo()
        {
            if (m_ped == null || !m_ped.TieneEntradaMovimiento)
                return 0f;

            float baseVel = m_ped.IsSprintOn && m_ped.PuedeEsprintar ? m_ped.VelocidadSprint : m_ped.VelocidadCorrer;
            if (FaseActual == FaseSalto.Aterrizaje)
                return baseVel * multiplicadorVelocidadAterrizaje;
            return baseVel;
        }

        protected override void EvaluarTransiciones()
        {
            if (m_ped == null)
                return;

            if (FaseActual == FaseSalto.Lanzamiento && m_ped.VelocidadVertical <= 0f)
                FaseActual = FaseSalto.Caida;

            if (FaseActual != FaseSalto.Aterrizaje)
            {
                bool aterrizo = m_ped.EnSuelo && (!saltoIniciadoPorEstado || TimeSinceActivated > 0.08f) && m_ped.VelocidadVertical <= 0f;
                if (aterrizo)
                {
                    FaseActual = FaseSalto.Aterrizaje;
                    tiempoAterrizajeRestante = duracionAterrizaje;
                    m_ped.AlAterrizar();
                }
                return;
            }

            tiempoAterrizajeRestante -= Time.deltaTime;
            if (tiempoAterrizajeRestante <= 0f)
            {
                System.Type siguiente = ObtenerEstadoSiguienteSegunEntrada(m_ped);
                if (siguiente == typeof(JumpState))
                    siguiente = m_ped.TieneEntradaMovimiento ? typeof(RunState) : typeof(StandState);
                m_ped.CambiarEstado(siguiente);
            }
        }

        protected override void ActualizarAnimaciones()
        {
            base.ActualizarAnimaciones();
            if (m_model == null)
                return;

            if (FaseActual == FaseSalto.Aterrizaje)
            {
                float t = duracionAterrizaje > 0f ? Mathf.Clamp01(tiempoAterrizajeRestante / duracionAterrizaje) : 0f;
                m_model.AplicarAgachadoAereoOAterrizaje(Mathf.Sin(t * Mathf.PI) * 0.12f);
            }
            else
            {
                m_model.AplicarAgachadoAereoOAterrizaje(0.06f);
            }
        }
    }
}
