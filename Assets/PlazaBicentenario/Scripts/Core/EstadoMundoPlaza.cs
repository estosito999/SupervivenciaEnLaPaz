using System;
using UnityEngine;

namespace SupervivenciaEnLaPaz.Core
{
    /// <summary>
    /// Estado global de la sesión en la Plaza del Bicentenario: reloj del altiplano,
    /// economía real del jugador en bolivianos y nivel de alerta pública.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EstadoMundoPlaza : MonoBehaviour
    {
        public static EstadoMundoPlaza Instancia { get; private set; }

        [Header("Reloj de La Paz")]
        [SerializeField, Range(0, 23)] private int horaInicial = 16;
        [SerializeField, Range(0, 59)] private int minutoInicial = 30;
        [Tooltip("Minutos de juego que transcurren por cada segundo real.")]
        [SerializeField, Min(0f)] private float minutosPorSegundoReal = 0.5f;

        [Header("Economía (Bolivianos)")]
        [SerializeField, Min(0)] private int dineroInicialBs = 50;

        [Header("Alerta pública")]
        [SerializeField, Min(1f)] private float duracionPorNivelAlerta = 14f;

        private float minutosAcumulados;
        private float tiempoRestanteAlerta;

        public int DineroBs { get; private set; }
        public int NivelAlerta { get; private set; }
        public float FraccionEnfriamientoAlerta => NivelAlerta > 0 && duracionPorNivelAlerta > 0f
            ? Mathf.Clamp01(tiempoRestanteAlerta / duracionPorNivelAlerta)
            : 0f;

        public int HoraActual
        {
            get
            {
                int totalMinutos = Mathf.FloorToInt(minutosAcumulados) % (24 * 60);
                return totalMinutos / 60;
            }
        }

        public int MinutoActual
        {
            get
            {
                int totalMinutos = Mathf.FloorToInt(minutosAcumulados) % (24 * 60);
                return totalMinutos % 60;
            }
        }

        public string HoraFormateada => $"{HoraActual:00}:{MinutoActual:00}";

        public event Action<int> AlCambiarDinero;
        public event Action<int> AlCambiarAlerta;

        public static EstadoMundoPlaza ObtenerOCrear()
        {
            if (Instancia != null)
                return Instancia;

            Instancia = FindAnyObjectByType<EstadoMundoPlaza>();
            if (Instancia == null)
            {
                var go = new GameObject("Estado Mundo Plaza");
                Instancia = go.AddComponent<EstadoMundoPlaza>();
            }
            return Instancia;
        }

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(this);
                return;
            }

            Instancia = this;
            minutosAcumulados = horaInicial * 60f + minutoInicial;
            DineroBs = dineroInicialBs;
            NivelAlerta = 0;
            tiempoRestanteAlerta = 0f;
        }

        private void OnDestroy()
        {
            if (Instancia == this)
                Instancia = null;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;

            minutosAcumulados += minutosPorSegundoReal * dt;
            if (minutosAcumulados >= 24f * 60f)
                minutosAcumulados -= 24f * 60f;

            if (NivelAlerta > 0)
            {
                // Mientras haya pelea activa, la alerta se sostiene lentamente antes de enfriarse.
                if (CombateArena.EnCombate)
                    tiempoRestanteAlerta = Mathf.Max(tiempoRestanteAlerta, duracionPorNivelAlerta * 0.45f);
                else
                    tiempoRestanteAlerta -= dt;

                if (tiempoRestanteAlerta <= 0f)
                    EstablecerAlerta(NivelAlerta - 1);
            }
        }

        public void SumarDinero(int cantidadBs)
        {
            if (cantidadBs == 0)
                return;

            int nuevo = Mathf.Max(0, DineroBs + cantidadBs);
            if (nuevo == DineroBs)
                return;

            DineroBs = nuevo;
            AlCambiarDinero?.Invoke(DineroBs);
        }

        public void RegistrarDisturbio(int nivelMinimo, float duracionExtra = 0f)
        {
            int objetivo = Mathf.Clamp(nivelMinimo, 0, 3);
            if (objetivo <= 0)
                return;

            if (objetivo > NivelAlerta)
                EstablecerAlerta(objetivo);

            tiempoRestanteAlerta = Mathf.Max(tiempoRestanteAlerta, duracionPorNivelAlerta + duracionExtra);
        }

        public void ReducirAlerta()
        {
            if (NivelAlerta <= 0)
                return;
            EstablecerAlerta(NivelAlerta - 1);
        }

        public void LimpiarAlerta()
        {
            EstablecerAlerta(0);
        }

        private void EstablecerAlerta(int nuevoNivel)
        {
            int clamped = Mathf.Clamp(nuevoNivel, 0, 3);
            if (clamped == NivelAlerta)
            {
                if (clamped > 0)
                    tiempoRestanteAlerta = duracionPorNivelAlerta;
                return;
            }

            NivelAlerta = clamped;
            tiempoRestanteAlerta = NivelAlerta > 0 ? duracionPorNivelAlerta : 0f;
            AlCambiarAlerta?.Invoke(NivelAlerta);
        }
    }
}

