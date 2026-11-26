using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SupervivenciaEnLaPaz
{
    /// <summary>
    /// Construye una única malla de navegación para la plaza y la comparte entre
    /// el rival y todos los peatones. La malla se genera en tiempo de ejecución a
    /// partir de los colisionadores de la escena, igual que hacía RivalPlaza.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NavegacionPlaza : MonoBehaviour
    {
        [SerializeField] private Bounds zonaNavegable = new(new Vector3(0f, 2f, -1.3f), new Vector3(60f, 12f, 46f));
        [SerializeField, Min(0.05f)] private float radioAgente = 0.28f;
        [SerializeField, Min(0.5f)] private float alturaAgente = 1.78f;
        [SerializeField, Min(0.05f)] private float alturaEscalon = 0.34f;
        [SerializeField, Min(0.02f)] private float tamanoVoxel = 0.1f;

        private static NavegacionPlaza instancia;
        private NavMeshData datos;
        private NavMeshDataInstance instanciaMalla;
        private bool intentoConstruccion;

        public static NavegacionPlaza Instancia
        {
            get
            {
                if (instancia != null)
                    return instancia;

                instancia = FindAnyObjectByType<NavegacionPlaza>();
                if (instancia == null)
                    instancia = new GameObject("Navegación de la plaza").AddComponent<NavegacionPlaza>();
                return instancia;
            }
        }

        public bool Lista => AsegurarMalla();

        private void Awake()
        {
            if (instancia != null && instancia != this)
            {
                Destroy(this);
                return;
            }
            instancia = this;
        }

        /// <summary>Genera la malla una sola vez. Devuelve true si está disponible.</summary>
        public bool AsegurarMalla()
        {
            if (intentoConstruccion)
                return instanciaMalla.valid;
            intentoConstruccion = true;

            var fuentes = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(zonaNavegable, Physics.DefaultRaycastLayers,
                NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), fuentes);
            if (fuentes.Count == 0)
            {
                intentoConstruccion = false;
                Debug.LogWarning("Navegación de la plaza: no se encontraron colisionadores para generar la malla.", this);
                return false;
            }

            NavMeshBuildSettings ajustes = NavMesh.GetSettingsByIndex(0);
            ajustes.agentRadius = radioAgente;
            ajustes.agentHeight = alturaAgente;
            ajustes.agentClimb = alturaEscalon;
            ajustes.overrideVoxelSize = true;
            ajustes.voxelSize = tamanoVoxel;

            datos = NavMeshBuilder.BuildNavMeshData(ajustes, fuentes, zonaNavegable,
                Vector3.zero, Quaternion.identity);
            if (datos == null)
            {
                Debug.LogWarning("Navegación de la plaza: no se pudo construir la malla.", this);
                return false;
            }

            instanciaMalla = NavMesh.AddNavMeshData(datos);
            return instanciaMalla.valid;
        }

        public bool Muestrear(Vector3 posicion, out Vector3 punto, float radio = 2f)
        {
            AsegurarMalla();
            if (NavMesh.SamplePosition(posicion, out NavMeshHit impacto, radio, NavMesh.AllAreas))
            {
                punto = impacto.position;
                return true;
            }
            punto = posicion;
            return false;
        }

        public bool CalcularRuta(Vector3 desde, Vector3 hasta, NavMeshPath ruta, float radioMuestreo = 1.2f)
        {
            if (ruta == null || !AsegurarMalla())
                return false;
            if (!NavMesh.SamplePosition(desde, out NavMeshHit inicio, radioMuestreo, NavMesh.AllAreas) ||
                !NavMesh.SamplePosition(hasta, out NavMeshHit fin, radioMuestreo, NavMesh.AllAreas))
                return false;
            if (!NavMesh.CalculatePath(inicio.position, fin.position, NavMesh.AllAreas, ruta))
                return false;
            return ruta.status == NavMeshPathStatus.PathComplete;
        }

        public bool PuntoAleatorio(Bounds zona, out Vector3 punto, int intentos = 30)
        {
            AsegurarMalla();
            for (int i = 0; i < intentos; i++)
            {
                var candidato = new Vector3(
                    Random.Range(zona.min.x, zona.max.x),
                    zona.center.y,
                    Random.Range(zona.min.z, zona.max.z));
                if (NavMesh.SamplePosition(candidato, out NavMeshHit impacto, 2.5f, NavMesh.AllAreas))
                {
                    punto = impacto.position;
                    return true;
                }
            }
            punto = zona.center;
            return false;
        }

        private void OnDestroy()
        {
            if (instancia == this)
                instancia = null;
            if (instanciaMalla.valid)
                instanciaMalla.Remove();
            if (datos != null)
                Destroy(datos);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.25f, 0.85f, 0.75f, 0.35f);
            Gizmos.DrawWireCube(zonaNavegable.center, zonaNavegable.size);
        }
    }
}
