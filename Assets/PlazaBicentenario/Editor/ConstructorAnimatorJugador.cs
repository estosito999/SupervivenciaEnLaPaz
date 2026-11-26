using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SupervivenciaEnLaPaz.Editor
{
    /// <summary>
    /// Actualiza in-place el AnimatorController del jugador (preparando_pelea_1.controller)
    /// conservando su GUID y utilizando exclusivamente los clips FBX reales disponibles.
    /// </summary>
    public static class ConstructorAnimatorJugador
    {
        private const string RutaControlador = "Assets/PlazaBicentenario/Animaciones/preparando_pelea_1.controller";
        private const string RutaIdle = "Assets/PlazaBicentenario/Animaciones/Idle.fbx";
        private const string RutaSadIdle = "Assets/PlazaBicentenario/Animaciones/Sad Idle.fbx";
        private const string RutaCaminar = "Assets/PlazaBicentenario/Animaciones/Walking2.fbx";
        private const string RutaAgachado = "Assets/PlazaBicentenario/Animaciones/Crouched Walking.fbx";
        private const string RutaGuardia = "Assets/PlazaBicentenario/Animaciones/Standing Idle To Fight Idle.fbx";

        [MenuItem("Supervivencia/Actualizar Animator del Jugador")]
        public static void ActualizarControladorJugador()
        {
            AnimatorController controlador = AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador);
            if (controlador == null)
                throw new System.InvalidOperationException($"No se encontró {RutaControlador}");

            AnimationClip clipIdle = CargarClip(RutaIdle);
            AnimationClip clipSadIdle = CargarClip(RutaSadIdle);
            AnimationClip clipCaminar = CargarClip(RutaCaminar);
            AnimationClip clipAgachado = CargarClip(RutaAgachado);
            AnimationClip clipGuardia = CargarClip(RutaGuardia);

            // Limpiar parámetros y estados previos conservando el archivo .controller y su GUID.
            while (controlador.parameters.Length > 0)
                controlador.RemoveParameter(0);

            AnimatorStateMachine maquina = controlador.layers[0].stateMachine;
            foreach (ChildAnimatorState child in maquina.states)
                maquina.RemoveState(child.state);
            foreach (AnimatorTransition t in maquina.entryTransitions)
                maquina.RemoveEntryTransition(t);
            foreach (AnimatorStateTransition t in maquina.anyStateTransitions)
                maquina.RemoveAnyStateTransition(t);

            // Eliminar sub-assets BlendTree huérfanos previos si existieran.
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(RutaControlador);
            foreach (Object obj in subAssets)
            {
                if (obj is BlendTree)
                    Object.DestroyImmediate(obj, true);
            }

            controlador.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controlador.AddParameter("EnCombate", AnimatorControllerParameterType.Bool);
            controlador.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
            controlador.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);
            controlador.AddParameter("Crouching", AnimatorControllerParameterType.Bool);
            controlador.AddParameter("Panic", AnimatorControllerParameterType.Bool);
            controlador.AddParameter("RollDir", AnimatorControllerParameterType.Float);
            controlador.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            controlador.AddParameter("Roll", AnimatorControllerParameterType.Trigger);
            controlador.AddParameter("Dodge", AnimatorControllerParameterType.Trigger);
            controlador.AddParameter("Hit", AnimatorControllerParameterType.Trigger);

            // Configurar valores por defecto de parámetros booleanos.
            AnimatorControllerParameter[] pars = controlador.parameters;
            for (int i = 0; i < pars.Length; i++)
            {
                if (pars[i].name == "Grounded")
                    pars[i].defaultBool = true;
            }
            controlador.parameters = pars;

            // BlendTree principal de locomoción (Reposo -> Caminar -> Correr -> Sprint).
            BlendTree arbolLocomocion = new BlendTree
            {
                name = "LocomocionBlendTree",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(arbolLocomocion, controlador);
            arbolLocomocion.AddChild(clipIdle, 0f);
            arbolLocomocion.AddChild(clipCaminar, 0.55f);
            arbolLocomocion.AddChild(clipCaminar, 1.0f);
            arbolLocomocion.AddChild(clipCaminar, 1.55f);

            ChildMotion[] hijosLoc = arbolLocomocion.children;
            hijosLoc[0].timeScale = 1.0f;
            hijosLoc[1].timeScale = 0.85f;
            hijosLoc[2].timeScale = 1.25f;
            hijosLoc[3].timeScale = 1.72f;
            arbolLocomocion.children = hijosLoc;

            AnimatorState estadoLocomocion = maquina.AddState("Locomocion", new Vector3(300f, 120f));
            estadoLocomocion.motion = arbolLocomocion;

            // Estado de reposo en guardia para combate cuerpo a cuerpo.
            AnimatorState estadoGuardia = maquina.AddState("Guardia", new Vector3(300f, 260f));
            estadoGuardia.motion = clipGuardia;

            // BlendTree agachado (Crouched Walking real: postura quieta y marcha agachada).
            BlendTree arbolAgachado = new BlendTree
            {
                name = "AgachadoBlendTree",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(arbolAgachado, controlador);
            arbolAgachado.AddChild(clipAgachado, 0f);
            arbolAgachado.AddChild(clipAgachado, 0.45f);
            ChildMotion[] hijosAgachado = arbolAgachado.children;
            hijosAgachado[0].timeScale = 0.02f;
            hijosAgachado[1].timeScale = 1.0f;
            arbolAgachado.children = hijosAgachado;

            AnimatorState estadoAgachado = maquina.AddState("Agachado", new Vector3(600f, 120f));
            estadoAgachado.motion = arbolAgachado;

            // Estado aéreo / caída reutilizando Idle controlado mientras no exista clip dedicado de salto.
            AnimatorState estadoAire = maquina.AddState("Aereo", new Vector3(300f, -20f));
            estadoAire.motion = clipIdle;

            maquina.defaultState = estadoLocomocion;

            // Locomocion -> Guardia
            AnimatorStateTransition locAGuardia = estadoLocomocion.AddTransition(estadoGuardia);
            locAGuardia.hasExitTime = false;
            locAGuardia.duration = 0.12f;
            locAGuardia.AddCondition(AnimatorConditionMode.If, 0f, "EnCombate");
            locAGuardia.AddCondition(AnimatorConditionMode.Less, 0.12f, "Speed");

            // Guardia -> Locomocion (si se mueve o sale de combate)
            AnimatorStateTransition guardiaALocMove = estadoGuardia.AddTransition(estadoLocomocion);
            guardiaALocMove.hasExitTime = false;
            guardiaALocMove.duration = 0.1f;
            guardiaALocMove.AddCondition(AnimatorConditionMode.Greater, 0.12f, "Speed");

            AnimatorStateTransition guardiaALocFin = estadoGuardia.AddTransition(estadoLocomocion);
            guardiaALocFin.hasExitTime = false;
            guardiaALocFin.duration = 0.15f;
            guardiaALocFin.AddCondition(AnimatorConditionMode.IfNot, 0f, "EnCombate");

            // Locomocion <-> Agachado
            AnimatorStateTransition locAAgachado = estadoLocomocion.AddTransition(estadoAgachado);
            locAAgachado.hasExitTime = false;
            locAAgachado.duration = 0.14f;
            locAAgachado.AddCondition(AnimatorConditionMode.If, 0f, "Crouching");

            AnimatorStateTransition agachadoALoc = estadoAgachado.AddTransition(estadoLocomocion);
            agachadoALoc.hasExitTime = false;
            agachadoALoc.duration = 0.14f;
            agachadoALoc.AddCondition(AnimatorConditionMode.IfNot, 0f, "Crouching");

            // Locomocion <-> Aereo
            AnimatorStateTransition locAAire = estadoLocomocion.AddTransition(estadoAire);
            locAAire.hasExitTime = false;
            locAAire.duration = 0.1f;
            locAAire.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");

            AnimatorStateTransition aireALoc = estadoAire.AddTransition(estadoLocomocion);
            aireALoc.hasExitTime = false;
            aireALoc.duration = 0.12f;
            aireALoc.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");

            EditorUtility.SetDirty(controlador);
            AssetDatabase.SaveAssets();
            Debug.Log("AnimatorController del jugador actualizado correctamente con clips reales.");
        }

        private static AnimationClip CargarClip(string ruta)
        {
            foreach (Object recurso in AssetDatabase.LoadAllAssetsAtPath(ruta))
            {
                if (recurso is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    return clip;
            }
            throw new System.InvalidOperationException($"No se encontró AnimationClip en {ruta}.");
        }
    }
}
