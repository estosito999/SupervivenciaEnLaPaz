// Adaptado e integrado para SupervivenciaEnLaPaz a partir de RunState.cs de SanAndreasUnity.
namespace SupervivenciaEnLaPaz.Movimiento.States
{
    public sealed class RunState : BaseMovementState
    {
        public override RecorridoPlaza.EstadoLocomocion EstadoLegacy =>
            m_ped != null && m_ped.EstaEnCombate
                ? RecorridoPlaza.EstadoLocomocion.Combate
                : RecorridoPlaza.EstadoLocomocion.Correr;

        public override string NombreEstadoHud =>
            m_ped != null && m_ped.EstaEnCombate ? "Combate" : "Correr";

        public override float CalcularVelocidadObjetivo() =>
            m_ped != null && m_ped.TieneEntradaMovimiento ? m_ped.VelocidadCorrer : 0f;
    }
}
