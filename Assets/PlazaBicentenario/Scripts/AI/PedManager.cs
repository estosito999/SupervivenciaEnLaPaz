// Adaptado e integrado para SupervivenciaEnLaPaz a partir de PedManager.cs de SanAndreasUnity.
using UnityEngine;

namespace SupervivenciaEnLaPaz
{
    /// <summary>
    /// Configuración centralizada de locomoción, resistencia, cámara, combate e IA
    /// para jugador y peatones en la Plaza del Bicentenario.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PedManager : MonoBehaviour
    {
        private static PedManager instancia;

        public static PedManager Instancia
        {
            get
            {
                if (instancia != null)
                    return instancia;

                instancia = FindAnyObjectByType<PedManager>();
                if (instancia == null)
                {
                    var go = new GameObject("PedManager");
                    instancia = go.AddComponent<PedManager>();
                }
                return instancia;
            }
        }

        [Header("Locomoción jugador")]
        [Min(0.1f)] public float velocidadCaminar = 1.8f;
        [Min(0.1f)] public float velocidadCorrer = 3.2f;
        [Min(0.1f)] public float velocidadSprint = 5.8f;
        [Min(0.1f)] public float velocidadAgachado = 1.35f;
        [Min(0.1f)] public float velocidadPanico = 3.1f;
        [Min(1f)] public float velocidadGiro = 720f;
        [Min(0.1f)] public float aceleracion = 12f;
        [Min(0.1f)] public float frenado = 18f;
        [Range(0.1f, 1f)] public float controlAereo = 0.35f;
        [Min(0.1f)] public float alturaSalto = 1.0f;

        [Header("Maniobras GTA")]
        [Min(0.5f)] public float velocidadRodar = 4.2f;
        [Min(0.1f)] public float duracionRodar = 0.44f;
        [Min(0.5f)] public float velocidadEsquiva = 4.8f;
        [Min(0.1f)] public float duracionEsquiva = 0.28f;
        [Min(0.05f)] public float duracionReaccionImpacto = 0.24f;

        [Header("Resistencia (Stamina)")]
        [Min(10f)] public float resistenciaMaxima = 100f;
        [Min(0f)] public float consumoSprintPorSegundo = 18f;
        [Min(0f)] public float consumoRodar = 18f;
        [Min(0f)] public float consumoEsquiva = 14f;
        [Min(0f)] public float recuperacionResistencia = 15f;
        [Min(0f)] public float umbralMinimoSprint = 12f;

        [Header("Transiciones de máquina de estados")]
        [Min(0f)] public float tiempoMinimoSalidaEstado = 0.08f;
        [Min(0f)] public float tiempoMinimoReentradaEstado = 0.08f;
        [Min(0f)] public float tiempoReentradaSalto = 0.25f;

        [Header("Cámara")]
        [Min(0.5f)] public float distanciaCamara = 4.4f;
        [Min(0.2f)] public float distanciaMinimaCamara = 2.0f;
        [Min(0.5f)] public float distanciaMaximaCamara = 7.0f;
        public LayerMask capasIgnoradasCamara = 1 << 2;

        [Header("IA de peatones")]
        [Min(0.1f)] public float distanciaParadaIA = 1.35f;
        [Min(1f)] public float distanciaAudicionPelea = 12f;
        [Range(10f, 180f)] public float anguloVisionIA = 85f;
        [Min(1f)] public float distanciaVisionIA = 22f;

        private void Awake()
        {
            if (instancia != null && instancia != this)
            {
                Destroy(this);
                return;
            }
            instancia = this;
        }

        private void OnDestroy()
        {
            if (instancia == this)
                instancia = null;
        }
    }
}

