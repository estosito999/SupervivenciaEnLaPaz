using System.Collections.Generic;
using SupervivenciaEnLaPaz.Core;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SupervivenciaEnLaPaz
{
    [DefaultExecutionOrder(110), DisallowMultipleComponent]
    public sealed class GestorMisiones : MonoBehaviour
    {
        public enum Mision { LlegadaCafe, PeleaMonoblock, PersecucionVillazon, EmboscadaCafe, Finalizado }
        public Mision Actual { get; private set; }

        // Lecturas reales expuestas para HUD / Minimapa sin duplicar la lógica de misiones.
        public bool TieneObjetivoActivo => marcador != null && marcador.gameObject.activeSelf;
        public Vector3 ObjetivoActualPosicion => ObtenerPosicionObjetivo();
        public Transform LadronActivo => ladron != null ? ladron.transform : null;
        public CombateArena Combate => combate;
        public string NombreMisionCorto => Actual switch
        {
            Mision.LlegadaCafe => "M1 · Café Ciudad",
            Mision.PeleaMonoblock => "M2 · Gradas Monoblock",
            Mision.PersecucionVillazon => "M3 · Villazón",
            Mision.EmboscadaCafe => "M4 · Emboscada",
            _ => "Jornada completa"
        };

        [Header("Encuentro y HUD")]
        [SerializeField] private CombateArena combate;
        [SerializeField] private Text titulo;
        [SerializeField] private Text instruccion;
        [SerializeField] private Material materialMarcador;
        [SerializeField] private RivalPlaza adversario;
        [SerializeField] private GameObject prefabMochila;
        [SerializeField] private RuntimeAnimatorController animacionHuida;

        [Header("Posiciones locales de la plaza")]
        [SerializeField] private Vector3 puntoCafe = new(-14.65f, 0.02f, -14.8f);
        [SerializeField, Min(0.3f)] private float radioCafe = 1.6f;
        [SerializeField] private Vector3[] puntosEmboscada = { new(-11.35f, 0f, -13.4f), new(-20.7f, 0f, -16.7f) };
        [SerializeField] private Vector3[] rutaVillazon = {
            new(5.4f, 0f, 7.5f), new(10.3f, 0f, 3.9f), new(6.6f, 0f, -3.95f), new(0.6f, 0f, -7.6f),
            new(-7.1f, 0f, -2.4f), new(-12.15f, 0f, 3.05f), new(-18f, 0.55f, 3.05f),
            new(-25.2f, 0.55f, 0.65f), new(-23.35f, 0.55f, 8f), new(-17.2f, 0.55f, 6.9f)
        };
        [SerializeField, Min(1f)] private float tiempoLimite = 40f;
        [SerializeField, Min(1f)] private float velocidadLadron = 4.35f;

        private MarcadorMision marcador;
        private Camera camara;
        private NavMeshPath comprobacion;
        private LadronVillazon ladron;
        private GameObject mochila;
        private float finPersecucion;
        private int segundosMostrados;
        private int rivalesMostrados;
        private bool reintentoPendiente;
        private bool debeSalirDelCafe;
        private bool derrotado;
        private Vector3 Cafe => transform.TransformPoint(puntoCafe);

        private void Awake()
        {
            comprobacion = new NavMeshPath();
            camara = Camera.main;
            marcador = new GameObject("Marcador de misión").AddComponent<MarcadorMision>();
            marcador.transform.SetParent(transform, false);
            marcador.Dibujar(materialMarcador);
            combate.PrepararCampana();
            EstadoMundoPlaza.ObtenerOCrear();
        }

        private void OnEnable()
        {
            CombateArena.CombateIniciado += AlIniciarCombate;
            CombateArena.CombateTerminado += AlTerminarCombate;
            combate.LadronGolpeado += RecuperarMochila;
        }

        private void OnDisable()
        {
            CombateArena.CombateIniciado -= AlIniciarCombate;
            CombateArena.CombateTerminado -= AlTerminarCombate;
            combate.LadronGolpeado -= RecuperarMochila;
        }

        private void Start()
        {
            Actual = Mision.LlegadaCafe;
            Mostrar("Misión 1 · Llegada al Bicentenario", "Camina con WASD o las flechas hasta el aro junto a las mesas del Café Ciudad.");
            marcador.Indicar(Cafe, camara);
        }

        private void Update()
        {
            if (derrotado) return;
            if (reintentoPendiente)
            {
                if (Keyboard.current?.rKey.wasPressedThisFrame == true)
                {
                    reintentoPendiente = false;
                    combate.OcultarResultado();
                    if (Actual == Mision.PersecucionVillazon) IniciarPersecucion(true);
                    else IniciarEmboscada();
                }
                return;
            }
            switch (Actual)
            {
                case Mision.LlegadaCafe:
                    if (DentroDelCafe())
                    {
                        Actual = Mision.PeleaMonoblock;
                        Mostrar("Misión 2 · Conflicto en las Gradas del Monoblock",
                            "Llegaste al café. Busca al rival al pie de las gradas.");
                        combate.HabilitarDuelo();
                    }
                    break;
                case Mision.PersecucionVillazon:
                    if (Time.time >= finPersecucion)
                    {
                        RetirarLadron();
                        PedirReintento("El ladrón escapó", "Pulsa R para reintentar la persecución.");
                    }
                    else if (ladron.CapturadoPor(combate.Jugador)) RecuperarMochila();
                    else ActualizarReloj();
                    break;
                case Mision.EmboscadaCafe:
                    if (CombateArena.EnCombate)
                    {
                        if (rivalesMostrados != combate.RivalesEnPie)
                        {
                            rivalesMostrados = combate.RivalesEnPie;
                            instruccion.text = $"Rivales en pie · {rivalesMostrados}. Usa las mesas y barandas como cobertura.";
                        }
                    }
                    else if (!DentroDelCafe()) debeSalirDelCafe = false;
                    else if (!debeSalirDelCafe) IniciarEmboscada();
                    break;
                case Mision.Finalizado:
                    if (Keyboard.current?.rKey.wasPressedThisFrame == true)
                        SceneManager.LoadScene(gameObject.scene.buildIndex);
                    break;
            }
        }

        private void LateUpdate()
        {
            if (!marcador.gameObject.activeSelf) return;
            marcador.Indicar(ObtenerPosicionObjetivo(), camara);
        }

        private Vector3 ObtenerPosicionObjetivo()
        {
            Vector3 objetivo = Cafe;
            if (CombateArena.EnCombate && combate.ObjetivoActual != null) objetivo = combate.ObjetivoActual.position;
            else if (Actual == Mision.PeleaMonoblock) objetivo = combate.RivalInicial.position;
            else if (Actual == Mision.PersecucionVillazon && ladron != null) objetivo = ladron.transform.position;
            return objetivo;
        }

        private bool DentroDelCafe() => (combate.Jugador.position - Cafe).sqrMagnitude <= radioCafe * radioCafe;

        private void AlIniciarCombate(Vector3 centro)
        {
            if (Actual == Mision.PeleaMonoblock)
                instruccion.text = "Derrota al rival. Pulsa F o clic izquierdo para golpear, clic derecho para esquivar.";
        }

        private void AlTerminarCombate()
        {
            if (!combate.VictoriaDelJugador)
            {
                derrotado = true;
                marcador.gameObject.SetActive(false);
                return;
            }
            if (Actual == Mision.PeleaMonoblock)
            {
                EstadoMundoPlaza.ObtenerOCrear().SumarDinero(30);
                IniciarPersecucion(false);
            }
            else if (Actual == Mision.EmboscadaCafe)
            {
                EstadoMundoPlaza.ObtenerOCrear().SumarDinero(100);
                Actual = Mision.Finalizado;
                marcador.gameObject.SetActive(false);
                Mostrar("Jornada completada", "Recuperaste la mochila y defendiste el Café Ciudad.");
                combate.MostrarResultado("Victoria de la Jornada", "Completaste las cuatro misiones. Pulsa R para volver a jugar.");
            }
        }

        private void IniciarPersecucion(bool reintento)
        {
            Actual = Mision.PersecucionVillazon;
            Mostrar("Misión 3 · Persecución por la Villazón",
                "Corre con Mayús. Recupera la mochila a menos de 1,5 m por 1 s o golpea con F.");
            if (reintento && rutaVillazon.Length >= 2 &&
                NavegacionPlaza.Instancia.Muestrear(transform.TransformPoint(new Vector3(0, 0, 6)), out var salida))
                combate.Jugador.GetComponent<RecorridoPlaza>().Reubicar(salida, transform.TransformPoint(rutaVillazon[0]));

            if (rutaVillazon.Length < 2 || !PuntoAccesible(rutaVillazon[0], out var origen))
            {
                PedirReintento("Ruta de huida no disponible", "Pulsa R para reintentar la persecución.");
                return;
            }
            var actor = CrearAdversario(origen, "Ladrón de la Villazón", new Color(0.92f, 0.55f, 0.12f));
            actor.GetComponent<Animator>().runtimeAnimatorController = animacionHuida;
            ladron = actor.gameObject.AddComponent<LadronVillazon>();
            var puntos = new Vector3[rutaVillazon.Length];
            for (int i = 0; i < puntos.Length; i++) puntos[i] = transform.TransformPoint(rutaVillazon[i]);
            if (!ladron.Iniciar(puntos, velocidadLadron))
            {
                RetirarLadron();
                PedirReintento("Ruta de huida no disponible", "Pulsa R para reintentar la persecución.");
                return;
            }
            mochila = Instantiate(prefabMochila);
            ColocarMochila(actor.transform);
            combate.MarcarLadron(actor.transform);
            finPersecucion = Time.time + tiempoLimite;
            segundosMostrados = -1;
            marcador.gameObject.SetActive(true);
            EstadoMundoPlaza.ObtenerOCrear().RegistrarDisturbio(1);
            ActualizarReloj();
        }

        private void ActualizarReloj()
        {
            int segundos = Mathf.Max(0, Mathf.CeilToInt(finPersecucion - Time.time));
            if (segundos == segundosMostrados) return;
            segundosMostrados = segundos;
            titulo.text = $"Misión 3 · Persecución por la Villazón   {segundos / 60:00}:{segundos % 60:00}";
        }

        private void RecuperarMochila()
        {
            if (Actual != Mision.PersecucionVillazon || ladron == null || Time.time >= finPersecucion) return;
            ColocarMochila(combate.Jugador);
            RetirarLadron();
            EstadoMundoPlaza.ObtenerOCrear().SumarDinero(50);
            EstadoMundoPlaza.ObtenerOCrear().ReducirAlerta();
            Actual = Mision.EmboscadaCafe;
            debeSalirDelCafe = DentroDelCafe();
            Mostrar("Misión 4 · Emboscada en el Café Ciudad", "Recuperaste la mochila. Regresa a las mesas del café.");
        }

        private void ColocarMochila(Transform actor)
        {
            var espalda = actor.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Chest);
            mochila.transform.SetPositionAndRotation(actor.TransformPoint(new Vector3(0, 1.17f, -0.18f)), actor.rotation);
            mochila.transform.SetParent(espalda != null ? espalda : actor, true);
        }

        private void RetirarLadron()
        {
            combate.MarcarLadron(null);
            ladron.gameObject.SetActive(false);
            Destroy(ladron.gameObject);
            ladron = null;
        }

        private void IniciarEmboscada()
        {
            var posiciones = new Vector3[puntosEmboscada.Length];
            if (posiciones.Length < 2 || posiciones.Length > 3)
            {
                PedirReintento("Faltan posiciones de la emboscada", "Configura dos o tres rivales junto al café.");
                return;
            }
            for (int i = 0; i < posiciones.Length; i++)
                if (!PuntoAccesible(puntosEmboscada[i], out posiciones[i]))
                {
                    PedirReintento("Acceso al café bloqueado", "Pulsa R para reintentar la emboscada.");
                    return;
                }
            var rivales = new List<RivalPlaza>(posiciones.Length);
            for (int i = 0; i < posiciones.Length; i++)
                rivales.Add(CrearAdversario(posiciones[i], $"Rival del café {i + 1}",
                    i % 2 == 0 ? new Color(0.52f, 0.12f, 0.17f) : new Color(0.12f, 0.33f, 0.37f)));
            rivalesMostrados = -1;
            marcador.gameObject.SetActive(true);
            combate.IniciarEmboscada(rivales);
        }

        private bool PuntoAccesible(Vector3 puntoLocal, out Vector3 punto)
        {
            var navegacion = NavegacionPlaza.Instancia;
            return navegacion.Muestrear(transform.TransformPoint(puntoLocal), out punto, 1.4f) &&
                navegacion.CalcularRuta(combate.Jugador.position, punto, comprobacion);
        }

        private RivalPlaza CrearAdversario(Vector3 punto, string nombre, Color chaqueta)
        {
            var actor = Instantiate(adversario, punto + Vector3.up * 0.04f, Quaternion.identity, transform);
            actor.name = nombre;
            var ropa = actor.GetComponentInChildren<SkinnedMeshRenderer>();
            var color = new MaterialPropertyBlock();
            color.SetColor("_BaseColor", chaqueta);
            ropa.SetPropertyBlock(color, 1);
            return actor;
        }

        private void PedirReintento(string encabezado, string detalle)
        {
            reintentoPendiente = true;
            marcador.gameObject.SetActive(false);
            combate.MostrarResultado(encabezado, detalle);
        }

        private void Mostrar(string nombre, string objetivo)
        {
            titulo.text = nombre;
            instruccion.text = objetivo;
        }
    }
}
