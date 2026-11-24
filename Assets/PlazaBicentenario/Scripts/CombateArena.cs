using System;
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
        private RivalPlaza rival;
        private Fase fase;
        private float proximoAtaqueRival;
        private float finDelCartel;

        private void Awake()
        {
            jugador = jugador1.Raiz.GetComponent<RecorridoPlaza>();
            rival = jugador2.Raiz.GetComponent<RivalPlaza>();
            jugador1.Preparar(false);
            jugador2.Preparar(true);
            cartelResultado.SetActive(false);
            guia.SetActive(true);
        }

        private void Update()
        {
            jugador1.RestaurarPose();
            jugador2.RestaurarPose();
            Keyboard teclado = Keyboard.current;
            float ahora = Time.time;
            switch (fase)
            {
                case Fase.Exploracion:
                    if (jugador1.ADistancia(jugador2, radioEncuentro) && jugador1.Ve(jugador2))
                    {
                        fase = Fase.Pelea;
                        guia.SetActive(false);
                        jugador1.MostrarPanel(true);
                        jugador2.MostrarPanel(true);
                        jugador.FijarRival(jugador2.Raiz);
                        rival.Perseguir(jugador1.Raiz, alcance * 0.77f);
                        proximoAtaqueRival = ahora + 1.0f;
                    }
                    return;
                case Fase.Victoria:
                    if (cartelResultado.activeSelf && ahora >= finDelCartel)
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

            if (teclado?.fKey.wasPressedThisFrame == true || Mouse.current?.leftButton.wasPressedThisFrame == true)
                jugador1.IniciarGolpe(ahora);
            if (ahora >= proximoAtaqueRival && jugador2.ADistancia(jugador1, alcance) && jugador2.Ve(jugador1))
            {
                jugador2.IniciarGolpe(ahora);
                proximoAtaqueRival = ahora + 1.15f;
            }

            // Resolver ambos impactos antes del resultado permite un empate en el mismo fotograma.
            bool golpe1 = jugador1.GolpeAlcanza(jugador2, ahora, alcance);
            bool golpe2 = jugador2.GolpeAlcanza(jugador1, ahora, alcance);
            if (golpe1) jugador2.RecibirDano(dano);
            if (golpe2) jugador1.RecibirDano(dano);
            if (jugador1.Vida == 0 || jugador2.Vida == 0) TerminarCombate();
        }

        private void LateUpdate()
        {
            if (fase != Fase.Pelea) return;
            jugador1.PosarGolpe(jugador2, Time.time);
            jugador2.PosarGolpe(jugador1, Time.time);
        }

        private void OnDisable()
        {
            jugador1.RestaurarPose();
            jugador2.RestaurarPose();
        }

        private void TerminarCombate()
        {
            rival.enabled = false;
            jugador2.DetenerAnimacion();
            cartelResultado.SetActive(true);
            if (jugador1.Vida > 0)
            {
                fase = Fase.Victoria;
                jugador.FijarRival(null);
                rival.gameObject.SetActive(false);
                jugador1.MostrarPanel(false);
                jugador2.MostrarPanel(false);
                textoResultado.text = "GANASTE";
                detalleResultado.text = "Puedes seguir recorriendo la plaza.";
                textoGuia.text = "Recorrido libre por el Monoblock";
                finDelCartel = Time.time + 2.2f;
            }
            else
            {
                fase = Fase.Derrota;
                jugador.enabled = false;
                jugador1.DetenerAnimacion();
                textoResultado.text = jugador2.Vida == 0 ? "EMPATE" : "PERDISTE";
                detalleResultado.text = "Pulsa R para volver a intentarlo.";
            }
        }

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
            private float inicioGolpe = float.NegativeInfinity;

            internal int Vida { get; private set; }
            internal Transform Raiz => animador.transform;

            internal void Preparar(bool desdeDerecha)
            {
                barraDerecha = desdeDerecha;
                Vida = VidaInicial;
                ActualizarBarra();
                MostrarPanel(false);
                if (!animador.isHuman) return;
                brazo = animador.GetBoneTransform(HumanBodyBones.RightUpperArm);
                antebrazo = animador.GetBoneTransform(HumanBodyBones.RightLowerArm);
                mano = animador.GetBoneTransform(HumanBodyBones.RightHand);
            }

            internal void MostrarPanel(bool visible) => panel.SetActive(visible);
            internal void DetenerAnimacion() => animador.SetFloat("Speed", 0f);

            internal bool ADistancia(Combatiente otro, float distancia)
            {
                return (otro.Raiz.position - Raiz.position).sqrMagnitude <= distancia * distancia;
            }

            internal bool Ve(Combatiente otro)
            {
                Vector3 altura = Vector3.up * 1.05f;
                return !Physics.Linecast(Raiz.position + altura, otro.Raiz.position + altura,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            }

            internal void IniciarGolpe(float ahora)
            {
                if (ahora - inicioGolpe < PausaEntreGolpes) return;
                inicioGolpe = ahora;
                impactoPendiente = true;
            }

            internal bool GolpeAlcanza(Combatiente objetivo, float ahora, float distanciaMaxima)
            {
                if (!impactoPendiente || ahora - inicioGolpe < MomentoImpacto) return false;
                impactoPendiente = false;
                if (!ADistancia(objetivo, distanciaMaxima)) return false;
                Vector3 direccion = Vector3.ProjectOnPlane(objetivo.Raiz.position - Raiz.position, Vector3.up);
                return Vector3.Dot(Raiz.forward, direccion.normalized) > 0.35f && Ve(objetivo);
            }

            internal void RecibirDano(int cantidad)
            {
                Vida = Mathf.Max(0, Vida - cantidad);
                ActualizarBarra();
            }

            private void ActualizarBarra()
            {
                float porcentaje = (float)Vida / VidaInicial;
                rellenoVida.anchorMin = new Vector2(barraDerecha ? 1f - porcentaje : 0f, 0f);
                rellenoVida.anchorMax = new Vector2(barraDerecha ? 1f : porcentaje, 1f);
                contadorVida.text = Vida.ToString();
            }

            internal void PosarGolpe(Combatiente objetivo, float ahora)
            {
                float tiempo = ahora - inicioGolpe;
                if (tiempo >= DuracionGolpe || brazo == null || antebrazo == null || mano == null) return;
                float progreso = tiempo < MomentoImpacto
                    ? tiempo / MomentoImpacto
                    : (DuracionGolpe - tiempo) / (DuracionGolpe - MomentoImpacto);
                float peso = Mathf.SmoothStep(0f, 1f, progreso);
                Vector3 direccion = Vector3.ProjectOnPlane(objetivo.Raiz.position - Raiz.position, Vector3.up).normalized;
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
