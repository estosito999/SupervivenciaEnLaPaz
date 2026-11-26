// Adaptado e integrado para SupervivenciaEnLaPaz a partir de NPCPedSpawner.cs de SanAndreasUnity.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SupervivenciaEnLaPaz.AI
{
    /// <summary>
    /// Motor reutilizable de muestreo, validación de rutas y distribución espacial de NPCs
    /// sobre el NavMesh de la Plaza del Bicentenario (reemplaza WorldSystem y NodeReader de GTA).
    /// Trabaja coordinado con <see cref="GestorPeatones"/> para evitar duplicar poblaciones.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NPCPedSpawner : MonoBehaviour
    {
        private NavMeshPath rutaTemporal;

        private void Awake()
        {
            rutaTemporal = new NavMeshPath();
        }

        private Bounds zonaPaseo = new(new Vector3(0f, 0f, -1.3f), new Vector3(52f, 3f, 38f));
        private float distanciaMinimaJugador = 7f;
        private float distanciaMinimaRival = 3.5f;
        private float distanciaMinimaEntrePeatones = 1.8f;
        private float distanciaMinimaPaseo = 5f;
        private float distanciaMaximaPaseo = 22f;

        private Transform jugador;
        private Transform rival;
        private IReadOnlyList<Peaton> peatonesActivos;

        public void Configurar(
            Bounds zona,
            float distMinJugador,
            float distMinRival,
            float distMinEntrePeatones,
            float distMinPaseo,
            float distMaxPaseo,
            Transform refJugador,
            Transform refRival,
            IReadOnlyList<Peaton> activos)
        {
            zonaPaseo = zona;
            distanciaMinimaJugador = Mathf.Max(0f, distMinJugador);
            distanciaMinimaRival = Mathf.Max(0f, distMinRival);
            distanciaMinimaEntrePeatones = Mathf.Max(0f, distMinEntrePeatones);
            distanciaMinimaPaseo = Mathf.Max(1f, distMinPaseo);
            distanciaMaximaPaseo = Mathf.Max(distanciaMinimaPaseo, distMaxPaseo);
            jugador = refJugador;
            rival = refRival;
            peatonesActivos = activos;
        }

        public void ActualizarReferencias(Transform refJugador, Transform refRival)
        {
            jugador = refJugador;
            rival = refRival;
        }

        public bool BuscarPuntoAparicion(out Vector3 punto)
        {
            NavegacionPlaza nav = NavegacionPlaza.Instancia;
            Vector3 referencia = new(zonaPaseo.center.x, 0f, zonaPaseo.center.z);
            if (!nav.Muestrear(referencia, out referencia, 3f))
                referencia = transform.position;

            for (int intento = 0; intento < 40; intento++)
            {
                if (!nav.PuntoAleatorio(zonaPaseo, out punto))
                    continue;

                if (jugador != null && PedAI.DistanciaHorizontal(punto, jugador.position) < distanciaMinimaJugador)
                    continue;

                if (rival != null && rival.gameObject.activeInHierarchy &&
                    PedAI.DistanciaHorizontal(punto, rival.position) < distanciaMinimaRival)
                    continue;

                if (peatonesActivos != null && EstaCercaDeOtroPeaton(punto))
                    continue;

                // Evita islas de navegación aisladas respecto al centro de la plaza.
                if (intento < 25 && !nav.CalcularRuta(punto, referencia, rutaTemporal))
                    continue;

                return true;
            }

            punto = transform.position;
            return false;
        }

        public bool BuscarDestino(Vector3 desde, out Vector3 destino)
        {
            return BuscarDestino(desde, distanciaMinimaPaseo, distanciaMaximaPaseo, out destino);
        }

        public bool BuscarDestino(Vector3 desde, float minDistancia, float maxDistancia, out Vector3 destino)
        {
            NavegacionPlaza nav = NavegacionPlaza.Instancia;
            for (int intento = 0; intento < 26; intento++)
            {
                if (!nav.PuntoAleatorio(zonaPaseo, out Vector3 candidato))
                    continue;

                float distancia = PedAI.DistanciaHorizontal(candidato, desde);
                if (distancia < minDistancia || distancia > maxDistancia)
                    continue;

                if (!nav.CalcularRuta(desde, candidato, rutaTemporal))
                    continue;

                destino = candidato;
                return true;
            }

            destino = desde;
            return false;
        }

        public bool BuscarPuntoHuida(Vector3 peligro, Vector3 desde, float radioReaccion, out Vector3 destino)
        {
            NavegacionPlaza nav = NavegacionPlaza.Instancia;
            float distanciaActual = PedAI.DistanciaHorizontal(desde, peligro);
            float distanciaMinima = Mathf.Max(radioReaccion + 2f, distanciaActual + 2f);
            float mejorDistancia = float.MinValue;
            Vector3 mejorPunto = desde;
            bool encontrado = false;

            for (int intento = 0; intento < 40; intento++)
            {
                if (!nav.PuntoAleatorio(zonaPaseo, out Vector3 candidato))
                    continue;

                float distancia = PedAI.DistanciaHorizontal(candidato, peligro);
                if (distancia < distanciaMinima || distancia <= mejorDistancia)
                    continue;

                if (!nav.CalcularRuta(desde, candidato, rutaTemporal))
                    continue;

                mejorDistancia = distancia;
                mejorPunto = candidato;
                encontrado = true;
            }

            destino = mejorPunto;
            return encontrado;
        }

        private bool EstaCercaDeOtroPeaton(Vector3 punto)
        {
            for (int i = 0; i < peatonesActivos.Count; i++)
            {
                Peaton otro = peatonesActivos[i];
                if (otro != null && PedAI.DistanciaHorizontal(punto, otro.transform.position) < distanciaMinimaEntrePeatones)
                    return true;
            }
            return false;
        }
    }
}
