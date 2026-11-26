// Adaptado e integrado para SupervivenciaEnLaPaz a partir de WalkState.cs de SanAndreasUnity.
namespace SupervivenciaEnLaPaz.Movimiento.States
{
    public sealed class WalkState : BaseMovementState
    {
        public override RecorridoPlaza.EstadoLocomocion EstadoLegacy => RecorridoPlaza.EstadoLocomocion.Caminar;
        public override string NombreEstadoHud => "Caminar";

        public override float CalcularVelocidadObjetivo() =>
            m_ped != null && m_ped.TieneEntradaMovimiento ? m_ped.VelocidadCaminar : 0f;
    }
}
