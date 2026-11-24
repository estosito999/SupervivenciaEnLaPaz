using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SupervivenciaEnLaPaz
{
    [RequireComponent(typeof(CharacterController), typeof(Animator))]
    public sealed class RivalPlaza : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float velocidad = 3.6f;
        [SerializeField] private Bounds zonaNavegable = new(new Vector3(0f, 2f, -1f), new Vector3(60f, 12f, 46f));

        private CharacterController controlador;
        private Animator animador;
        private Transform objetivo;
        private NavMeshData datos;
        private NavMeshDataInstance instancia;
        private NavMeshPath ruta;
        private readonly Vector3[] esquinas = new Vector3[32];
        private int cantidad;
        private int siguiente;
        private float recalcularEn;
        private float distanciaParada;
        private float velocidadVertical;

        private void Start()
        {
            controlador = GetComponent<CharacterController>();
            animador = GetComponent<Animator>();
            ruta = new NavMeshPath();

            // Los colisionadores incluyen gradas, bancos y puestos. Ambos actores usan Ignore Raycast.
            Bounds zona = zonaNavegable;
            zona.center = transform.root.TransformPoint(zona.center);
            var fuentes = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(zona, Physics.DefaultRaycastLayers, NavMeshCollectGeometry.PhysicsColliders,
                0, new List<NavMeshBuildMarkup>(), fuentes);
            var ajustes = NavMesh.GetSettingsByIndex(0);
            ajustes.agentRadius = controlador.radius + 0.05f;
            ajustes.agentHeight = controlador.height;
            ajustes.agentClimb = controlador.stepOffset;
            ajustes.overrideVoxelSize = true;
            ajustes.voxelSize = 0.1f;
            datos = NavMeshBuilder.BuildNavMeshData(ajustes, fuentes, zona, Vector3.zero, Quaternion.identity);
            if (datos != null) instancia = NavMesh.AddNavMeshData(datos);
        }

        internal void Perseguir(Transform jugador, float distancia)
        {
            objetivo = jugador;
            distanciaParada = distancia;
            recalcularEn = 0f;
        }

        private void Update()
        {
            if (objetivo == null) return;
            Vector3 haciaJugador = objetivo.position - transform.position;
            if (Time.time >= recalcularEn)
            {
                ActualizarRuta();
                recalcularEn = Time.time + 0.22f;
            }

            Vector3 direccion = Vector3.zero;
            if (haciaJugador.sqrMagnitude > distanciaParada * distanciaParada)
            {
                while (siguiente < cantidad - 1 && DistanciaHorizontal(esquinas[siguiente]) < 0.24f)
                    siguiente++;
                Vector3 destino = siguiente < cantidad ? esquinas[siguiente] : objetivo.position;
                direccion = Vector3.ProjectOnPlane(destino - transform.position, Vector3.up).normalized;
            }

            Vector3 mirada = haciaJugador.sqrMagnitude < 4f || direccion == Vector3.zero
                ? Vector3.ProjectOnPlane(haciaJugador, Vector3.up) : direccion;
            float dt = Time.deltaTime;
            if (mirada.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(mirada), 720f * dt);

            if (controlador.isGrounded && velocidadVertical < 0f) velocidadVertical = -2f;
            velocidadVertical += Physics.gravity.y * dt;
            controlador.Move((direccion * velocidad + Vector3.up * velocidadVertical) * dt);
            animador.SetFloat("Speed", direccion.magnitude, 0.08f, dt);
        }

        private void ActualizarRuta()
        {
            cantidad = siguiente = 0;
            if (!instancia.valid ||
                !NavMesh.SamplePosition(transform.position, out NavMeshHit inicio, 1.2f, NavMesh.AllAreas) ||
                !NavMesh.SamplePosition(objetivo.position, out NavMeshHit fin, 1.2f, NavMesh.AllAreas) ||
                !NavMesh.CalculatePath(inicio.position, fin.position, NavMesh.AllAreas, ruta))
                return;
            cantidad = ruta.GetCornersNonAlloc(esquinas);
            siguiente = cantidad > 1 ? 1 : 0;
        }

        private float DistanciaHorizontal(Vector3 punto)
        {
            return Vector3.ProjectOnPlane(punto - transform.position, Vector3.up).magnitude;
        }

        private void OnDestroy()
        {
            if (instancia.valid) instancia.Remove();
            if (datos != null) Destroy(datos);
        }
    }
}
