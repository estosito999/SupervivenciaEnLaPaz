// Adaptado e integrado para SupervivenciaEnLaPaz a partir de SprintState.cs de SanAndreasUnity.
using UnityEngine;

namespace SupervivenciaEnLaPaz.Movimiento.States
{
    public sealed class SprintState : BaseMovementState
    {
        public override bool ConsumeResistencia => true;
        public override RecorridoPlaza.EstadoLocomocion EstadoLegacy => RecorridoPlaza.EstadoLocomocion.Sprint;
        public override string NombreEstadoHud => "Sprint";

        public override float CalcularVelocidadObjetivo() =>
            m_ped != null && m_ped.TieneEntradaMovimiento ? m_ped.VelocidadSprint : 0f;

        protected override void EvaluarTransiciones()
        {
            if (m_ped != null)
            {
                m_ped.ConsumirResistencia(m_ped.GestorConfig.consumoSprintPorSegundo * Time.deltaTime);
                if (!m_ped.PuedeEsprintar)
                {
                    IntentarCambiarA(typeof(RunState));
                    return;
                }
            }

            base.EvaluarTransiciones();
        }
    }
}
