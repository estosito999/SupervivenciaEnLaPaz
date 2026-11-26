using System;
using System.Collections.Generic;
using System.Reflection;
using SupervivenciaEnLaPaz.AI;
using SupervivenciaEnLaPaz.AI.States;
using SupervivenciaEnLaPaz.Core;
using SupervivenciaEnLaPaz.Movimiento.States;
using SupervivenciaEnLaPaz.Utilidades;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SupervivenciaEnLaPaz.Editor
{
    /// <summary>
    /// Valida en modo lote o desde el menú de Unity la integridad de prefabs,
    /// escena, animadores y ausencias de Missing Scripts tras la integración.
    /// </summary>
    public static class ValidadorIntegracion
    {
        private const string RutaEscena = "Assets/PlazaBicentenario/Scenes/PlazaBicentenario.unity";

        [MenuItem("Supervivencia/Validar integración GTA")]
        public static void ValidarTodo()
        {
            // 1. Actualizar AnimatorController del jugador con clips FBX reales.
            ConstructorAnimatorJugador.ActualizarControladorJugador();

            // 2. Verificar prefabs de PlazaBicentenario sin scripts faltantes.
            string[] rutasPrefabs =
            {
                "Assets/PlazaBicentenario/Prefabs/EncuentroMonoblock.prefab",
                "Assets/PlazaBicentenario/Prefabs/AdversarioMision.prefab",
                "Assets/PlazaBicentenario/Prefabs/Peaton.prefab",
                "Assets/PlazaBicentenario/Prefabs/BarandaCafe.prefab",
                "Assets/PlazaBicentenario/Prefabs/Mochila.prefab"
            };

            int totalMissing = 0;
            foreach (string ruta in rutasPrefabs)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
                if (prefab == null)
                    throw new InvalidOperationException($"Prefab requerido no encontrado: {ruta}");

                int missing = ContarMissingScriptsEnJerarquia(prefab);
                if (missing > 0)
                {
                    Debug.LogError($"Missing Scripts ({missing}) detectados en prefab: {ruta}");
                    totalMissing += missing;
                }
            }

            // 3. Abrir PlazaBicentenario.unity en modo lectura/verificación (sin sobrescribir).
            Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
            foreach (GameObject root in escena.GetRootGameObjects())
                totalMissing += ContarMissingScriptsEnJerarquia(root);

            var vista = Object.FindAnyObjectByType<VistaPlazaBicentenario>(FindObjectsInactive.Include);
            var gestorPeatones = Object.FindAnyObjectByType<GestorPeatones>(FindObjectsInactive.Include);
            var navegacion = Object.FindAnyObjectByType<NavegacionPlaza>(FindObjectsInactive.Include);
            var combate = Object.FindAnyObjectByType<CombateArena>(FindObjectsInactive.Include);
            var misiones = Object.FindAnyObjectByType<GestorMisiones>(FindObjectsInactive.Include);
            var recorrido = Object.FindAnyObjectByType<RecorridoPlaza>(FindObjectsInactive.Include);
            var rival = Object.FindAnyObjectByType<RivalPlaza>(FindObjectsInactive.Include);

            if (vista == null || gestorPeatones == null || navegacion == null ||
                combate == null || misiones == null || recorrido == null || rival == null)
            {
                throw new InvalidOperationException(
                    $"Faltan componentes clave en {RutaEscena}: " +
                    $"Vista={vista != null}, Peatones={gestorPeatones != null}, Nav={navegacion != null}, " +
                    $"Combate={combate != null}, Misiones={misiones != null}, Recorrido={recorrido != null}, Rival={rival != null}");
            }

            if (totalMissing > 0)
                throw new InvalidOperationException($"Se encontraron {totalMissing} referencias Missing Script.");

            // 4. Comprobar creación de mallas NavMesh sobre la escena actual.
            bool navOk = navegacion.AsegurarMalla();
            if (!navOk)
                throw new InvalidOperationException("NavegacionPlaza.AsegurarMalla() devolvió false en la escena.");

            // 5. Verificar parámetros del Animator del jugador y del peatón.
            Animator animJugador = recorrido.GetComponent<Animator>();
            if (animJugador == null || animJugador.runtimeAnimatorController == null)
                throw new InvalidOperationException("El jugador no tiene Animator o RuntimeAnimatorController asignado.");

            GameObject prefabPeaton = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PlazaBicentenario/Prefabs/Peaton.prefab");
            Animator animPeaton = prefabPeaton.GetComponentInChildren<Animator>(true);
            if (animPeaton == null || animPeaton.runtimeAnimatorController == null)
                throw new InvalidOperationException("El prefab Peaton no tiene Animator o RuntimeAnimatorController asignado.");

            Debug.Log($"VALIDACION_OK: Escena='{escena.name}', MissingScripts={totalMissing}, " +
                      $"NavMesh={navOk}, AnimJugador='{animJugador.runtimeAnimatorController.name}', " +
                      $"AnimPeaton='{animPeaton.runtimeAnimatorController.name}'.");
        }

        [MenuItem("Supervivencia/Ejecutar Smoke Tests Integración")]
        public static void EjecutarSmokeTests()
        {
            GameObject prefabEncuentro = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PlazaBicentenario/Prefabs/EncuentroMonoblock.prefab");
            GameObject prefabPeaton = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PlazaBicentenario/Prefabs/Peaton.prefab");
            if (prefabEncuentro == null || prefabPeaton == null)
                throw new InvalidOperationException("Prefabs críticos EncuentroMonoblock o Peaton no encontrados.");

            GameObject instEncuentro = Object.Instantiate(prefabEncuentro);
            GameObject instPeaton = Object.Instantiate(prefabPeaton);
            try
            {
                if (instEncuentro.GetComponentInChildren<RecorridoPlaza>(true) == null)
                    throw new InvalidOperationException("EncuentroMonoblock instanciado sin RecorridoPlaza.");
                if (instEncuentro.GetComponentInChildren<CombateArena>(true) == null)
                    throw new InvalidOperationException("EncuentroMonoblock instanciado sin CombateArena.");
                if (instEncuentro.GetComponentInChildren<GestorMisiones>(true) == null)
                    throw new InvalidOperationException("EncuentroMonoblock instanciado sin GestorMisiones.");
                if (instPeaton.GetComponentInChildren<Peaton>(true) == null)
                    throw new InvalidOperationException("Peaton instanciado sin componente Peaton.");
            }
            finally
            {
                Object.DestroyImmediate(instPeaton);
                Object.DestroyImmediate(instEncuentro);
            }

            var goJugador = new GameObject("TestJugador");
            try
            {
                goJugador.AddComponent<CharacterController>();
                var ped = goJugador.AddComponent<Ped>();
                InvocarCiclo(ped, "Awake");
                ped.Rol = PedestrianRole.Jugador;

                var damageable = ped.Salud;
                if (damageable == null || !damageable.EstaVivo)
                    throw new InvalidOperationException("Damageable no inicializado en Ped.");

                var estadosEsperados = new Type[]
                {
                    typeof(StandState), typeof(WalkState), typeof(RunState), typeof(SprintState),
                    typeof(JumpState), typeof(CrouchState), typeof(CrouchMoveState),
                    typeof(RollState), typeof(DodgeState), typeof(HitReactionState), typeof(PanicState)
                };
                foreach (Type t in estadosEsperados)
                {
                    if (ped.ObtenerEstado(t) == null)
                        throw new InvalidOperationException($"Estado faltante en Ped: {t.Name}");
                }

                ped.CambiarEstado<WalkState>();
                if (!(ped.EstadoActualMaquina is WalkState))
                    throw new InvalidOperationException("Ped no transicionó a WalkState.");
                ped.CambiarEstado<RunState>();
                if (!(ped.EstadoActualMaquina is RunState))
                    throw new InvalidOperationException("Ped no transicionó a RunState.");
                ped.CambiarEstado<SprintState>();
                if (!(ped.EstadoActualMaquina is SprintState))
                    throw new InvalidOperationException("Ped no transicionó a SprintState.");
                ped.CambiarEstado<CrouchState>();
                if (!(ped.EstadoActualMaquina is CrouchState))
                    throw new InvalidOperationException("Ped no transicionó a CrouchState.");
                ped.CambiarEstado<CrouchMoveState>();
                if (!(ped.EstadoActualMaquina is CrouchMoveState))
                    throw new InvalidOperationException("Ped no transicionó a CrouchMoveState.");
                ped.CambiarEstado<PanicState>();
                if (!(ped.EstadoActualMaquina is PanicState))
                    throw new InvalidOperationException("Ped no transicionó a PanicState.");

                ped.EstablecerInvulnerabilidadTemporal(1.0f);
                float bloqueado = ped.RecibirImpacto(new DamageInfo(25f, goJugador));
                if (bloqueado != 0f)
                    throw new InvalidOperationException("El daño debió bloquearse durante invulnerabilidad.");

                ped.EstablecerInvulnerabilidadTemporal(0f);
                float aplicado = ped.RecibirImpacto(new DamageInfo(20f, goJugador));
                if (!Mathf.Approximately(aplicado, 20f))
                    throw new InvalidOperationException($"Se esperaba daño aplicado=20 y fue {aplicado}.");
                if (!(ped.EstadoActualMaquina is HitReactionState))
                    throw new InvalidOperationException("Recibir impacto debió activar HitReactionState.");

                var goNpc = new GameObject("TestNPC");
                try
                {
                    goNpc.AddComponent<CharacterController>();
                    var pedNpc = goNpc.AddComponent<Ped>();
                    InvocarCiclo(pedNpc, "Awake");
                    var ai = goNpc.AddComponent<PedAI>();
                    InvocarCiclo(ai, "Awake");

                    ai.StartIdling(1f);
                    if (!(ai.CurrentState is IdleState))
                        throw new InvalidOperationException("PedAI no entró a IdleState.");
                    ai.StartWalkingAround(Vector3.forward * 5f);
                    if (!(ai.CurrentState is WalkAroundState))
                        throw new InvalidOperationException("PedAI no entró a WalkAroundState.");
                    ai.StartEscaping(Vector3.zero, 12f);
                    if (!(ai.CurrentState is EscapeState))
                        throw new InvalidOperationException("PedAI no entró a EscapeState.");
                    ai.StartChasing(ped);
                    if (!(ai.CurrentState is ChaseState))
                        throw new InvalidOperationException("PedAI no entró a ChaseState.");
                    ai.StartFollowing(ped);
                    if (!(ai.CurrentState is FollowState))
                        throw new InvalidOperationException("PedAI no entró a FollowState.");
                }
                finally
                {
                    Object.DestroyImmediate(goNpc);
                }

                var goMundo = new GameObject("TestEstadoMundo");
                try
                {
                    var mundo = goMundo.AddComponent<EstadoMundoPlaza>();
                    InvocarCiclo(mundo, "Awake");
                    int dineroBase = mundo.DineroBs;
                    mundo.SumarDinero(50);
                    if (mundo.DineroBs != dineroBase + 50)
                        throw new InvalidOperationException("EstadoMundoPlaza.SumarDinero falló.");
                    mundo.RegistrarDisturbio(2);
                    if (mundo.NivelAlerta < 2)
                        throw new InvalidOperationException("EstadoMundoPlaza.RegistrarDisturbio falló.");
                    mundo.LimpiarAlerta();
                    if (mundo.NivelAlerta != 0)
                        throw new InvalidOperationException("EstadoMundoPlaza.LimpiarAlerta falló.");
                }
                finally
                {
                    Object.DestroyImmediate(goMundo);
                }
            }
            finally
            {
                Object.DestroyImmediate(goJugador);
            }

            Debug.Log("SMOKE_TESTS_OK: Prefabs, Ped(11 estados), Damageable, PedAI(5 estados) y EstadoMundoPlaza verificados.");
        }

        private static void InvocarCiclo(Component componente, string nombreMetodo)
        {
            MethodInfo metodo = componente.GetType().GetMethod(
                nombreMetodo,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            metodo?.Invoke(componente, null);
        }

        private static int ContarMissingScriptsEnJerarquia(GameObject raiz)
        {
            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(raiz);
            foreach (Transform child in raiz.transform)
                count += ContarMissingScriptsEnJerarquia(child.gameObject);
            return count;
        }
    }
}

