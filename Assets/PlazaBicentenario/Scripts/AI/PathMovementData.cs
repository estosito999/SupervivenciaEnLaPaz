// Adaptado e integrado para SupervivenciaEnLaPaz a partir de PathMovementData.cs de SanAndreasUnity.
using UnityEngine;
using UnityEngine.AI;

namespace SupervivenciaEnLaPaz.AI
{
    /// <summary>
    /// Datos y utilidades de seguimiento de rutas sobre el NavMesh local de la plaza.
    /// Reemplaza el uso de PathNode binarios de GTA por NavMeshPath reutilizable.
    /// </summary>
    public sealed class PathMovementData
    {
        public const int MaxEsquinas = 64;

        public Vector3? DestinoActual;
        public Vector3 CentroPeligro;
        public float TiempoUltimoIntentoDestino;
        public float ProximoRecalculo;
        public float ProximoChequeoAtasco;
        public Vector3 UltimaPosicionAtasco;
        public int IntentosAtasco;

        public readonly NavMeshPath Ruta = new();
        public readonly Vector3[] Esquinas = new Vector3[MaxEsquinas];
        public int CantidadEsquinas;
        public int SiguienteEsquina;
        public bool TieneRuta;

        public void Limpiar()
        {
            DestinoActual = null;
            TiempoUltimoIntentoDestino = 0f;
            ProximoRecalculo = 0f;
            CantidadEsquinas = 0;
            SiguienteEsquina = 0;
            TieneRuta = false;
        }

        public bool CalcularRuta(Vector3 origen, Vector3 destino, NavegacionPlaza navegacion)
        {
            if (navegacion == null || !navegacion.CalcularRuta(origen, destino, Ruta))
            {
                TieneRuta = false;
                CantidadEsquinas = 0;
                SiguienteEsquina = 0;
                return false;
            }

            DestinoActual = destino;
            CantidadEsquinas = Ruta.GetCornersNonAlloc(Esquinas);
            SiguienteEsquina = CantidadEsquinas > 1 ? 1 : 0;
            TieneRuta = CantidadEsquinas > 0;
            ProximoRecalculo = Time.time + Random.Range(1.0f, 1.65f);
            return TieneRuta;
        }

        public Vector3 ObtenerDireccionHorizontal(Vector3 posicionActual, float distanciaLlegada)
        {
            if (!TieneRuta || CantidadEsquinas == 0)
                return Vector3.zero;

            while (SiguienteEsquina < CantidadEsquinas - 1 &&
                   DistanciaHorizontal(Esquinas[SiguienteEsquina] - posicionActual) < 0.3f)
            {
                SiguienteEsquina++;
            }

            Vector3 diferencia = Esquinas[SiguienteEsquina] - posicionActual;
            diferencia.y = 0f;
            float distancia = diferencia.magnitude;
            if (distancia < distanciaLlegada && SiguienteEsquina >= CantidadEsquinas - 1)
                return Vector3.zero;

            return diferencia / Mathf.Max(distancia, 0.001f);
        }

        private static float DistanciaHorizontal(Vector3 v)
        {
            v.y = 0f;
            return v.magnitude;
        }
    }
}
