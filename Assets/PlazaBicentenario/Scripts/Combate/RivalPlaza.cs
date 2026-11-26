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
        private Vector3 desplazamiento;
        private Vector3 destinoFijo;
        private float paso;
        private bool enMarcha;
        private NavMeshPath ruta;
        private readonly Vector3[] esquinas = new Vector3[32];
        private int cantidad;
        private int siguiente;
        private float recalcularEn;
        private float distanciaParada;
        private float velocidadVertical;

        private void Awake()
        {
            controlador = GetComponent<CharacterController>();
            animador = GetComponent<Animator>();
            ruta = new NavMeshPath();
        }

        internal void Perseguir(Transform jugador, float distancia, Vector3 flanco = default)
        {
            objetivo = jugador;
            desplazamiento = flanco;
            distanciaParada = distancia;
            paso = velocidad;
            enMarcha = true;
            recalcularEn = 0f;
        }

        internal bool IrA(Vector3 punto, float velocidadHuida)
        {
            objetivo = null;
            destinoFijo = punto;
            distanciaParada = 0.22f;
            paso = velocidadHuida;
            enMarcha = ActualizarRuta(punto);
            recalcularEn = Time.time + 0.5f;
            return enMarcha;
        }

        private void Update()
        {
            if (!enMarcha) return;
            Vector3 hasta = objetivo != null ? objetivo.position + desplazamiento : destinoFijo;
            if (Time.time >= recalcularEn)
            {
                ActualizarRuta(hasta);
                recalcularEn = Time.time + (objetivo != null ? 0.22f : 0.5f);
            }

            float dt = Time.deltaTime;
            Vector3 avance = Vector3.zero;
            if (cantidad > 0 && DistanciaHorizontal(hasta) > distanciaParada)
            {
                while (siguiente < cantidad - 1 && DistanciaHorizontal(esquinas[siguiente]) < 0.24f)
                    siguiente++;
                Vector3 tramo = Vector3.ProjectOnPlane(esquinas[siguiente] - transform.position, Vector3.up);
                avance = Vector3.ClampMagnitude(tramo, paso * dt);
            }

            Vector3 mirada = objetivo != null && (objetivo.position - transform.position).sqrMagnitude < 6.25f
                ? Vector3.ProjectOnPlane(objetivo.position - transform.position, Vector3.up) : avance.normalized;
            if (mirada.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(mirada), 720f * dt);

            if (controlador.isGrounded && velocidadVertical < 0f) velocidadVertical = -2f;
            velocidadVertical += Physics.gravity.y * dt;
            controlador.Move(avance + Vector3.up * (velocidadVertical * dt));
            animador.SetFloat("Speed", dt > 0f ? avance.magnitude / (velocidad * dt) : 0f, 0.08f, dt);
        }

        private bool ActualizarRuta(Vector3 hasta)
        {
            cantidad = siguiente = 0;
            if (!NavegacionPlaza.Instancia.CalcularRuta(transform.position, hasta, ruta))
                return false;
            cantidad = ruta.GetCornersNonAlloc(esquinas);
            siguiente = cantidad > 1 ? 1 : 0;
            return cantidad > 0;
        }

        private float DistanciaHorizontal(Vector3 punto)
        {
            return Vector3.ProjectOnPlane(punto - transform.position, Vector3.up).magnitude;
        }
    }
}
