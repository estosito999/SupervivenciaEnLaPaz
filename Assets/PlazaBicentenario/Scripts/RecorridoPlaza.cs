using UnityEngine;
using UnityEngine.InputSystem;

namespace SupervivenciaEnLaPaz
{
    [DisallowMultipleComponent, RequireComponent(typeof(CharacterController))]
    public sealed class RecorridoPlaza : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float velocidad = 3.2f;
        [SerializeField, Min(0.1f)] private float velocidadCarrera = 5.8f;

        private CharacterController controlador;
        private Animator animador;
        private VistaPlazaBicentenario vista;
        private Transform rival;
        private float velocidadVertical;

        private void Awake()
        {
            controlador = GetComponent<CharacterController>();
            animador = GetComponent<Animator>();
        }

        private void Start()
        {
            vista = Camera.main.GetComponent<VistaPlazaBicentenario>();
            vista.Seguir(transform);
        }

        internal void FijarRival(Transform objetivo)
        {
            rival = objetivo;
            animador.SetBool("EnCombate", rival != null);
            vista.EnfocarRival(objetivo);
        }

        private void Update()
        {
            Keyboard teclado = Keyboard.current;
            Vector3 direccion = Vector3.zero;
            if (teclado != null)
            {
                bool derecha = teclado.dKey.isPressed || teclado.rightArrowKey.isPressed;
                bool izquierda = teclado.aKey.isPressed || teclado.leftArrowKey.isPressed;
                bool adelante = teclado.wKey.isPressed || teclado.upArrowKey.isPressed;
                bool atras = teclado.sKey.isPressed || teclado.downArrowKey.isPressed;
                direccion = Vector3.ClampMagnitude(
                    new Vector3((derecha ? 1 : 0) - (izquierda ? 1 : 0), 0, (adelante ? 1 : 0) - (atras ? 1 : 0)),
                    1f);
            }

            direccion = vista.OrientacionHorizontal * direccion;
            Vector3 mirada = rival != null ? rival.position - transform.position : direccion;
            mirada.y = 0f;
            float dt = Time.deltaTime;
            if (mirada.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(mirada), 720f * dt);

            if (controlador.isGrounded && velocidadVertical < 0)
                velocidadVertical = -2f;

            bool correr = rival == null && teclado != null && teclado.shiftKey.isPressed;
            float paso = correr ? velocidadCarrera : velocidad;
            velocidadVertical += Physics.gravity.y * dt;
            controlador.Move((direccion * paso + Vector3.up * velocidadVertical) * dt);
            animador.SetFloat("Speed", direccion.magnitude * (correr ? 1.55f : 1f), 0.08f, dt);
        }
    }
}
