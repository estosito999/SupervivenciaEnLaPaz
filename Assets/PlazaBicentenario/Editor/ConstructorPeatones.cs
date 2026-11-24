using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace SupervivenciaEnLaPaz.Editor
{
    /// <summary>
    /// Genera los recursos de los peatones (controlador de animación y prefab) y
    /// coloca el gestor de población en la plaza. Se puede ejecutar desde el menú
    /// o en modo batch con -executeMethod.
    /// </summary>
    public static class ConstructorPeatones
    {
        private const string RutaModelo = "Assets/PlazaBicentenario/Personajes/HumanCharacterDummy_M.fbx";
        private const string RutaMalla = "Assets/PlazaBicentenario/Personajes/Rival_Animado.asset";
        private const string RutaChaqueta = "Assets/PlazaBicentenario/Personajes/Rival_Chaqueta.mat";
        private const string RutaPiel = "Assets/PlazaBicentenario/Personajes/Rival_Piel.mat";
        private const string RutaCabello = "Assets/PlazaBicentenario/Personajes/Rival_Cabello.mat";
        private const string RutaRostro = "Assets/PlazaBicentenario/Personajes/Luis.mat";
        private const string RutaReposo = "Assets/PlazaBicentenario/Personajes/Animaciones/Idle.fbx";
        private const string RutaCaminar = "Assets/PlazaBicentenario/Personajes/Animaciones/Walking2.fbx";
        private const string RutaControlador = "Assets/PlazaBicentenario/Personajes/Animaciones/Peaton.controller";
        private const string RutaPrefab = "Assets/PlazaBicentenario/Prefabs/Peaton.prefab";
        private const string RutaEscena = "Assets/PlazaBicentenario/Scenes/PlazaBicentenario.unity";

        [InitializeOnLoadMethod]
        private static void AutoInicializar()
        {
            EditorApplication.playModeStateChanged += (PlayModeStateChange cambio) =>
            {
                if (cambio == PlayModeStateChange.EnteredEditMode)
                {
                    EditorApplication.delayCall += VerificarYConfigurar;
                }
            };
            EditorApplication.delayCall += VerificarYConfigurar;
        }

        public static void VerificarYConfigurar()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) == null ||
                AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador) == null ||
                Object.FindFirstObjectByType<GestorPeatones>(FindObjectsInactive.Include) == null)
            {
                Configurar();
            }
        }

        [MenuItem("Supervivencia/Instalar peatones en la plaza")]
        public static void Configurar()
        {
            AnimatorController controlador = CrearControlador();
            GameObject prefab = CrearPrefab(controlador);
            ConfigurarEscena(prefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Peatones de la plaza configurados correctamente.");
        }

        /// <summary>Punto de entrada para Unity en modo batch.</summary>
        public static void ConfigurarPorLote() => Configurar();

        private static AnimatorController CrearControlador()
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(RutaControlador) != null)
                AssetDatabase.DeleteAsset(RutaControlador);

            AnimationClip reposo = CargarClip(RutaReposo);
            AnimationClip caminar = CargarClip(RutaCaminar);

            AnimatorController controlador = AnimatorController.CreateAnimatorControllerAtPath(RutaControlador);
            controlador.AddParameter("Speed", AnimatorControllerParameterType.Float);

            AnimatorStateMachine maquina = controlador.layers[0].stateMachine;
            AnimatorState estadoReposo = maquina.AddState("Reposo", new Vector3(260f, 80f));
            estadoReposo.motion = reposo;

            AnimatorState estadoCaminar = maquina.AddState("Caminar", new Vector3(520f, 80f));
            estadoCaminar.motion = caminar;
            estadoCaminar.speedParameterActive = true;
            estadoCaminar.speedParameter = "Speed";

            maquina.defaultState = estadoReposo;

            AnimatorStateTransition haciaCaminar = estadoReposo.AddTransition(estadoCaminar);
            haciaCaminar.hasExitTime = false;
            haciaCaminar.duration = 0.12f;
            haciaCaminar.AddCondition(AnimatorConditionMode.Greater, 0.05f, "Speed");

            AnimatorStateTransition haciaReposo = estadoCaminar.AddTransition(estadoReposo);
            haciaReposo.hasExitTime = false;
            haciaReposo.duration = 0.18f;
            haciaReposo.AddCondition(AnimatorConditionMode.Less, 0.05f, "Speed");

            EditorUtility.SetDirty(controlador);
            return controlador;
        }

        private static AnimationClip CargarClip(string ruta)
        {
            foreach (Object recurso in AssetDatabase.LoadAllAssetsAtPath(ruta))
                if (recurso is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    return clip;

            throw new System.InvalidOperationException($"No se encontró una animación en {ruta}.");
        }

        private static GameObject CrearPrefab(AnimatorController controlador)
        {
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(RutaModelo);
            Mesh malla = AssetDatabase.LoadAssetAtPath<Mesh>(RutaMalla);
            Material rostro = AssetDatabase.LoadAssetAtPath<Material>(RutaRostro);
            Material chaqueta = AssetDatabase.LoadAssetAtPath<Material>(RutaChaqueta);
            Material piel = AssetDatabase.LoadAssetAtPath<Material>(RutaPiel);
            Material cabello = AssetDatabase.LoadAssetAtPath<Material>(RutaCabello);

            if (modelo == null || malla == null || rostro == null || chaqueta == null || piel == null || cabello == null)
                throw new System.InvalidOperationException("Faltan recursos para construir el prefab del peatón.");

            // El prefab se arma en una escena temporal para no ensuciar la plaza.
            Scene escenaAnterior = SceneManager.GetActiveScene();
            Scene escenaTemporal = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(escenaTemporal);
            GameObject instancia = null;
            try
            {
                instancia = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
                instancia.name = "Peaton";
                instancia.layer = 2;
                foreach (Transform parte in instancia.GetComponentsInChildren<Transform>(true))
                    parte.gameObject.layer = 2;

                SkinnedMeshRenderer renderizador = instancia.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (renderizador == null)
                    throw new System.InvalidOperationException("El modelo del peatón no tiene SkinnedMeshRenderer.");

                renderizador.sharedMesh = malla;
                renderizador.sharedMaterials = new[] { rostro, chaqueta, piel, cabello };
                renderizador.enabled = true;

                Animator animador = instancia.GetComponentInChildren<Animator>(true);
                if (animador == null)
                    throw new System.InvalidOperationException("El modelo del peatón no tiene Animator.");

                animador.runtimeAnimatorController = controlador;
                animador.applyRootMotion = false;

                CharacterController controladorFisico = instancia.AddComponent<CharacterController>();
                controladorFisico.height = 1.7f;
                controladorFisico.radius = 0.22f;
                controladorFisico.center = new Vector3(0f, 0.85f, 0f);
                controladorFisico.stepOffset = 0.3f;
                controladorFisico.slopeLimit = 45f;
                controladorFisico.skinWidth = 0.03f;

                instancia.AddComponent<Peaton>();

                if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab) != null)
                    AssetDatabase.DeleteAsset(RutaPrefab);

                return PrefabUtility.SaveAsPrefabAsset(instancia, RutaPrefab);
            }
            finally
            {
                if (instancia != null)
                    Object.DestroyImmediate(instancia);
                if (escenaAnterior.IsValid())
                    SceneManager.SetActiveScene(escenaAnterior);
                EditorSceneManager.CloseScene(escenaTemporal, true);
            }
        }

        private static void ConfigurarEscena(GameObject prefab)
        {
            Scene escena = EditorSceneManager.GetActiveScene();
            if (escena.path != RutaEscena)
                escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);

            GestorPeatones gestor = Object.FindFirstObjectByType<GestorPeatones>(FindObjectsInactive.Include);
            GameObject raiz;
            if (gestor == null)
            {
                raiz = new GameObject("Peatones de la plaza");
                gestor = raiz.AddComponent<GestorPeatones>();
            }
            else
            {
                raiz = gestor.gameObject;
            }

            if (Object.FindFirstObjectByType<NavegacionPlaza>(FindObjectsInactive.Include) == null)
                raiz.AddComponent<NavegacionPlaza>();

            Peaton prefabPeaton = prefab.GetComponent<Peaton>();
            var serializado = new SerializedObject(gestor);
            serializado.FindProperty("prefabPeaton").objectReferenceValue = prefabPeaton;
            serializado.FindProperty("cantidad").intValue = 14;
            serializado.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(gestor);

            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);

            if (!EditorBuildSettings.scenes.Any(s => s.path == RutaEscena))
            {
                var escenas = EditorBuildSettings.scenes.ToList();
                escenas.Insert(0, new EditorBuildSettingsScene(RutaEscena, true));
                EditorBuildSettings.scenes = escenas.ToArray();
            }
        }
    }
}
