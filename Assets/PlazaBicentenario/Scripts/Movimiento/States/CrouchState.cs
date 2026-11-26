// Adaptado e integrado para SupervivenciaEnLaPaz a partir de CrouchState.cs de SanAndreasUnity.
using System;

namespace SupervivenciaEnLaPaz.Movimiento.States
{
    public sealed class CrouchState : BaseMovementState
    {
        public override bool EsAgachado => true;
        public override RecorridoPlaza.EstadoLocomocion EstadoLegacy => RecorridoPlaza.EstadoLocomocion.Agachado;
        public override string NombreEstadoHud => "Agachado";

        public override void OnBecameActive()
        {
            base.OnBecameActive();
            m_ped?.EstablecerModoAgachado(true);
        }

        public override void OnBecameInactive()
        {
            if (m_ped != null && !m_ped.IsCrouchOn)
                m_ped.EstablecerModoAgachado(false);
            base.OnBecameInactive();
        }

        public override float CalcularVelocidadObjetivo() => 0f;

        protected override void EvaluarTransiciones()
        {
            if (m_ped == null)
                return;

            if (!m_ped.EnSuelo)
            {
                m_ped.IsCrouchOn = false;
                m_ped.EstablecerModoAgachado(false);
                IntentarCambiarA(typeof(JumpState));
                return;
            }

            if (!m_ped.IsCrouchOn)
            {
                if (m_ped.PuedeLevantarse())
                {
                    m_ped.EstablecerModoAgachado(false);
                    Type siguiente = ObtenerEstadoSiguienteSegunEntrada(m_ped);
                    IntentarCambiarA(siguiente);
                }
                else
                {
                    m_ped.IsCrouchOn = true;
                }
                return;
            }

            if (m_ped.TieneEntradaMovimiento)
                IntentarCambiarA(typeof(CrouchMoveState));
        }

        public override void AlPulsarSalto()
        {
            if (m_ped == null || !m_ped.PuedeLevantarse())
                return;

            m_ped.IsCrouchOn = false;
            m_ped.EstablecerModoAgachado(false);
            IntentarCambiarA(typeof(StandState));
        }
    }
}
