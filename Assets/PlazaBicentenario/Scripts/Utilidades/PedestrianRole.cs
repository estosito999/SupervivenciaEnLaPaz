// Adaptado e integrado para SupervivenciaEnLaPaz a partir de PedestrianType de SanAndreasUnity.
namespace SupervivenciaEnLaPaz.Utilidades
{
    /// <summary>
    /// Rol local de comportamiento para personajes y peatones en la Plaza del Bicentenario.
    /// </summary>
    public enum PedestrianRole
    {
        Jugador = 0,
        Civil = 1,
        Rival = 2,
        Ladron = 3,
        Policia = 4
    }

    public static class PedestrianRoleExtensions
    {
        public static bool EsCivil(this PedestrianRole rol) => rol == PedestrianRole.Civil;
        public static bool EsHostil(this PedestrianRole rol) => rol == PedestrianRole.Rival || rol == PedestrianRole.Ladron;
        public static bool EsAutoridad(this PedestrianRole rol) => rol == PedestrianRole.Policia;
    }
}
