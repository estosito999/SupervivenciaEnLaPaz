using SupervivenciaEnLaPaz.Core;
using SupervivenciaEnLaPaz.Utilidades;
using UnityEngine;

namespace SupervivenciaEnLaPaz
{
    /// <summary>
    /// Fachada de compatibilidad para el controlador del jugador en la Plaza del Bicentenario.
    /// Delega la ejecución real a <see cref="Ped"/> y <see cref="PlayerController"/> para evitar
    /// duplicar movimiento, rotación o animación sobre el CharacterController.
    /// </summary>
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

        public enum EstadoLocomocion
        {
            Reposo,
            Caminar,
            Correr,
            Sprint,
            Saltar,
            Caer,
            Combate,
            Agachado,
            CaminarAgachado,
            Rodar,
            Esquivar,
            Golpeado
        }

        public EstadoLocomocion EstadoActual => ped != null ? ped.EstadoActual : EstadoLocomocion.Reposo;
        public float VelocidadActual => ped != null ? ped.VelocidadHorizontalActual : 0f;
        public bool EnSuelo => ped != null ? ped.EnSuelo : (controlador != null && controlador.isGrounded);
        public Ped Ped => ped;

        private CharacterController controlador;
        private Ped ped;
        private PlayerController controladorJugador;
        private VistaPlazaBicentenario vista;

        private void Awake()
        {
            controlador = GetComponent<CharacterController>();
            ped = gameObject.GetOrAddComponent<Ped>();
            ped.Rol = PedestrianRole.Jugador;
            ped.ConfigurarParametros(velocidadCaminar, velocidad, velocidadCarrera, aceleracion, frenado,
                giroPorSegundo, alturaSalto, controlAereo);

            controladorJugador = gameObject.GetOrAddComponent<PlayerController>();
            EstadoMundoPlaza.ObtenerOCrear();
        }

        private void Start()
        {
            Camera camara = Camera.main;
            if (camara != null)
                vista = camara.GetComponent<VistaPlazaBicentenario>();

            if (vista != null)
            {
                vista.Seguir(transform);
                if (controladorJugador != null)
                    controladorJugador.EstablecerVista(vista);
            }
            else
            {
                Debug.LogWarning("RecorridoPlaza: no se encontró VistaPlazaBicentenario en Main Camera.", this);
            }

            if (mostrarHudGTA)
            {
                gameObject.GetOrAddComponent<HudGTA>();
                gameObject.GetOrAddComponent<MinimapaGTA>();
            }
        }

        private void OnValidate()
        {
            if (ped != null)
                ped.ConfigurarParametros(velocidadCaminar, velocidad, velocidadCarrera, aceleracion, frenado,
                    giroPorSegundo, alturaSalto, controlAereo);
        }

        // API conservada para CombateArena.
        internal void FijarRival(Transform objetivo)
        {
            if (ped != null)
                ped.FijarRival(objetivo);

            if (vista != null)
                vista.EnfocarRival(objetivo);
        }

        // API conservada para GestorMisiones.
        internal void Reubicar(Vector3 punto, Vector3 mirarHacia)
        {
            if (ped != null)
                ped.Reubicar(punto, mirarHacia, true);

            if (vista != null)
                vista.Seguir(transform);
        }

        private void OnDisable()
        {
            if (ped != null && !enabled)
                ped.ResetearEntrada();
        }
    }
}
