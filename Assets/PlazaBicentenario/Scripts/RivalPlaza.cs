using UnityEngine;
using UnityEngine.AI;

namespace SupervivenciaEnLaPaz
{
    [RequireComponent(typeof(CharacterController), typeof(Animator))]
    public sealed class RivalPlaza : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float velocidad = 3.6f;

        private CharacterController controlador;
        private Animator animador;
        private Transform objetivo;
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
            if (!NavegacionPlaza.Instancia.CalcularRuta(transform.position, objetivo.position, ruta))
                return;
            cantidad = ruta.GetCornersNonAlloc(esquinas);
            siguiente = cantidad > 1 ? 1 : 0;
        }

        private float DistanciaHorizontal(Vector3 punto)
        {
            return Vector3.ProjectOnPlane(punto - transform.position, Vector3.up).magnitude;
        }
    }
}
