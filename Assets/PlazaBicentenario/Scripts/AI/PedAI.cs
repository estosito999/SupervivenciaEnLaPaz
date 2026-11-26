// Adaptado e integrado para SupervivenciaEnLaPaz a partir de PedAI.cs de SanAndreasUnity.
using System.Collections.Generic;
using SupervivenciaEnLaPaz.AI.States;
using SupervivenciaEnLaPaz.Core;
using SupervivenciaEnLaPaz.Utilidades;
using UnityEngine;

namespace SupervivenciaEnLaPaz.AI
{
    /// <summary>
    /// Cerebro de decisión para NPCs y peatones de la Plaza del Bicentenario.
    /// Ejecuta la máquina de estados de IA (<see cref="IdleState"/>, <see cref="WalkAroundState"/>,
    /// <see cref="EscapeState"/>, <see cref="ChaseState"/>, <see cref="FollowState"/>) sobre el NavMesh local.
    /// </summary>
    [DefaultExecutionOrder(-5)]
    [DisallowMultipleComponent, RequireComponent(typeof(Ped))]
    public sealed class PedAI : MonoBehaviour
    {
        private static readonly List<PedAI> s_allPedAIs = new();
        public static IReadOnlyList<PedAI> AllPedAIs => s_allPedAIs;

        private readonly StateMachine _stateMachine = new();
        private float _timer2Segundos;

        public Ped MyPed { get; private set; }
        public NavegacionPlaza Navegacion { get; private set; }
        public GestorPeatones Gestor { get; private set; }
        public NPCPedSpawner Spawner { get; private set; }

        public PedestrianRole Rol => MyPed != null ? MyPed.Rol : PedestrianRole.Civil;
        public List<Ped> EnemyPeds { get; } = new();
        public BaseState CurrentState => _stateMachine.CurrentState as BaseState;
        public StateContainer<BaseState> StateContainer { get; } = new();

        public float DistanciaLlegada { get; private set; } = 0.55f;
        public float ReposoMinimo { get; private set; } = 1.2f;
        public float ReposoMaximo { get; private set; } = 5.5f;
        public float RadioReaccionCombate { get; private set; } = 11f;
        public float DuracionHuida { get; private set; } = 4.5f;
        public float RadioSeparacion { get; private set; } = 1.15f;
        public float EmpujeSeparacion { get; private set; } = 1.5f;

        private void Awake()
        {
            MyPed = GetComponent<Ped>();
            Navegacion = NavegacionPlaza.Instancia;

            BaseState[] states =
            {
                new IdleState(),
                new WalkAroundState(),
                new EscapeState(),
                new ChaseState(),
                new FollowState()
            };

            StateContainer.AddStates(states);
            for (int i = 0; i < states.Length; i++)
                states[i].OnAwake(this);

            _stateMachine.SwitchStateWithParameter(StateContainer.GetStateOrThrow<IdleState>(), Random.Range(0.25f, 2.2f));
        }

        private void OnEnable()
        {
            s_allPedAIs.AddIfNotPresent(this);
            if (MyPed != null && MyPed.Salud != null)
                MyPed.Salud.AlRecibirDano += OnMyPedDamaged;
        }

        private void OnDisable()
        {
            s_allPedAIs.Remove(this);
            if (MyPed != null && MyPed.Salud != null)
                MyPed.Salud.AlRecibirDano -= OnMyPedDamaged;
        }

        public void ConfigurarParametros(
            GestorPeatones gestor,
            NPCPedSpawner spawner,
            float distanciaLlegada,
            float reposoMinimo,
            float reposoMaximo,
            float radioReaccion,
            float duracionHuida,
            float radioSeparacion,
            float empujeSeparacion)
        {
            Gestor = gestor;
            Spawner = spawner;
            Navegacion = NavegacionPlaza.Instancia;
            DistanciaLlegada = Mathf.Max(0.1f, distanciaLlegada);
            ReposoMinimo = Mathf.Max(0f, reposoMinimo);
            ReposoMaximo = Mathf.Max(ReposoMinimo, reposoMaximo);
            RadioReaccionCombate = Mathf.Max(0.5f, radioReaccion);
            DuracionHuida = Mathf.Max(0.5f, duracionHuida);
            RadioSeparacion = Mathf.Max(0.1f, radioSeparacion);
            EmpujeSeparacion = Mathf.Max(0f, empujeSeparacion);
        }

        private void Update()
        {
            if (MyPed == null || MyPed.Controlador == null || !MyPed.Controlador.enabled)
                return;

            // La IA controla las entradas de su Ped antes de que Ped.Update evalúe los estados físicos.
            MyPed.ResetearEntrada();

            _timer2Segundos += Time.deltaTime;
            if (_timer2Segundos >= 2f)
            {
                _timer2Segundos = 0f;
                CurrentState?.UpdateState2Seconds();
                DetectarPeleaPorPercepcion();
            }

            CurrentState?.UpdateState();
        }

        private void DetectarPeleaPorPercepcion()
        {
            if (!CombateArena.EnCombate || CurrentState is EscapeState || CurrentState is ChaseState)
                return;

            Vector3 centro = CombateArena.CentroCombate;
            float distancia = DistanciaHorizontal(transform.position, centro);
            float umbralAudicion = Mathf.Max(RadioReaccionCombate, PedManager.Instancia.distanciaAudicionPelea);
            if (distancia <= umbralAudicion)
            {
                NotificarCombateCercano(centro, umbralAudicion);
                return;
            }

            if (Ped.JugadorPrincipal != null && PedVeACabezaDeOtro(MyPed, Ped.JugadorPrincipal))
                NotificarCombateCercano(centro, PedManager.Instancia.distanciaVisionIA);
        }

        private void OnMyPedDamaged(DamageInfo dmgInfo, float cantidad)
        {
            CurrentState?.OnMyPedDamaged(dmgInfo, cantidad);
        }

        public void NotificarCombateCercano(Vector3 centro, float radio)
        {
            if (radio > 0f)
                RadioReaccionCombate = radio;
            CurrentState?.OnCombateCercano(centro, RadioReaccionCombate);
        }

        public void NotificarCombateTerminado()
        {
            CurrentState?.OnCombateTerminado();
        }

        public void SwitchState<T>() where T : BaseState
        {
            _stateMachine.SwitchState(StateContainer.GetStateOrThrow<T>());
        }

        public void SwitchStateWithParameter<T>(object parameter) where T : BaseState
        {
            _stateMachine.SwitchStateWithParameter(StateContainer.GetStateOrThrow<T>(), parameter);
        }

        public void StartIdling(float pausa = -1f)
        {
            if (pausa >= 0f)
                SwitchStateWithParameter<IdleState>(pausa);
            else
                SwitchState<IdleState>();
        }

        public void StartWalkingAround() => SwitchState<WalkAroundState>();
        public void StartWalkingAround(Vector3 destino) => SwitchStateWithParameter<WalkAroundState>(destino);
        public void StartEscaping(Vector3 peligro, float radio = -1f)
        {
            if (radio > 0f)
                RadioReaccionCombate = radio;
            SwitchStateWithParameter<EscapeState>(peligro);
        }
        public void StartChasing(Ped objetivo) => SwitchStateWithParameter<ChaseState>(objetivo);
        public void StartChasing() => SwitchState<ChaseState>();
        public void StartFollowing(Ped lider) => SwitchStateWithParameter<FollowState>(lider);
        public void Recruit(Ped reclutador) => CurrentState?.OnRecruit(reclutador);

        public bool BuscarDestinoPaseo(out Vector3 destino)
        {
            if (Spawner != null)
                return Spawner.BuscarDestino(transform.position, out destino);

            if (Gestor != null)
                return Gestor.BuscarDestino(transform.position, Gestor.DistanciaMinimaPaseo, Gestor.DistanciaMaximaPaseo, out destino);

            destino = transform.position;
            return false;
        }

        public bool BuscarPuntoHuida(Vector3 peligro, out Vector3 destino)
        {
            if (Spawner != null)
                return Spawner.BuscarPuntoHuida(peligro, transform.position, RadioReaccionCombate, out destino);

            if (Gestor != null)
                return Gestor.BuscarPuntoHuida(peligro, transform.position, out destino);

            destino = transform.position;
            return false;
        }

        public void SolicitarReubicacion()
        {
            if (Gestor != null && TryGetComponent(out Peaton peaton))
                Gestor.Reubicar(peaton);
            else if (Spawner != null && Spawner.BuscarPuntoAparicion(out Vector3 punto))
                MyPed.Reubicar(punto, punto + transform.forward, false);
        }

        public Vector3 CalcularSeparacion()
        {
            Vector3 empuje = Vector3.zero;
            IReadOnlyList<Ped> peds = Ped.Todos;
            for (int i = 0; i < peds.Count; i++)
            {
                Ped otro = peds[i];
                if (otro == null || otro == MyPed || !otro.gameObject.activeInHierarchy)
                    continue;

                float radio = otro.Rol == PedestrianRole.Jugador ? 1.2f : RadioSeparacion;
                empuje += EmpujeDesde(otro.transform.position, radio);
            }

            if (Gestor != null && Gestor.Rival != null && Gestor.Rival.gameObject.activeInHierarchy)
                empuje += EmpujeDesde(Gestor.Rival.position, 1.2f);

            return Vector3.ClampMagnitude(empuje, 1.5f);
        }

        public Vector3 EmpujeDesde(Vector3 posicion, float radio)
        {
            Vector3 dif = transform.position - posicion;
            dif.y = 0f;
            float d = dif.magnitude;
            if (d < 0.001f || d >= radio)
                return Vector3.zero;
            return (dif / d) * (1f - d / radio);
        }

        public static bool PedVeACabezaDeOtro(Ped observador, Ped objetivo)
        {
            if (observador == null || objetivo == null || observador == objetivo)
                return false;

            Vector3 origen = observador.Modelo != null && observador.Modelo.Head != null
                ? observador.Modelo.Head.position
                : observador.transform.position + Vector3.up * 1.55f;

            Vector3 destino = objetivo.Modelo != null && objetivo.Modelo.Head != null
                ? objetivo.Modelo.Head.position
                : objetivo.transform.position + Vector3.up * 1.55f;

            Vector3 diff = destino - origen;
            float dist = diff.magnitude;
            if (dist < 0.05f || dist > PedManager.Instancia.distanciaVisionIA)
                return false;

            if (Vector3.Angle(diff / dist, observador.transform.forward) > PedManager.Instancia.anguloVisionIA)
                return false;

            // Capa 2 corresponde a personajes/objetos que no bloquean línea de vista de entorno.
            int mascaraEntorno = ~(1 << 2);
            return !Physics.Linecast(origen, destino, mascaraEntorno, QueryTriggerInteraction.Ignore);
        }

        public static float DistanciaHorizontal(Vector3 a, Vector3 b)
        {
            Vector3 d = a - b;
            d.y = 0f;
            return d.magnitude;
        }

        private void OnDrawGizmosSelected()
        {
            CurrentState?.OnDrawGizmosSelected();
        }
    }
}
