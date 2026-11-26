using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SupervivenciaEnLaPaz
{
    [DefaultExecutionOrder(100)]
    public sealed class CombateArena : MonoBehaviour
    {
        private const int VidaInicial = 100;
        private const float MomentoImpacto = 0.13f;
        private const float DuracionGolpe = 0.34f;
        private const float PausaEntreGolpes = 0.52f;
        private enum Fase { Exploracion, Pelea, Victoria, Derrota }

        /// <summary>Se dispara al empezar la pelea con el punto medio de los combatientes.</summary>
        public static event Action<Vector3> CombateIniciado;
        public static event Action CombateTerminado;
        public static bool EnCombate { get; private set; }
        public static Vector3 CentroCombate { get; private set; }
        internal event Action LadronGolpeado;
        internal Transform Jugador => jugador1.Raiz;
        internal Transform RivalInicial => jugador2.Raiz;
        internal Transform ObjetivoActual => enfocado?.Raiz;
        internal int RivalesEnPie => contrarios.Count;
        internal bool VictoriaDelJugador => fase == Fase.Victoria;

        // Acceso de solo lectura para HUD externo; conserva el sistema original de dano.
        public int VidaJugador => jugador1 != null ? jugador1.Vida : 0;
        public int VidaMaximaJugador => VidaInicial;

        [SerializeField] private Combatiente jugador1;
        [SerializeField] private Combatiente jugador2;
        [SerializeField, Min(1)] private int dano = 20;
        [SerializeField, Min(0.1f)] private float alcance = 1.05f;
        [SerializeField, Min(1f)] private float radioEncuentro = 3.4f;
        [SerializeField] private GameObject guia;
        [SerializeField] private Text textoGuia;
        [SerializeField] private GameObject cartelResultado;
        [SerializeField] private Text textoResultado;
        [SerializeField] private Text detalleResultado;

        private RecorridoPlaza jugador;
        private readonly List<Combatiente> contrarios = new();
        private Combatiente enfocado;
        private Transform ladron;
        private int turno;
        private bool enCampana;
        private bool dueloHabilitado = true;
        private Fase fase;
        private float proximoAtaqueRival;
        private float cambiarTurnoEn;
        private float finDelCartel;

        private void Awake()
        {
            EnCombate = false;
            jugador = jugador1.Raiz.GetComponent<RecorridoPlaza>();
            jugador1.Preparar(false);
            jugador2.Preparar(true);
            cartelResultado.SetActive(false);
            guia.SetActive(true);
        }

        internal void PrepararCampana()
        {
            enCampana = true;
            dueloHabilitado = false;
        }

        internal void HabilitarDuelo() => dueloHabilitado = true;
        internal void MarcarLadron(Transform actor)
        {
            ladron = actor;
            jugador1.CancelarGolpe();
        }

        internal void IniciarEmboscada(IReadOnlyList<RivalPlaza> rivales)
        {
            if (rivales.Count == 0 || EnCombate || fase == Fase.Derrota) return;
            contrarios.Clear();
            foreach (var actor in rivales)
                contrarios.Add(new Combatiente(actor.GetComponent<Animator>()));
            IniciarCombate();
        }

        private void IniciarCombate()
        {
            fase = Fase.Pelea;
            dueloHabilitado = false;
            jugador1.Preparar(false);
            foreach (var contrario in contrarios) contrario.Preparar(true);
            cartelResultado.SetActive(false);
            guia.SetActive(enCampana);
            jugador1.MostrarPanel(true);
            jugador2.MostrarPanel(true);
            turno = 0;
            cambiarTurnoEn = float.PositiveInfinity;
            AsignarPosiciones();
            ActualizarEnfoque();
            proximoAtaqueRival = Time.time + 1f;
            EnCombate = true;
            CentroCombate = (Jugador.position + enfocado.Raiz.position) * 0.5f;
            CombateIniciado?.Invoke(CentroCombate);
        }

        private void AsignarPosiciones()
        {
            for (int i = 0; i < contrarios.Count; i++)
            {
                bool ataca = i == turno;
                float angulo = i * 2f * Mathf.PI / contrarios.Count;
                Vector3 flanco = ataca ? Vector3.zero : new Vector3(Mathf.Cos(angulo), 0, Mathf.Sin(angulo)) * 1.65f;
                contrarios[i].Motor.Perseguir(Jugador, ataca ? alcance * 0.74f : 0.22f, flanco);
            }
        }

        private void ActualizarEnfoque()
        {
            Combatiente elegido = null;
            float mejor = float.PositiveInfinity;
            foreach (var contrario in contrarios)
            {
                float coste = (contrario.Raiz.position - Jugador.position).sqrMagnitude;
                if (contrarios.Count > 1 && !jugador1.Ve(contrario.Raiz)) coste += 10000f;
                if (coste >= mejor) continue;
                mejor = coste;
                elegido = contrario;
            }
            if (elegido != enfocado)
            {
                enfocado = elegido;
                jugador.FijarRival(enfocado.Raiz);
            }
            jugador2.DibujarVida(enfocado.Vida);
        }

        private void Update()
        {
            jugador1.RestaurarPose();
            foreach (var contrario in contrarios) contrario.RestaurarPose();
            Keyboard teclado = Keyboard.current;
            float ahora = Time.time;
            switch (fase)
            {
                case Fase.Exploracion:
                    if (dueloHabilitado && jugador1.ADistancia(RivalInicial, radioEncuentro) && jugador1.Ve(RivalInicial))
                    {
                        contrarios.Add(jugador2);
                        IniciarCombate();
                    }
                    return;
                case Fase.Victoria:
                    if (ladron != null)
                    {
                        LeerGolpe(ahora);
                        if (jugador1.GolpeAlcanza(ladron, ahora, alcance)) LadronGolpeado?.Invoke();
                    }
                    if (!enCampana && cartelResultado.activeSelf && ahora >= finDelCartel)
                    {
                        cartelResultado.SetActive(false);
                        guia.SetActive(true);
                    }
                    return;
                case Fase.Derrota:
                    if (teclado?.rKey.wasPressedThisFrame == true)
                        SceneManager.LoadScene(gameObject.scene.buildIndex);
                    return;
            }

            ActualizarEnfoque();
            LeerGolpe(ahora);
            if (ahora >= cambiarTurnoEn)
            {
                turno = (turno + 1) % contrarios.Count;
                cambiarTurnoEn = float.PositiveInfinity;
                AsignarPosiciones();
            }
            var atacante = contrarios[turno];
            if (ahora >= proximoAtaqueRival && atacante.ADistancia(Jugador, alcance) && atacante.Ve(Jugador))
            {
                atacante.IniciarGolpe(ahora);
                proximoAtaqueRival = ahora + 1.15f;
                if (contrarios.Count > 1) cambiarTurnoEn = ahora + DuracionGolpe;
            }

            if (jugador1.GolpeAlcanza(enfocado.Raiz, ahora, alcance)) enfocado.RecibirDano(dano);
            foreach (var contrario in contrarios)
                if (contrario.GolpeAlcanza(Jugador, ahora, alcance)) jugador1.RecibirDano(dano);
            jugador2.DibujarVida(enfocado.Vida);

            if (enfocado.Vida == 0)
            {
                enfocado.Raiz.gameObject.SetActive(false);
                contrarios.Remove(enfocado);
                enfocado = null;
                if (contrarios.Count > 0)
                {
                    turno = 0;
                    cambiarTurnoEn = float.PositiveInfinity;
                    AsignarPosiciones();
                    ActualizarEnfoque();
                }
            }
            if (jugador1.Vida == 0 || contrarios.Count == 0) TerminarCombate();
        }

        private void LeerGolpe(float ahora)
        {
            if (Keyboard.current?.fKey.wasPressedThisFrame == true || Mouse.current?.leftButton.wasPressedThisFrame == true)
                jugador1.IniciarGolpe(ahora);
        }

        private void LateUpdate()
        {
            if (fase == Fase.Pelea)
            {
                jugador1.PosarGolpe(enfocado.Raiz, Time.time);
                foreach (var contrario in contrarios) contrario.PosarGolpe(Jugador, Time.time);
            }
            else if (ladron != null) jugador1.PosarGolpe(ladron, Time.time);
        }

        private void OnDisable()
        {
            jugador1.RestaurarPose();
            foreach (var contrario in contrarios) contrario.RestaurarPose();
        }

        private void OnDestroy()
        {
            EnCombate = false;
        }

        private void TerminarCombate()
        {
            foreach (var contrario in contrarios)
            {
                contrario.Motor.enabled = false;
                contrario.DetenerAnimacion();
            }
            EnCombate = false;
            jugador.FijarRival(null);
            jugador1.CancelarGolpe();
            if (jugador1.Vida > 0)
            {
                fase = Fase.Victoria;
                jugador1.MostrarPanel(false);
                jugador2.MostrarPanel(false);
                if (!enCampana)
                {
                    MostrarResultado("GANASTE", "Puedes seguir recorriendo la plaza.");
                    textoGuia.text = "Recorrido libre por el Monoblock";
                    finDelCartel = Time.time + 2.2f;
                }
            }
            else
            {
                fase = Fase.Derrota;
                jugador.enabled = false;
                jugador1.DetenerAnimacion();
                MostrarResultado(contrarios.Count == 0 ? "EMPATE" : "PERDISTE", "Pulsa R para volver a intentarlo.");
            }
            CombateTerminado?.Invoke();
        }

        internal void MostrarResultado(string titulo, string detalle)
        {
            textoResultado.text = titulo;
            detalleResultado.text = detalle;
            cartelResultado.SetActive(true);
        }
        internal void OcultarResultado() => cartelResultado.SetActive(false);

        [Serializable]
        private sealed class Combatiente
        {
            [SerializeField] private Animator animador;
            [SerializeField] private GameObject panel;
            [SerializeField] private RectTransform rellenoVida;
            [SerializeField] private Text contadorVida;

            private Transform brazo;
            private Transform antebrazo;
            private Transform mano;
            private Quaternion rotacionBrazo;
            private Quaternion rotacionAntebrazo;
            private bool poseAplicada;
            private bool impactoPendiente;
            private bool barraDerecha;
            private int vidaMostrada = -1;
            private float inicioGolpe = float.NegativeInfinity;

            internal int Vida { get; private set; }
            internal Transform Raiz => animador.transform;
            internal RivalPlaza Motor { get; private set; }

            public Combatiente() { }
            internal Combatiente(Animator actor) => animador = actor;

            internal void Preparar(bool desdeDerecha)
            {
                barraDerecha = desdeDerecha;
                Vida = VidaInicial;
                CancelarGolpe();
                Motor = animador.GetComponent<RivalPlaza>();
                DibujarVida(Vida);
                MostrarPanel(false);
                if (!animador.isHuman) return;
                brazo = animador.GetBoneTransform(HumanBodyBones.RightUpperArm);
                antebrazo = animador.GetBoneTransform(HumanBodyBones.RightLowerArm);
                mano = animador.GetBoneTransform(HumanBodyBones.RightHand);
            }

            internal void MostrarPanel(bool visible) { if (panel != null) panel.SetActive(visible); }
            internal void DetenerAnimacion() => animador.SetFloat("Speed", 0f);
            internal void CancelarGolpe()
            {
                RestaurarPose();
                inicioGolpe = float.NegativeInfinity;
                impactoPendiente = false;
            }

            internal bool ADistancia(Transform otro, float distancia)
            {
                return (otro.position - Raiz.position).sqrMagnitude <= distancia * distancia;
            }

            internal bool Ve(Transform otro)
            {
                Vector3 altura = Vector3.up * 1.05f;
                return !Physics.Linecast(Raiz.position + altura, otro.position + altura,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            }

            internal void IniciarGolpe(float ahora)
            {
                if (ahora - inicioGolpe < PausaEntreGolpes) return;
                inicioGolpe = ahora;
                impactoPendiente = true;
            }

            internal bool GolpeAlcanza(Transform objetivo, float ahora, float distanciaMaxima)
            {
                if (!impactoPendiente || ahora - inicioGolpe < MomentoImpacto) return false;
                impactoPendiente = false;
                if (!ADistancia(objetivo, distanciaMaxima)) return false;
                Vector3 direccion = Vector3.ProjectOnPlane(objetivo.position - Raiz.position, Vector3.up);
                if (Vector3.Dot(Raiz.forward, direccion.normalized) <= 0.35f || !Ve(objetivo)) return false;
                // Un tablero de mesa o un pasamanos también intercepta un golpe a corta distancia.
                Vector3 altura = Vector3.up * 0.74f;
                return !Physics.CheckCapsule(Raiz.position + altura, objetivo.position + altura,
                    0.08f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            }

            internal void RecibirDano(int cantidad)
            {
                Vida = Mathf.Max(0, Vida - cantidad);
                DibujarVida(Vida);
            }

            internal void DibujarVida(int vida)
            {
                if (rellenoVida == null || vidaMostrada == vida) return;
                vidaMostrada = vida;
                float porcentaje = (float)vida / VidaInicial;
                rellenoVida.anchorMin = new Vector2(barraDerecha ? 1f - porcentaje : 0f, 0f);
                rellenoVida.anchorMax = new Vector2(barraDerecha ? 1f : porcentaje, 1f);
                contadorVida.text = vida.ToString();
            }

            internal void PosarGolpe(Transform objetivo, float ahora)
            {
                float tiempo = ahora - inicioGolpe;
                if (tiempo >= DuracionGolpe || brazo == null || antebrazo == null || mano == null) return;
                float progreso = tiempo < MomentoImpacto
                    ? tiempo / MomentoImpacto
                    : (DuracionGolpe - tiempo) / (DuracionGolpe - MomentoImpacto);
                float peso = Mathf.SmoothStep(0f, 1f, progreso);
                Vector3 direccion = Vector3.ProjectOnPlane(objetivo.position - Raiz.position, Vector3.up).normalized;
                if (direccion == Vector3.zero) direccion = Raiz.forward;

                rotacionBrazo = brazo.localRotation;
                rotacionAntebrazo = antebrazo.localRotation;
                poseAplicada = true;
                Alinear(brazo, antebrazo.position - brazo.position, direccion + Vector3.down * 0.16f, peso);
                Alinear(antebrazo, mano.position - antebrazo.position, direccion, peso);
            }

            internal void RestaurarPose()
            {
                if (!poseAplicada) return;
                poseAplicada = false;
                if (brazo != null) brazo.localRotation = rotacionBrazo;
                if (antebrazo != null) antebrazo.localRotation = rotacionAntebrazo;
            }

            private static void Alinear(Transform hueso, Vector3 actual, Vector3 objetivo, float peso)
            {
                Quaternion giro = Quaternion.FromToRotation(actual, objetivo);
                hueso.rotation = Quaternion.Slerp(Quaternion.identity, giro, peso) * hueso.rotation;
            }
        }
    }
}
