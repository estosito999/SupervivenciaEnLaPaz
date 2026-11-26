// Adaptado e integrado para SupervivenciaEnLaPaz a partir de SanAndreasUnity.
using UnityEngine;

namespace SupervivenciaEnLaPaz.Core
{
    public enum TipoDano
    {
        CuerpoACuerpo = 0,
        Caida = 1,
        Entorno = 2
    }

    /// <summary>
    /// Describe un evento de daño local entre entidades del escenario.
    /// </summary>
    public sealed class DamageInfo
    {
        public float Cantidad { get; set; }
        public GameObject Atacante { get; set; }
        public Vector3 DireccionImpacto { get; set; }
        public Vector3 PuntoImpacto { get; set; }
        public TipoDano Tipo { get; set; }
        public bool IgnorarEsquiva { get; set; }

        public DamageInfo(
            float cantidad,
            GameObject atacante = null,
            Vector3 direccionImpacto = default,
            Vector3 puntoImpacto = default,
            TipoDano tipo = TipoDano.CuerpoACuerpo,
            bool ignorarEsquiva = false)
        {
            Cantidad = cantidad;
            Atacante = atacante;
            DireccionImpacto = direccionImpacto.sqrMagnitude > 0.0001f ? direccionImpacto.normalized : Vector3.forward;
            PuntoImpacto = puntoImpacto;
            Tipo = tipo;
            IgnorarEsquiva = ignorarEsquiva;
        }
    }
}
