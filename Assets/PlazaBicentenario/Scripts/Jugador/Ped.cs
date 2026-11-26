// Adaptado e integrado para SupervivenciaEnLaPaz a partir de Ped.cs y Ped_Damage.cs de SanAndreasUnity.
using System;
using System.Collections.Generic;
using SupervivenciaEnLaPaz.Core;
using SupervivenciaEnLaPaz.Movimiento.States;
using SupervivenciaEnLaPaz.Utilidades;
using UnityEngine;

namespace SupervivenciaEnLaPaz
{
    /// <summary>
    /// Entidad física y de estados para personajes humanoides (jugador y NPCs).
    /// Centraliza la máquina de estados, el movimiento único sobre CharacterController,
    /// la resistencia física, el agachado y la reacción a daño.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(CharacterController))]
    public sealed class Ped : MonoBehaviour
    {
        private static readonly List<Ped> todos = new();
        public static IReadOnlyList<Ped> Todos => todos;
        public static Ped JugadorPrincipal { get; private set; }

        [Header("Identidad")]
        [SerializeField] private PedestrianRole rol = PedestrianRole.Civil;

        [Header("Locomoción")]
        [SerializeField, Min(0.1f)] private float velocidadCaminar = 1.8f;
        [SerializeField, Min(0.1f)] private float velocidadCorrer = 3.2f;
        [SerializeField, Min(0.1f)] private float velocidadSprint = 5.8f;
        [SerializeField, Min(0.1f)] private float velocidadAgachado = 1.35f;
        [SerializeField, Min(0.1f)] private float velocidadPanico = 3.1f;
        [SerializeField, Min(1f)] private float velocidadGiro = 720f;
        [SerializeField, Min(0.1f)] private float aceleracion = 12f;
        [SerializeField, Min(0.1f)] private float frenado = 18f;
        [SerializeField, Range(0.1f, 1f)] private float controlAereo = 0.35f;
        [SerializeField, Min(0.1f)] private float alturaSalto = 1.0f;

        [Header("Colisionador agachado")]
        [SerializeField, Min(0.6f)] private float alturaAgachado = 1.15f;
        [SerializeField, Min(2f)] private float velocidadTransicionAltura = 10f;

        private readonly StateMachine maquinaEstados = new();
        private readonly StateContainer<BaseMovementState> contenedorEstados = new();

        private CharacterController controlador;
        private PedModel modelo;
        private Damageable damageable;
        private Transform raizEstados;

        private float alturaOriginal;
        private Vector3 centroOriginal;
        private float alturaObjetivoCollider;
        private Vector3 velocidadHorizontal;
        private float velocidadVertical;
        private bool desplazamientoDirectoActivo;
        private Vector3 desplazamientoDirecto;
        private float tiempoInvulnerableRestante;
        private bool agachadoFisico;
        private bool agotadoResistencia;
        private float resistencia;

        public PedestrianRole Rol
        {
            get => rol;
            set
            {
                rol = value;
                if (rol == PedestrianRole.Jugador)
                    JugadorPrincipal = this;
                else if (JugadorPrincipal == this)
                    JugadorPrincipal = null;
            }
        }

        public CharacterController Controlador => controlador;
        public PedModel Modelo => modelo;
        public Damageable Salud => damageable;
        public PedManager GestorConfig => PedManager.Instancia;

        public BaseMovementState EstadoActualMaquina => maquinaEstados.CurrentState as BaseMovementState;
        public RecorridoPlaza.EstadoLocomocion EstadoActual =>
            EstadoActualMaquina != null ? EstadoActualMaquina.EstadoLegacy : RecorridoPlaza.EstadoLocomocion.Reposo;
        public string NombreEstadoActual =>
            EstadoActualMaquina != null ? EstadoActualMaquina.NombreEstadoHud : "Reposo";

        public float VelocidadCaminar => velocidadCaminar;
        public float VelocidadCorrer => velocidadCorrer;
        public float VelocidadSprint => velocidadSprint;
        public float VelocidadAgachado => velocidadAgachado;
        public float VelocidadPanico => velocidadPanico;

        public Vector3 EntradaMovimiento { get; set; }
        public Vector3 DireccionMirada { get; set; }
        public bool TieneEntradaMovimiento => EntradaMovimiento.sqrMagnitude > 0.001f;
        public bool IsWalkOn { get; set; }
        public bool IsRunOn { get; set; }
        public bool IsSprintOn { get; set; }
        public bool IsCrouchOn { get; set; }
        public bool IsPanicOn { get; set; }
        public bool IsJumpPressedThisFrame { get; set; }

        public Transform RivalFijado { get; private set; }
        public bool EstaEnCombate => RivalFijado != null || (rol == PedestrianRole.Jugador && CombateArena.EnCombate);
        public bool EstaAgachado => EstadoActualMaquina != null && EstadoActualMaquina.EsAgachado;
        public bool EsInvulnerable => tiempoInvulnerableRestante > 0f || (damageable != null && damageable.Invulnerable);

        public float Resistencia => resistencia;
        public float ResistenciaMaxima => GestorConfig.resistenciaMaxima;
        public float FraccionResistencia => ResistenciaMaxima > 0f ? Mathf.Clamp01(resistencia / ResistenciaMaxima) : 1f;
        public bool EstaAgotado => agotadoResistencia;
        public bool PuedeEsprintar =>
            !agotadoResistencia && !IsCrouchOn && !EstaEnCombate && resistencia > 0.5f;

        public float VelocidadHorizontalActual { get; private set; }
        public float VelocidadVertical => velocidadVertical;
        public bool EnSuelo => controlador != null && controlador.isGrounded;

        public event Action AlAterrizarEvento;

        private void Awake()
        {
            controlador = GetComponent<CharacterController>();
            modelo = gameObject.GetOrAddComponent<PedModel>();
            modelo.InicializarComponentes();
            damageable = gameObject.GetOrAddComponent<Damageable>();

            alturaOriginal = controlador.height;
            centroOriginal = controlador.center;
            alturaObjetivoCollider = alturaOriginal;
            resistencia = GestorConfig.resistenciaMaxima;

            InicializarEstados();
            damageable.AlRecibirDano += AlRecibirDanoInterno;
        }

        private void OnEnable()
        {
            todos.AddIfNotPresent(this);
            if (rol == PedestrianRole.Jugador)
                JugadorPrincipal = this;

            if (maquinaEstados.CurrentState == null)
                CambiarEstado<StandState>();
        }

        private void OnDisable()
        {
            todos.Remove(this);
            if (JugadorPrincipal == this)
                JugadorPrincipal = null;

            velocidadHorizontal = Vector3.zero;
            VelocidadHorizontalActual = 0f;
            desplazamientoDirectoActivo = false;
            ResetearEntrada();
        }

        private void OnDestroy()
        {
            if (damageable != null)
                damageable.AlRecibirDano -= AlRecibirDanoInterno;
        }

        private void InicializarEstados()
        {
            Transform existente = transform.Find("EstadosMovimiento");
            if (existente != null)
            {
                raizEstados = existente;
            }
            else
            {
                var go = new GameObject("EstadosMovimiento");
                raizEstados = go.transform;
                raizEstados.SetParent(transform, false);
            }

            BaseMovementState[] estados =
            {
                raizEstados.gameObject.GetOrAddComponent<StandState>(),
                raizEstados.gameObject.GetOrAddComponent<WalkState>(),
                raizEstados.gameObject.GetOrAddComponent<RunState>(),
                raizEstados.gameObject.GetOrAddComponent<SprintState>(),
                raizEstados.gameObject.GetOrAddComponent<JumpState>(),
                raizEstados.gameObject.GetOrAddComponent<CrouchState>(),
                raizEstados.gameObject.GetOrAddComponent<CrouchMoveState>(),
                raizEstados.gameObject.GetOrAddComponent<RollState>(),
                raizEstados.gameObject.GetOrAddComponent<DodgeState>(),
                raizEstados.gameObject.GetOrAddComponent<HitReactionState>(),
                raizEstados.gameObject.GetOrAddComponent<PanicState>()
            };

            for (int i = 0; i < estados.Length; i++)
                estados[i].AsignarPed(this);

            contenedorEstados.AddStates(estados);
        }

        public void ConfigurarParametros(
            float caminar,
            float correr,
            float sprint,
            float accel,
            float freno,
            float giroSeg,
            float salto,
            float aire)
        {
            velocidadCaminar = Mathf.Max(0.1f, caminar);
            velocidadCorrer = Mathf.Max(0.1f, correr);
            velocidadSprint = Mathf.Max(0.1f, sprint);
            aceleracion = Mathf.Max(0.1f, accel);
            frenado = Mathf.Max(0.1f, freno);
            velocidadGiro = Mathf.Max(1f, giroSeg);
            alturaSalto = Mathf.Max(0.1f, salto);
            controlAereo = Mathf.Clamp(aire, 0.1f, 1f);
        }

        public void ConfigurarVelocidadesNPC(float caminar, float correr, float panico, float giroSeg)
        {
            velocidadCaminar = Mathf.Max(0.1f, caminar);
            velocidadCorrer = Mathf.Max(0.1f, correr);
            velocidadPanico = Mathf.Max(0.1f, panico);
            velocidadGiro = Mathf.Max(1f, giroSeg);
        }

        public BaseMovementState ObtenerEstado(Type tipo) => contenedorEstados.GetState(tipo);
        public T ObtenerEstado<T>() where T : BaseMovementState => contenedorEstados.GetState<T>();

        public bool CambiarEstado(Type tipo)
        {
            BaseMovementState estado = contenedorEstados.GetStateOrLogError(tipo);
            return estado != null && maquinaEstados.SwitchState(estado);
        }

        public bool CambiarEstado<T>() where T : BaseMovementState => CambiarEstado(typeof(T));

        public bool CambiarEstadoConParametro<T>(object parametro) where T : BaseMovementState
        {
            BaseMovementState estado = contenedorEstados.GetStateOrLogError<T>();
            return estado != null && maquinaEstados.SwitchStateWithParameter(estado, parametro);
        }

        public void ResetearEntrada()
        {
            EntradaMovimiento = Vector3.zero;
            IsWalkOn = false;
            IsRunOn = false;
            IsSprintOn = false;
            IsPanicOn = false;
            IsJumpPressedThisFrame = false;
        }

        public void FijarRival(Transform objetivo)
        {
            RivalFijado = objetivo;
            if (RivalFijado != null && IsCrouchOn)
            {
                IsCrouchOn = false;
                EstablecerModoAgachado(false);
            }
            modelo?.EstablecerEnCombate(EstaEnCombate);
        }

        public void Reubicar(Vector3 punto, Vector3 mirarHacia, bool conDesfaseSuperior = true)
        {
            bool estabaActivo = controlador.enabled;
            controlador.enabled = false;
            transform.position = conDesfaseSuperior ? punto + Vector3.up * 0.03f : punto;

            Vector3 dir = Vector3.ProjectOnPlane(mirarHacia - punto, Vector3.up);
            if (dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(dir);
                DireccionMirada = dir.normalized;
            }

            Physics.SyncTransforms();
            velocidadVertical = 0f;
            velocidadHorizontal = Vector3.zero;
            VelocidadHorizontalActual = 0f;
            desplazamientoDirectoActivo = false;
            IsCrouchOn = false;
            EstablecerModoAgachado(false);
            controlador.enabled = estabaActivo;
            CambiarEstado<StandState>();
        }

        public void EstablecerModoAgachado(bool agachado)
        {
            agachadoFisico = agachado;
            alturaObjetivoCollider = agachado
                ? Mathf.Clamp(alturaAgachado, 0.7f, alturaOriginal)
                : alturaOriginal;
        }

        public bool PuedeLevantarse()
        {
            if (controlador == null || alturaOriginal <= alturaAgachado + 0.05f)
                return true;

            float radio = Mathf.Max(0.1f, controlador.radius * 0.85f);
            Vector3 baseEsfera = transform.position + Vector3.up * (alturaAgachado - radio);
            float distancia = Mathf.Max(0.05f, alturaOriginal - alturaAgachado);
            return !Physics.SphereCast(baseEsfera, radio, Vector3.up, out RaycastHit hit, distancia,
                ~(1 << gameObject.layer), QueryTriggerInteraction.Ignore) ||
                   hit.transform == transform || hit.transform.IsChildOf(transform);
        }

        public void ConsumirResistencia(float cantidad)
        {
            if (cantidad <= 0f)
                return;

            resistencia = Mathf.Max(0f, resistencia - cantidad);
            if (resistencia <= 0.01f)
                agotadoResistencia = true;
        }

        public void EstablecerInvulnerabilidadTemporal(float duracion)
        {
            tiempoInvulnerableRestante = Mathf.Max(tiempoInvulnerableRestante, duracion);
        }

        public void EstablecerDesplazamientoDirecto(Vector3 velocidadMundo)
        {
            desplazamientoDirecto = velocidadMundo;
            desplazamientoDirecto.y = 0f;
            desplazamientoDirectoActivo = true;
        }

        public void AplicarImpulsoSalto()
        {
            float gravedad = Mathf.Min(-0.1f, Physics.gravity.y);
            velocidadVertical = Mathf.Sqrt(-2f * gravedad * alturaSalto);
        }

        internal void AlAterrizar() => AlAterrizarEvento?.Invoke();

        public void AlPulsarSalto() => EstadoActualMaquina?.AlPulsarSalto();
        public void AlPulsarAgacharse() => EstadoActualMaquina?.AlPulsarAgacharse();
        public void AlPulsarRodar(bool izquierda) => EstadoActualMaquina?.AlPulsarRodar(izquierda);
        public void AlPulsarEsquivar(Vector3 direccionMundo) => EstadoActualMaquina?.AlPulsarEsquivar(direccionMundo);

        public float RecibirImpacto(DamageInfo info)
        {
            if (damageable == null || info == null)
                return 0f;

            if (EsInvulnerable && !info.IgnorarEsquiva)
                return 0f;

            return damageable.RecibirDano(info);
        }

        private void AlRecibirDanoInterno(DamageInfo info, float aplicado)
        {
            if (aplicado > 0f)
                EstadoActualMaquina?.AlRecibirDano(info);
        }

        private void Update()
        {
            if (controlador == null || !controlador.enabled)
                return;

            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;

            if (tiempoInvulnerableRestante > 0f)
                tiempoInvulnerableRestante = Mathf.Max(0f, tiempoInvulnerableRestante - dt);

            desplazamientoDirectoActivo = false;
            EstadoActualMaquina?.UpdateState();

            ActualizarResistencia(dt);
            ActualizarAlturaCollider(dt);
            ActualizarRotacion(dt);
            ActualizarMovimientoFisico(dt);

            IsJumpPressedThisFrame = false;
        }

        private void LateUpdate()
        {
            EstadoActualMaquina?.LateUpdateState();
        }

        private void FixedUpdate()
        {
            EstadoActualMaquina?.FixedUpdateState();
        }

        private void ActualizarResistencia(float dt)
        {
            bool consume = EstadoActualMaquina != null && EstadoActualMaquina.ConsumeResistencia;
            if (!consume)
            {
                float rec = GestorConfig.recuperacionResistencia * (TieneEntradaMovimiento ? 0.65f : 1f);
                resistencia = Mathf.Min(ResistenciaMaxima, resistencia + rec * dt);
                if (agotadoResistencia && resistencia >= GestorConfig.umbralMinimoSprint)
                    agotadoResistencia = false;
            }
        }

        private void ActualizarAlturaCollider(float dt)
        {
            if (Mathf.Abs(controlador.height - alturaObjetivoCollider) < 0.005f)
                return;

            float nuevaAltura = Mathf.MoveTowards(controlador.height, alturaObjetivoCollider, velocidadTransicionAltura * dt);
            controlador.height = nuevaAltura;
            float factor = alturaOriginal > 0.01f ? nuevaAltura / alturaOriginal : 1f;
            controlador.center = new Vector3(centroOriginal.x, centroOriginal.y * factor, centroOriginal.z);
        }

        private void ActualizarRotacion(float dt)
        {
            if (EstadoActualMaquina != null && !EstadoActualMaquina.PermiteGiro)
                return;

            Vector3 mirada = Vector3.zero;
            if (RivalFijado != null)
                mirada = RivalFijado.position - transform.position;
            else if (DireccionMirada.sqrMagnitude > 0.001f)
                mirada = DireccionMirada;
            else if (TieneEntradaMovimiento)
                mirada = EntradaMovimiento;

            mirada.y = 0f;
            if (mirada.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    Quaternion.LookRotation(mirada.normalized),
                    velocidadGiro * dt);
            }
        }

        private void ActualizarMovimientoFisico(float dt)
        {
            bool sueloAnterior = controlador.isGrounded;

            if (desplazamientoDirectoActivo)
            {
                velocidadHorizontal = desplazamientoDirecto;
            }
            else
            {
                float velocidadObj = EstadoActualMaquina != null ? EstadoActualMaquina.CalcularVelocidadObjetivo() : 0f;
                Vector3 dir = EntradaMovimiento.WithXAndZ();
                if (dir.sqrMagnitude > 1f)
                    dir.Normalize();

                Vector3 deseada = dir * velocidadObj;
                if (EstaEnCombate && sueloAnterior)
                {
                    velocidadHorizontal = deseada;
                }
                else
                {
                    float tasa = TieneEntradaMovimiento ? aceleracion : frenado;
                    if (!sueloAnterior)
                        tasa *= controlAereo;
                    velocidadHorizontal = Vector3.MoveTowards(velocidadHorizontal, deseada, tasa * dt);
                }
            }

            float gravedad = Mathf.Min(-0.1f, Physics.gravity.y);
            if (sueloAnterior && velocidadVertical < 0f)
                velocidadVertical = -2f;

            velocidadVertical += gravedad * dt;

            CollisionFlags contacto = controlador.Move((velocidadHorizontal + Vector3.up * velocidadVertical) * dt);
            if ((contacto & CollisionFlags.Above) != 0 && velocidadVertical > 0f)
                velocidadVertical = 0f;
            if ((contacto & CollisionFlags.Below) != 0 && velocidadVertical < 0f)
                velocidadVertical = -2f;

            VelocidadHorizontalActual = Vector3.ProjectOnPlane(controlador.velocity, Vector3.up).magnitude;
        }
    }
}
