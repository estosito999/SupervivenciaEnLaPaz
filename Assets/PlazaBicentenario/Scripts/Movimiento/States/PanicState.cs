// Adaptado e integrado para SupervivenciaEnLaPaz a partir de PanicState.cs de SanAndreasUnity.
using UnityEngine;

namespace SupervivenciaEnLaPaz.Movimiento.States
{
    /// <summary>
    /// Estado físico de carrera rápida en pánico para peatones ante peligro cercano.
    /// </summary>
    public sealed class PanicState : BaseMovementState
    {
        public override RecorridoPlaza.EstadoLocomocion EstadoLegacy => RecorridoPlaza.EstadoLocomocion.Correr;
        public override string NombreEstadoHud => "Pánico";

        public override float CalcularVelocidadObjetivo() =>
            m_ped != null && m_ped.TieneEntradaMovimiento ? m_ped.VelocidadPanico : 0f;

        protected override void ActualizarAnimaciones()
        {
            if (m_ped == null || m_model == null)
                return;

            float refVel = Mathf.Max(0.1f, m_ped.VelocidadCorrer);
            float valorSpeed = Mathf.Clamp(m_ped.VelocidadHorizontalActual / refVel, 0f, 1.55f);
            m_model.ActualizarParametros(valorSpeed, m_ped.EnSuelo, m_ped.VelocidadVertical, false, false, true);
        }
    }
}
