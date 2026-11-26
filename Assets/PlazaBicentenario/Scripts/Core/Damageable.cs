// Adaptado e integrado para SupervivenciaEnLaPaz a partir de SanAndreasUnity.
using System;
using UnityEngine;

namespace SupervivenciaEnLaPaz.Core
{
    /// <summary>
    /// Componente local de salud y recepción de daño para jugador y NPCs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Damageable : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float saludMaxima = 100f;
        [SerializeField, Min(0f)] private float saludActual = 100f;

        public float SaludMaxima
        {
            get => saludMaxima;
            set
            {
                saludMaxima = Mathf.Max(1f, value);
                saludActual = Mathf.Clamp(saludActual, 0f, saludMaxima);
            }
        }

        public float SaludActual
        {
            get => saludActual;
            set => saludActual = Mathf.Clamp(value, 0f, saludMaxima);
        }

        public bool EstaVivo => saludActual > 0f;
        public bool Invulnerable { get; set; }
        public DamageInfo UltimoDano { get; private set; }
        public double TiempoUltimoDano { get; private set; } = double.NegativeInfinity;

        public event Action<DamageInfo, float> AlRecibirDano;
        public event Action<DamageInfo> AlMorir;

        private void Awake()
        {
            saludMaxima = Mathf.Max(1f, saludMaxima);
            saludActual = Mathf.Clamp(saludActual, 0f, saludMaxima);
        }

        public void RestaurarSalud(float nuevaMaxima = -1f)
        {
            if (nuevaMaxima > 0f)
                saludMaxima = nuevaMaxima;
            saludActual = saludMaxima;
        }

        public void EstablecerSaludDirecta(float valor, float maxima = -1f)
        {
            if (maxima > 0f)
                saludMaxima = maxima;
            saludActual = Mathf.Clamp(valor, 0f, saludMaxima);
        }

        public float RecibirDano(DamageInfo info)
        {
            if (info == null || info.Cantidad <= 0f || !EstaVivo)
                return 0f;

            if (Invulnerable && !info.IgnorarEsquiva)
                return 0f;

            float previa = saludActual;
            saludActual = Mathf.Max(0f, saludActual - info.Cantidad);
            float aplicado = previa - saludActual;

            UltimoDano = info;
            TiempoUltimoDano = Time.timeAsDouble;

            if (aplicado > 0f)
            {
                AlRecibirDano?.Invoke(info, aplicado);
                if (saludActual <= 0f)
                    AlMorir?.Invoke(info);
            }

            return aplicado;
        }
    }
}
