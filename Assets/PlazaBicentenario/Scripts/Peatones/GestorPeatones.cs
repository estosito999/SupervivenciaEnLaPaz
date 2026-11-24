using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SupervivenciaEnLaPaz
{
    /// <summary>
    /// Población de peatones de la plaza. Mantiene un número estable de civiles,
    /// los reparte por la malla de navegación y les avisa cuando empieza o termina
    /// el combate para que se alejen y después vuelvan a su rutina.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GestorPeatones : MonoBehaviour
    {
        [Header("Población")]
        [SerializeField] private Peaton prefabPeaton;
        [SerializeField, Min(0)] private int cantidad = 14;
        [SerializeField] private Bounds zonaPaseo = new(new Vector3(0f, 0f, -1.3f), new Vector3(52f, 3f, 38f));
        [SerializeField, Min(0.01f)] private float intervaloAparicion = 0.28f;

        [Header("Distancias")]
        [SerializeField, Min(0f)] private float distanciaMinimaJugador = 7f;
        [SerializeField, Min(0f)] private float distanciaMinimaRival = 3.5f;
        [SerializeField, Min(0f)] private float distanciaMinimaEntrePeatones = 1.8f;
        [SerializeField, Min(1f)] private float distanciaMinimaPaseo = 5f;
        [SerializeField, Min(1f)] private float distanciaMaximaPaseo = 22f;
        [SerializeField, Min(1f)] private float radioReaccionCombate = 11f;

        private static readonly Color[] ColoresChaqueta =
        {
            new(0.78f, 0.20f, 0.24f), new(0.18f, 0.38f, 0.62f), new(0.16f, 0.47f, 0.36f),
            new(0.74f, 0.53f, 0.16f), new(0.42f, 0.26f, 0.52f), new(0.82f, 0.40f, 0.20f),
            new(0.20f, 0.52f, 0.58f), new(0.62f, 0.20f, 0.44f), new(0.35f, 0.35f, 0.40f),
            new(0.85f, 0.66f, 0.36f), new(0.22f, 0.30f, 0.24f), new(0.66f, 0.30f, 0.14f),
        };

        private static readonly Color[] ColoresPiel =
        {
            new(0.78f, 0.57f, 0.47f), new(0.88f, 0.70f, 0.58f), new(0.62f, 0.43f, 0.33f),
            new(0.47f, 0.31f, 0.23f), new(0.70f, 0.50f, 0.38f),
        };

        private static readonly Color[] ColoresCabello =
        {
            new(0.10f, 0.07f, 0.05f), new(0.21f, 0.12f, 0.09f), new(0.35f, 0.22f, 0.13f),
            new(0.48f, 0.34f, 0.20f), new(0.55f, 0.55f, 0.53f),
        };

        private readonly List<Peaton> peatones = new();
        private NavMeshPath rutaTemporal;

        private Transform jugador;
        private Transform rival;
        private Material[] chaquetas;
        private Material[] pieles;
        private Material[] cabellos;
        private float proximaAparicion;
        private int contador;

        public static GestorPeatones Instancia { get; private set; }
        public Transform Jugador => jugador;
        public Transform Rival => rival;
        public float DistanciaMinimaPaseo => distanciaMinimaPaseo;
        public float DistanciaMaximaPaseo => distanciaMaximaPaseo;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(this);
                return;
            }
            Instancia = this;
            rutaTemporal = new NavMeshPath();
        }

        private void Start()
        {
            jugador = FindFirstObjectByType<RecorridoPlaza>()?.transform;
            rival = FindFirstObjectByType<RivalPlaza>()?.transform;

            if (prefabPeaton == null)
            {
                Debug.LogError("GestorPeatones necesita un prefab de peatón.", this);
                return;
            }

            NavegacionPlaza.Instancia.AsegurarMalla();
            CrearVariantesDeAspecto();
            proximaAparicion = Time.time + 0.2f;

            CombateArena.CombateIniciado += AlIniciarCombate;
            CombateArena.CombateTerminado += AlTerminarCombate;
        }

        private void Update()
        {
            if (prefabPeaton == null)
                return;

            for (int i = peatones.Count - 1; i >= 0; i--)
                if (peatones[i] == null)
                    peatones.RemoveAt(i);

            if (peatones.Count >= cantidad || Time.time < proximaAparicion)
                return;

            proximaAparicion = Time.time + intervaloAparicion;
            IntentarCrearPeaton();
        }

        private void IntentarCrearPeaton()
        {
            if (!BuscarPuntoAparicion(out Vector3 punto))
                return;

            Peaton peaton = Instantiate(prefabPeaton, punto,
                Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
            peaton.name = $"Peaton {++contador:00}";
            peaton.Configurar(this);

            if (chaquetas != null && chaquetas.Length > 0)
            {
                peaton.ConfigurarAspecto(
                    chaquetas[Random.Range(0, chaquetas.Length)],
                    pieles[Random.Range(0, pieles.Length)],
                    cabellos[Random.Range(0, cabellos.Length)]);
            }

            peatones.Add(peaton);
        }

        private bool BuscarPuntoAparicion(out Vector3 punto)
        {
            Vector3 referencia = new(zonaPaseo.center.x, 0f, zonaPaseo.center.z);
            if (!NavegacionPlaza.Instancia.Muestrear(referencia, out referencia, 3f))
                referencia = transform.position;

            for (int intento = 0; intento < 40; intento++)
            {
                if (!NavegacionPlaza.Instancia.PuntoAleatorio(zonaPaseo, out punto))
                    continue;

                if (jugador != null && DistanciaHorizontal(punto, jugador.position) < distanciaMinimaJugador)
                    continue;
                if (rival != null && rival.gameObject.activeInHierarchy &&
                    DistanciaHorizontal(punto, rival.position) < distanciaMinimaRival)
                    continue;

                bool cercaDeOtro = false;
                for (int i = 0; i < peatones.Count; i++)
                {
                    if (peatones[i] != null &&
                        DistanciaHorizontal(punto, peatones[i].transform.position) < distanciaMinimaEntrePeatones)
                    {
                        cercaDeOtro = true;
                        break;
                    }
                }
                if (cercaDeOtro)
                    continue;

                // Evita islas de navegación a las que no se puede llegar desde la plaza.
                if (intento < 25 && !NavegacionPlaza.Instancia.CalcularRuta(punto, referencia, rutaTemporal))
                    continue;

                return true;
            }

            punto = transform.position;
            return false;
        }

        public bool BuscarDestino(Vector3 desde, float distanciaMinima, float distanciaMaxima, out Vector3 destino)
        {
            for (int intento = 0; intento < 26; intento++)
            {
                if (!NavegacionPlaza.Instancia.PuntoAleatorio(zonaPaseo, out Vector3 candidato))
                    continue;

                float distancia = DistanciaHorizontal(candidato, desde);
                if (distancia < distanciaMinima || distancia > distanciaMaxima)
                    continue;
                if (!NavegacionPlaza.Instancia.CalcularRuta(desde, candidato, rutaTemporal))
                    continue;

                destino = candidato;
                return true;
            }

            destino = desde;
            return false;
        }

        public bool BuscarPuntoHuida(Vector3 peligro, Vector3 desde, out Vector3 destino)
        {
            float distanciaActual = DistanciaHorizontal(desde, peligro);
            float distanciaMinima = Mathf.Max(radioReaccionCombate + 2f, distanciaActual + 2f);
            float mejorDistancia = float.MinValue;
            Vector3 mejorPunto = desde;
            bool encontrado = false;

            for (int intento = 0; intento < 40; intento++)
            {
                if (!NavegacionPlaza.Instancia.PuntoAleatorio(zonaPaseo, out Vector3 candidato))
                    continue;

                float distancia = DistanciaHorizontal(candidato, peligro);
                if (distancia < distanciaMinima || distancia <= mejorDistancia)
                    continue;
                if (!NavegacionPlaza.Instancia.CalcularRuta(desde, candidato, rutaTemporal))
                    continue;

                mejorDistancia = distancia;
                mejorPunto = candidato;
                encontrado = true;
            }

            destino = mejorPunto;
            return encontrado;
        }

        public void Reubicar(Peaton peaton)
        {
            if (peaton == null)
                return;
            if (BuscarPuntoAparicion(out Vector3 punto))
                peaton.Reubicar(punto);
        }

        private void AlIniciarCombate(Vector3 centro)
        {
            for (int i = 0; i < peatones.Count; i++)
                if (peatones[i] != null)
                    peatones[i].ReaccionarAlCombate(centro, radioReaccionCombate);
        }

        private void AlTerminarCombate()
        {
            for (int i = 0; i < peatones.Count; i++)
                if (peatones[i] != null)
                    peatones[i].Calmarse();
        }

        private void CrearVariantesDeAspecto()
        {
            SkinnedMeshRenderer malla = prefabPeaton.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (malla == null)
                return;

            Material[] baseMateriales = malla.sharedMaterials;
            if (baseMateriales.Length < 4)
            {
                Debug.LogWarning("El prefab del peatón necesita 4 materiales para variar ropa, piel y cabello.", this);
                return;
            }

            chaquetas = CrearVariantes(baseMateriales[1], ColoresChaqueta, "Chaqueta");
            pieles = CrearVariantes(baseMateriales[2], ColoresPiel, "Piel");
            cabellos = CrearVariantes(baseMateriales[3], ColoresCabello, "Cabello");
        }

        private static Material[] CrearVariantes(Material baseMaterial, Color[] colores, string nombre)
        {
            var variantes = new Material[colores.Length];
            for (int i = 0; i < colores.Length; i++)
            {
                var material = new Material(baseMaterial) { name = $"{nombre} {i + 1}" };
                material.SetColor("_BaseColor", colores[i]);
                material.SetColor("_Color", colores[i]);
                variantes[i] = material;
            }
            return variantes;
        }

        private void OnDestroy()
        {
            CombateArena.CombateIniciado -= AlIniciarCombate;
            CombateArena.CombateTerminado -= AlTerminarCombate;

            DestruirVariantes(chaquetas);
            DestruirVariantes(pieles);
            DestruirVariantes(cabellos);

            if (Instancia == this)
                Instancia = null;
        }

        private static void DestruirVariantes(Material[] materiales)
        {
            if (materiales == null)
                return;
            for (int i = 0; i < materiales.Length; i++)
                if (materiales[i] != null)
                    Destroy(materiales[i]);
        }

        private static float DistanciaHorizontal(Vector3 a, Vector3 b)
        {
            Vector3 diferencia = a - b;
            diferencia.y = 0f;
            return diferencia.magnitude;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.25f, 0.85f, 0.75f, 0.45f);
            Gizmos.DrawWireCube(zonaPaseo.center, zonaPaseo.size);

            if (!Application.isPlaying)
                return;

            Gizmos.color = new Color(0.95f, 0.75f, 0.30f, 0.8f);
            for (int i = 0; i < peatones.Count; i++)
                if (peatones[i] != null)
                    Gizmos.DrawWireSphere(peatones[i].transform.position, 0.3f);
        }
    }
}
