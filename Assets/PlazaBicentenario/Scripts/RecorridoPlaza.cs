using UnityEngine;
using UnityEngine.InputSystem;

namespace SupervivenciaEnLaPaz
{
    [DisallowMultipleComponent, RequireComponent(typeof(CharacterController))]
    public sealed class RecorridoPlaza : MonoBehaviour
    {
        // Se conservan los nombres serializados del controlador original.
        [SerializeField, Min(0.1f)] private float velocidad = 3.2f;
        [SerializeField, Min(0.1f)] private float velocidadCarrera = 5.8f;
        [Header("Locomocion estilo GTA")]
        [SerializeField, Min(0.1f)] private float velocidadCaminar = 1.8f;
        [SerializeField, Min(0.1f)] private float aceleracion = 12f;
        [SerializeField, Min(0.1f)] private float frenado = 18f;
        [SerializeField, Min(1f)] private float giroPorSegundo = 720f;
        [SerializeField, Min(0.1f)] private float alturaSalto = 1.0f;
        [SerializeField, Range(0.1f, 1f)] private float controlAereo = 0.35f;
        [SerializeField] private bool mostrarHudGTA = true;

        public enum EstadoLocomocion { Reposo, Caminar, Correr, Sprint, Saltar, Caer, Combate }
        public EstadoLocomocion EstadoActual { get; private set; }
        public float VelocidadActual { get; private set; }
        public bool EnSuelo => controlador != null && controlador.isGrounded;

        private CharacterController controlador;
        private Animator animador;
        private VistaPlazaBicentenario vista;
        private Transform rival;
        private float velocidadVertical;
        private Vector3 velocidadHorizontal;
        private bool paramSpeed, paramCombate, paramGrounded, paramVerticalSpeed, paramJump;

        private void Awake()
        {
            controlador = GetComponent<CharacterController>();
            animador = GetComponent<Animator>();
            if (animador != null)
            {
                // El CharacterController es el unico responsable del desplazamiento.
                animador.applyRootMotion = false;
                foreach (var p in animador.parameters)
                {
                    if (p.name == "Speed" && p.type == AnimatorControllerParameterType.Float) paramSpeed = true;
                    if (p.name == "EnCombate" && p.type == AnimatorControllerParameterType.Bool) paramCombate = true;
                    if (p.name == "Grounded" && p.type == AnimatorControllerParameterType.Bool) paramGrounded = true;
                    if (p.name == "VerticalSpeed" && p.type == AnimatorControllerParameterType.Float) paramVerticalSpeed = true;
                    if (p.name == "Jump" && p.type == AnimatorControllerParameterType.Trigger) paramJump = true;
                }
            }
        }

        private void Start()
        {
            var camara = Camera.main;
            if (camara != null) vista = camara.GetComponent<VistaPlazaBicentenario>();
            if (vista != null) vista.Seguir(transform);
            else Debug.LogWarning("RecorridoPlaza: no se encontro VistaPlazaBicentenario en Main Camera.", this);
            if (mostrarHudGTA && GetComponent<HudGTA>() == null) gameObject.AddComponent<HudGTA>();
        }

        // API conservada para CombateArena.
        internal void FijarRival(Transform objetivo)
        {
            rival = objetivo;
            if (animador != null && paramCombate) animador.SetBool("EnCombate", rival != null);
            if (vista != null) vista.EnfocarRival(objetivo);
        }

        // API conservada para GestorMisiones.
        internal void Reubicar(Vector3 punto, Vector3 mirarHacia)
        {
            controlador.enabled = false;
            transform.position = punto + Vector3.up * 0.03f;
            Vector3 direccion = Vector3.ProjectOnPlane(mirarHacia - punto, Vector3.up);
            if (direccion.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(direccion);
            velocidadVertical = 0f;
            velocidadHorizontal = Vector3.zero;
            VelocidadActual = 0f;
            controlador.enabled = true;
            if (vista != null) vista.Seguir(transform);
        }

        private void Update()
        {
            if (!controlador.enabled) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Keyboard teclado = Keyboard.current;
            Vector3 entrada = Vector3.zero;
            if (teclado != null)
            {
                bool derecha = teclado.dKey.isPressed || teclado.rightArrowKey.isPressed;
                bool izquierda = teclado.aKey.isPressed || teclado.leftArrowKey.isPressed;
                bool adelante = teclado.wKey.isPressed || teclado.upArrowKey.isPressed;
                bool atras = teclado.sKey.isPressed || teclado.downArrowKey.isPressed;
                entrada = Vector3.ClampMagnitude(
                    new Vector3((derecha ? 1 : 0) - (izquierda ? 1 : 0), 0,
                                (adelante ? 1 : 0) - (atras ? 1 : 0)), 1f);
            }
            Vector3 direccion = (vista != null ? vista.OrientacionHorizontal : Quaternion.identity) * entrada;
            bool combate = rival != null || CombateArena.EnCombate;
            bool hayEntrada = entrada.sqrMagnitude > 0.001f;
            bool sprint = !combate && teclado != null && teclado.shiftKey.isPressed;
            bool caminar = !combate && !sprint && teclado != null && teclado.leftAltKey.isPressed;
            float paso = sprint ? velocidadCarrera : caminar ? velocidadCaminar : velocidad;

            bool sueloAnterior = controlador.isGrounded;
            Vector3 deseada = direccion * paso;
            if (combate && sueloAnterior)
                velocidadHorizontal = deseada; // conserva respuesta del duelo original
            else
                velocidadHorizontal = Vector3.MoveTowards(velocidadHorizontal, deseada,
                    (hayEntrada ? aceleracion : frenado) * (sueloAnterior ? 1f : controlAereo) * dt);

            Vector3 mirada = rival != null ? rival.position - transform.position :
                (hayEntrada ? direccion : Vector3.zero);
            mirada.y = 0f;
            if (mirada.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(mirada), giroPorSegundo * dt);

            float gravedad = Mathf.Min(-0.1f, Physics.gravity.y);
            if (sueloAnterior && velocidadVertical < 0f) velocidadVertical = -2f;
            bool salto = !combate && sueloAnterior && teclado != null && teclado.spaceKey.wasPressedThisFrame;
            if (salto)
            {
                velocidadVertical = Mathf.Sqrt(-2f * gravedad * alturaSalto);
                if (animador != null && paramJump) animador.SetTrigger("Jump");
            }
            velocidadVertical += gravedad * dt;
            CollisionFlags contacto = controlador.Move((velocidadHorizontal + Vector3.up * velocidadVertical) * dt);
            if ((contacto & CollisionFlags.Above) != 0 && velocidadVertical > 0f) velocidadVertical = 0f;
            if ((contacto & CollisionFlags.Below) != 0 && velocidadVertical < 0f) velocidadVertical = -2f;

            VelocidadActual = Vector3.ProjectOnPlane(controlador.velocity, Vector3.up).magnitude;
            if (combate) EstadoActual = EstadoLocomocion.Combate;
            else if (salto || (!EnSuelo && velocidadVertical > 0f)) EstadoActual = EstadoLocomocion.Saltar;
            else if (!EnSuelo) EstadoActual = EstadoLocomocion.Caer;
            else if (VelocidadActual < 0.08f) EstadoActual = EstadoLocomocion.Reposo;
            else if (caminar) EstadoActual = EstadoLocomocion.Caminar;
            else if (sprint) EstadoActual = EstadoLocomocion.Sprint;
            else EstadoActual = EstadoLocomocion.Correr;

            if (animador != null)
            {
                // Mantiene la escala Speed del Animator existente (1 normal, 1.55 carrera).
                float valorSpeed = Mathf.Clamp(VelocidadActual / Mathf.Max(0.1f, velocidad), 0f, 1f);
                if (VelocidadActual > velocidad && velocidadCarrera > velocidad)
                    valorSpeed = Mathf.Lerp(1f, 1.55f, Mathf.InverseLerp(velocidad, velocidadCarrera, VelocidadActual));
                if (paramSpeed) animador.SetFloat("Speed", valorSpeed, 0.08f, dt);
                // Solo se escriben parametros que existen. Los clips no se crean por codigo.
                if (paramGrounded) animador.SetBool("Grounded", EnSuelo);
                if (paramVerticalSpeed) animador.SetFloat("VerticalSpeed", velocidadVertical);
            }
        }

        private void OnDisable()
        {
            velocidadHorizontal = Vector3.zero;
            VelocidadActual = 0f;
            EstadoActual = EstadoLocomocion.Reposo;
        }
    }
}
