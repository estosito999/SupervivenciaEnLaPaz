// Adaptado e integrado para SupervivenciaEnLaPaz a partir de StandState.cs de SanAndreasUnity.
namespace SupervivenciaEnLaPaz.Movimiento.States
{
    public sealed class StandState : BaseMovementState
    {
        public override float TiempoMinimoSalida => 0f;

        public override RecorridoPlaza.EstadoLocomocion EstadoLegacy =>
            m_ped != null && m_ped.EstaEnCombate
                ? RecorridoPlaza.EstadoLocomocion.Combate
                : RecorridoPlaza.EstadoLocomocion.Reposo;

        public override string NombreEstadoHud =>
            m_ped != null && m_ped.EstaEnCombate ? "Combate" : "Reposo";

        public override float CalcularVelocidadObjetivo() => 0f;
    }
}
