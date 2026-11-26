using System.Collections.Generic;
using SupervivenciaEnLaPaz.AI;
using SupervivenciaEnLaPaz.AI.States;
using SupervivenciaEnLaPaz.Utilidades;
using UnityEngine;

namespace SupervivenciaEnLaPaz
{
    /// <summary>
    /// Peatón civil autónomo de la Plaza del Bicentenario.
    /// Fachada unificada sobre <see cref="Ped"/> y <see cref="PedAI"/> para evitar
    /// controladores paralelos: <see cref="PedAI"/> decide los estados y <see cref="Ped"/>
    /// ejecuta locomoción, giro, física y animación.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(CharacterController), typeof(Animator))]
    public sealed class Peaton : MonoBehaviour
    {
        [Header("Movimiento")]
        [SerializeField, Min(0.1f)] private float velocidadMinima = 1.45f;
        [SerializeField, Min(0.1f)] private float velocidadMaxima = 2.1f;
        [SerializeField, Min(0.1f)] private float velocidadHuida = 2.9f;
        [SerializeField, Min(1f)] private float velocidadGiro = 420f;
        [SerializeField, Min(0.1f)] private float radioSeparacion = 1.15f;
        [SerializeField, Min(0f)] private float empujeSeparacion = 1.5f;
        [SerializeField, Min(0.1f)] private float referenciaAnimacion = 3.2f;

        [Header("Rutina")]
        [SerializeField, Min(0.1f)] private float distanciaLlegada = 0.55f;
        [SerializeField, Min(0f)] private float reposoMinimo = 1.2f;
        [SerializeField, Min(0f)] private float reposoMaximo = 5.5f;
        [SerializeField, Min(0.1f)] private float radioReaccion = 11f;
        [SerializeField, Min(0.1f)] private float duracionHuida = 4.5f;

        private static readonly List<Peaton> activos = new();
        public static IReadOnlyList<Peaton> Activos => activos;

        private Ped ped;
        private PedAI ai;
        private SkinnedMeshRenderer malla;
        private GestorPeatones gestor;

        public bool Huyendo => ai != null && ai.CurrentState is EscapeState;
        public Ped Ped => ped;
        public PedAI IA => ai;

        private void Awake()
        {
            malla = GetComponentInChildren<SkinnedMeshRenderer>(true);
            ped = gameObject.GetOrAddComponent<Ped>();
            ped.Rol = PedestrianRole.Civil;
            ai = gameObject.GetOrAddComponent<PedAI>();

            AplicarConfiguracion();
        }

        private void OnEnable() => activos.AddIfNotPresent(this);
        private void OnDisable() => activos.Remove(this);

        public void Configurar(GestorPeatones nuevoGestor)
        {
            gestor = nuevoGestor;
            AplicarConfiguracion();
        }

        private void AplicarConfiguracion()
        {
            float velocidadPaseo = Random.Range(velocidadMinima, Mathf.Max(velocidadMinima, velocidadMaxima));
            if (ped != null)
            {
                // Se usa referenciaAnimacion como velocidadCorrer para conservar exactamente el escalado Speed del Animator.
                ped.ConfigurarVelocidadesNPC(velocidadPaseo, referenciaAnimacion, velocidadHuida, velocidadGiro);
            }

            if (ai != null)
            {
                ai.ConfigurarParametros(
                    gestor,
                    gestor != null ? gestor.Spawner : null,
                    distanciaLlegada,
                    reposoMinimo,
                    reposoMaximo,
                    radioReaccion,
                    duracionHuida,
                    radioSeparacion,
                    empujeSeparacion);
            }
        }

        /// <summary>Cambia la ropa, la piel y el cabello sin compartir materiales entre peatones.</summary>
        public void ConfigurarAspecto(Material chaqueta, Material piel, Material cabello)
        {
            if (malla == null)
                return;
            Material[] actuales = malla.sharedMaterials;
            if (actuales.Length < 4)
                return;
            actuales[1] = chaqueta;
            actuales[2] = piel;
            actuales[3] = cabello;
            malla.sharedMaterials = actuales;
        }

        public void ReaccionarAlCombate(Vector3 centro, float radio)
        {
            if (ai != null)
                ai.NotificarCombateCercano(centro, radio);
        }

        public void Calmarse()
        {
            if (ai != null)
                ai.NotificarCombateTerminado();
        }

        public void Reubicar(Vector3 punto)
        {
            if (ped == null)
                return;

            ped.Reubicar(punto, punto + transform.forward, false);
            if (ai != null)
            {
                if (CombateArena.EnCombate)
                    ai.SwitchStateWithParameter<IdleState>(CombateArena.CentroCombate);
                else
                    ai.StartIdling(Random.Range(0.6f, 1.8f));
            }
        }
    }
}
