using System;
using System.Collections.Generic;
using System.IO;
using SupervivenciaEnLaPaz;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SupervivenciaEnLaPaz.Editor
{
    internal sealed partial class ConstructorPlaza
    {
        private const string Raiz = "Assets/PlazaBicentenario";
        private const string RutaEscena = Raiz + "/Scenes/PlazaBicentenario.unity";
        private const string RutaMallas = Raiz + "/Geometria/PlazaBicentenario.asset";
        private const float MediaAnchura = 28f;
        private const float MediaLongitud = 13.7f;
        private const float TamanoCenital = 22.8f;

        private readonly Dictionary<string, Material> materiales = new();
        private bool mallasCreadas;

        private Material Piedra => materiales["Cantera clara"];
        private Material Oscura => materiales["Basalto"];
        private Material Metal => materiales["Acero pintado"];
        private Material Madera => materiales["Madera"];
        private Material Cristal => materiales["Cristal sombreado"];

        [MenuItem("Supervivencia/Abrir plaza del Bicentenario")]
        private static void AbrirPlaza()
        {
            if (EditorSceneManager.GetActiveScene().path == RutaEscena)
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaEscena) != null)
                EditorSceneManager.OpenScene(RutaEscena);
            else
                new ConstructorPlaza().Generar();
        }

        private void Generar()
        {
            foreach (string carpeta in new[] { "Scenes", "Geometria", "Materiales", "Texturas" })
                Directory.CreateDirectory($"{Raiz}/{carpeta}");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrepararMateriales();
            CrearPlaza();
            CrearMonoblock();
            CrearEntorno();
            CrearRecorrido();
            CrearIluminacion();
            CrearCamara();

            Physics.SyncTransforms();
            if (!Physics.Raycast(new Vector3(0, 5, 0), Vector3.down, out RaycastHit hit, 6f)
                || Math.Abs(hit.point.y) > 0.01f)
                throw new InvalidOperationException("El pavimento principal necesita un colisionador en y = 0.");

            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(escena, RutaEscena))
                throw new IOException($"No se pudo guardar {RutaEscena}");

            Selection.activeObject = null;
            SceneView.lastActiveSceneView?.LookAt(new Vector3(0, 5, 5), Quaternion.Euler(37, -18, 0), 58f);
            Debug.Log($"Plaza del Bicentenario guardada en {RutaEscena}. Materiales: {materiales.Count}.");
        }

        private void PrepararMateriales()
        {
            Texture2D empedrado = Textura("Mosaico de la plaza", 1024, 1024, PixelPiso);
            Texture2D cantera = Textura("Juntas de piedra", 512, 384, PixelMuro);
            Texture2D piedraFachada = AssetDatabase.LoadAssetAtPath<Texture2D>(Raiz + "/Panorama/Texturas/Canteria.jpg");
            Texture2D vidrio = AssetDatabase.LoadAssetAtPath<Texture2D>(Raiz + "/Panorama/Texturas/Vidrio.jpg");

            MaterialLit("Mosaico", "FFFFFF", 0.15f, empedrado);
            MaterialLit("Cantera clara", "D1C8B7", 0.18f, piedraFachada);
            MaterialLit("Cantera tostada", "B89F85", 0.23f, piedraFachada);
            MaterialLit("Basalto", "777C7A", 0.24f);
            MaterialLit("Asfalto", "686D72", 0.19f, cantera);
            MaterialLit("Terraza", "B5A894", 0.16f, cantera);
            MaterialLit("Arcilla principal", "B68B6C", 0.23f, piedraFachada);
            MaterialLit("Arcilla al sol", "C79C79", 0.23f, piedraFachada);
            MaterialLit("Arcilla en sombra", "99765E", 0.23f, piedraFachada);
            MaterialLit("Marco bronce", "8C7256", 0.35f);
            MaterialLit("Cristal sombreado", "A1ADAE", 0.47f, vidrio);
            MaterialLit("Cristal azul", "B1C2CA", 0.51f, vidrio);
            MaterialLit("Cristal cálido", "BBA78E", 0.41f, vidrio);
            MaterialLit("Acero pintado", "424D53", 0.38f);
            MaterialLit("Madera", "936849", 0.24f, cantera);
            MaterialLit("Roble claro", "BB8F61", 0.23f);
            MaterialLit("Toldo rojo", "A84047", 0.19f);
            MaterialLit("Toldo iluminado", "C85454", 0.20f);
            MaterialLit("Toldo oscuro", "87343E", 0.19f);
            MaterialLit("Lona verde", "517064", 0.17f);
            MaterialLit("Lona cruda", "E4D2AF", 0.17f);
            MaterialLit("Tierra", "695747", 0.14f);
            MaterialLit("Follaje jade", "526E58", 0.17f);
            MaterialLit("Follaje al sol", "80946B", 0.18f);
            MaterialLit("Follaje oscuro", "425A4C", 0.14f);
            MaterialLit("Floral", "A46580", 0.19f);
            MaterialLit("Ladrillo", "B17761", 0.21f, cantera);
            MaterialLit("Revoque claro", "D7CFC1", 0.19f, cantera);
            MaterialLit("Revoque verde", "95A29B", 0.19f, cantera);
            MaterialLit("Revoque azul", "8E9EAD", 0.19f, cantera);
            MaterialLit("Ladera urbana", "A88B78", 0.17f, cantera);
            MaterialLit("Chaqueta", "355B88", 0.31f);
            MaterialLit("Piel", "C18A63", 0.20f);
        }

        private void CrearPlaza()
        {
            Transform plaza = Grupo("Plaza del Bicentenario · recorte jugable");
            Transform piso = Grupo("Pavimento, bordillos y explanada", plaza);
            var malla = new Geometria();
            malla.Caja(new Vector3(0, -0.35f, 0), new Vector3(56, 0.70f, 27.4f), Piedra);
            Suelo(malla, -MediaAnchura, MediaAnchura, -MediaLongitud, MediaLongitud, 0.004f, 3.1f);
            malla.Caja(new Vector3(0, -0.24f, -17.8f), new Vector3(56, 0.46f, 8.2f), Piedra);
            Suelo(malla, -MediaAnchura, MediaAnchura, -21.9f, -MediaLongitud, -0.006f, 3.1f);
            for (int lado = -1; lado <= 1; lado += 2)
            {
                malla.Caja(new Vector3(lado * 27.78f, 0.065f, 0), new Vector3(0.44f, 0.13f, 27.4f), Piedra);
                malla.Caja(new Vector3(lado * 26.9f, 0.007f, 0), new Vector3(0.16f, 0.016f, 27.4f), Oscura);
                malla.Caja(new Vector3(lado * 0.63f, 0.005f, 0), new Vector3(0.19f, 0.015f, 27.4f), Oscura);
            }
            Guardar(malla, piso);
            Colision(piso, new Vector3(0, -0.35f, 0), new Vector3(56, 0.7f, 27.4f));
            Colision(piso, new Vector3(0, -0.24f, -17.8f), new Vector3(56, 0.46f, 8.2f));

            Terraza(plaza);
            Graderias(plaza);
            Cafe(plaza);
            Transform puestos = Grupo("Puestos de feria", plaza);
            Puesto(puestos, "Puesto de libros", new Vector3(15.0f, 0, -7.4f), -8f, 0);
            Puesto(puestos, "Puesto de artesanía", new Vector3(20.2f, 0, -7.8f), -4f, 1);
            Puesto(puestos, "Puesto de textiles", new Vector3(25.15f, 0, -8.2f), 3f, 2);
            Puesto(puestos, "Puesto lateral", new Vector3(24.1f, 0, -2.7f), -12f, 3);
            Puesto(puestos, "Puesto de la esquina", new Vector3(24.3f, 0, 2.24f), -15f, 4);

            Transform mobiliario = Grupo("Faroles, jardineras y bancos", plaza);
            Farol(mobiliario, new Vector3(10.6f, 0, -1.1f), 6.1f, 0f);
            Farol(mobiliario, new Vector3(-11.7f, 0, 4.7f), 5.5f, 0f);
            Farol(mobiliario, new Vector3(11.3f, 0, 10.3f), 5.15f, 0.25f);
            Farol(mobiliario, new Vector3(-23.1f, 0.55f, 7.4f), 4.9f, 0.4f);
            Jardinera(mobiliario, new Vector3(-25.2f, 0.55f, 5.5f), 3.4f, 1.55f, 100);
            Jardinera(mobiliario, new Vector3(-25.2f, 0.55f, 9.3f), 3.4f, 1.55f, 116);
            Jardinera(mobiliario, new Vector3(19.3f, 0, 9.6f), 6.1f, 1.7f, 84);
            Jardinera(mobiliario, new Vector3(25.15f, 0, 8.4f), 2.7f, 1.65f, 67);
            Jardinera(mobiliario, new Vector3(-21.3f, 0, -12.5f), 3.4f, 1.15f, 124);
            Banco(mobiliario, new Vector3(18.9f, 0, 7.8f), 2.8f, 180f);
            Banco(mobiliario, new Vector3(-18.7f, 0.55f, 2.6f), 2.8f, 90f);
            Banco(mobiliario, new Vector3(-18.7f, 0.55f, 7.5f), 2.8f, 90f);

            Transform placa = Grupo("Placa de la plaza", mobiliario, new Vector3(12.2f, 0, 4.1f));
            var piedra = new Geometria();
            piedra.Caja(new Vector3(0, 0.39f, 0), new Vector3(1.6f, 0.78f, 0.42f), Piedra);
            piedra.Caja(new Vector3(0, 0.50f, -0.22f), new Vector3(1.42f, 0.34f, 0.037f), Metal);
            Guardar(piedra, placa);
            Colision(placa, new Vector3(0, 0.39f, 0), new Vector3(1.6f, 0.78f, 0.42f));
            Rotulo(placa, "Placa", "PLAZA DEL\nBICENTENARIO", new Vector3(0, 0.51f, -0.244f), 0.16f, ColorHex("EEDFC6"));
        }

        private void Terraza(Transform padre)
        {
            Transform grupo = Grupo("Terraza lateral y pasamanos", padre);
            var malla = new Geometria();
            malla.Caja(new Vector3(-21.6f, 0.275f, 3.3f), new Vector3(12.8f, 0.55f, 17.7f), materiales["Terraza"]);
            Suelo(malla, -28, -15.2f, -5.55f, 12.15f, 0.554f, 3.1f);
            Colision(grupo, new Vector3(-21.6f, 0.275f, 3.3f), new Vector3(12.8f, 0.55f, 17.7f));
            for (int escalon = 0; escalon < 3; escalon++)
            {
                float altura = (escalon + 1) * 0.55f / 3f;
                float z = -6.45f + escalon * 0.36f;
                Vector3 centro = new(-21.6f, altura * 0.5f, z);
                Vector3 medidas = new(12.8f, altura, 0.36f);
                malla.Caja(centro, medidas, Piedra);
                Colision(grupo, centro, medidas);
            }
            Baranda(malla, new Vector3(-15.37f, 0.55f, -5.53f), new Vector3(-15.37f, 0.55f, 1.4f), 5);
            Baranda(malla, new Vector3(-15.37f, 0.55f, 4.75f), new Vector3(-15.37f, 0.55f, 11.95f), 5);
            Colision(grupo, new Vector3(-15.37f, 1.0f, -2.065f), new Vector3(0.12f, 0.9f, 6.93f));
            Colision(grupo, new Vector3(-15.37f, 1.0f, 8.35f), new Vector3(0.12f, 0.9f, 7.2f));

            for (int escalon = 0; escalon < 3; escalon++)
            {
                float altura = (escalon + 1) * 0.55f / 3f;
                Vector3 centro = new(-14.28f - 0.37f * escalon, altura / 2f, 3.07f);
                Vector3 medidas = new(0.37f, altura, 3.30f);
                malla.Caja(centro, medidas, Piedra);
                Colision(grupo, centro, medidas);
            }
            Guardar(malla, grupo);
        }

        private void Graderias(Transform padre)
        {
            Transform grupo = Grupo("Gradas hacia el atrio", padre, new Vector3(0, 0, 12.0f));
            var malla = new Geometria();
            for (int escalon = 0; escalon < 6; escalon++)
            {
                float altura = (escalon + 1) * 0.15f;
                float fondo = 0.54f;
                Vector3 centro = new(0, altura / 2f, (escalon + 0.5f) * fondo);
                Vector3 medidas = new(25.5f, altura, fondo);
                malla.Caja(centro, medidas, Piedra);
                malla.Caja(new Vector3(0, altura + 0.001f, escalon * fondo + 0.052f), new Vector3(25.5f, 0.009f, 0.068f), Oscura);
                Colision(grupo, centro, medidas);
            }
            Vector3 centroAtrio = new(0, 0.44f, 5.52f);
            Vector3 tamanoAtrio = new(65f, 0.92f, 4.60f);
            malla.Caja(centroAtrio, tamanoAtrio, Piedra);
            Colision(grupo, centroAtrio, tamanoAtrio);
            for (int lado = -1; lado <= 1; lado += 2)
            {
                malla.Caja(new Vector3(lado * 21.7f, 0.43f, 2), new Vector3(17.8f, 0.86f, 4.0f), materiales["Terraza"]);
                malla.Caja(new Vector3(lado * 21.7f, 0.94f, 0.12f), new Vector3(17.8f, 0.25f, 0.30f), Piedra);
                Colision(grupo, new Vector3(lado * 21.7f, 0.43f, 2), new Vector3(17.8f, 0.86f, 4.0f));
            }
            Guardar(malla, grupo);
        }

        private void CrearIluminacion()
        {
            Transform grupo = Grupo("Luz y cielo del Altiplano");
            Transform sol = Grupo("Sol de la tarde", grupo);
            Vector3 haciaSol = new Vector3(-0.65f, 0.58f, -0.60f).normalized;
            sol.rotation = Quaternion.LookRotation(-haciaSol);
            var luz = sol.gameObject.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.color = ColorHex("FFE2BF");
            luz.intensity = 1.38f;
            luz.shadows = LightShadows.Soft;
            luz.shadowStrength = 0.73f;
            luz.shadowBias = 0.10f;
            luz.shadowNormalBias = 0.24f;
            RenderSettings.sun = luz;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ColorHex("AABDD2");
            RenderSettings.ambientEquatorColor = ColorHex("959DA2");
            RenderSettings.ambientGroundColor = ColorHex("6D6257");
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0030f;
            RenderSettings.fogColor = ColorHex("B5CAD7");

            Shader shaderCielo = Shader.Find("SupervivenciaEnLaPaz/Cielo Altiplano");
            if (shaderCielo == null)
                throw new InvalidOperationException("No se encontró el shader del cielo.");
            var cielo = new Material(shaderCielo) { name = "Cielo del Altiplano" };
            cielo.SetColor("_Zenit", ColorHex("5691C7"));
            cielo.SetColor("_Horizonte", ColorHex("D3E1E7"));
            cielo.SetColor("_Luz", ColorHex("FFF5E5"));
            cielo.SetColor("_Sombra", ColorHex("A7BED1"));
            cielo.SetVector("_Sol", haciaSol);
            RenderSettings.skybox = GuardarActivo(cielo, Raiz + "/Materiales/Cielo del Altiplano.mat");

            var perfil = ScriptableObject.CreateInstance<VolumeProfile>();
            perfil.name = "Color de la plaza";
            string rutaPerfil = Raiz + "/Materiales/Color de la plaza.asset";
            AssetDatabase.CreateAsset(perfil, rutaPerfil);
            var color = perfil.Add<ColorAdjustments>();
            color.postExposure.Override(0.05f);
            color.contrast.Override(7f);
            color.saturation.Override(3f);
            AssetDatabase.AddObjectToAsset(color, perfil);
            var rango = perfil.Add<Tonemapping>();
            rango.mode.Override(TonemappingMode.Neutral);
            AssetDatabase.AddObjectToAsset(rango, perfil);
            EditorUtility.SetDirty(perfil);
            var volumen = grupo.gameObject.AddComponent<Volume>();
            volumen.isGlobal = true;
            volumen.priority = 10;
            volumen.sharedProfile = perfil;
        }

        private void CrearCamara()
        {
            Transform vista = Grupo("Vista cenital de la plaza", null, new Vector3(0, 62, -1.3f));
            vista.rotation = Quaternion.Euler(90, 0, 0);

            Transform frontal = Grupo("Cámara principal · vista del Monoblock", null, new Vector3(0.8f, 3.25f, -35.5f));
            frontal.LookAt(new Vector3(0.55f, 16.5f, 23));
            frontal.gameObject.tag = "MainCamera";
            var camera = frontal.gameObject.AddComponent<Camera>();
            camera.fieldOfView = 62f;
            camera.nearClipPlane = 0.08f;
            camera.farClipPlane = 1250f;
            var datos = camera.GetUniversalAdditionalCameraData();
            datos.renderPostProcessing = true;
            datos.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            datos.antialiasingQuality = AntialiasingQuality.High;
            frontal.gameObject.AddComponent<AudioListener>();

            var control = frontal.gameObject.AddComponent<VistaPlazaBicentenario>();
            var ajustes = new SerializedObject(control);
            ajustes.FindProperty("vistaCenital").objectReferenceValue = vista;
            ajustes.FindProperty("tamanoCenital").floatValue = TamanoCenital;
            ajustes.ApplyModifiedPropertiesWithoutUndo();
        }

        private void CrearRecorrido()
        {
            Transform limites = Grupo("Límites del recorrido");
            for (int lado = -1; lado <= 1; lado += 2)
                Colision(limites, new Vector3(lado * 28.12f, 1.15f, -1.3f), new Vector3(0.24f, 2.9f, 41.3f));
            Colision(limites, new Vector3(0, 1.15f, -21.86f), new Vector3(56.48f, 2.9f, 0.20f));
            Colision(limites, new Vector3(0, 1.15f, 19.15f), new Vector3(56.48f, 2.9f, 0.20f));

            var encuentro = AssetDatabase.LoadAssetAtPath<GameObject>(Raiz + "/Prefabs/EncuentroMonoblock.prefab");
            PrefabUtility.InstantiatePrefab(encuentro);
        }

        [MenuItem("Supervivencia/Capturar vistas de la plaza")]
        private static void Capturar()
        {
            Camera camara = Camera.main;
            GameObject cenital = GameObject.Find("Vista cenital de la plaza");
            if (camara == null || cenital == null)
                return;

            string carpeta = Path.GetFullPath("Temp/PlazaBicentenarioBuild");
            Directory.CreateDirectory(carpeta);
            var pose = new Pose(camara.transform.position, camara.transform.rotation);
            bool ortografica = camara.orthographic;
            float tamano = camara.orthographicSize;
            try
            {
                GuardarCaptura(camara, Path.Combine(carpeta, "Plaza_Bicentenario_Frontis.png"));
                camara.transform.SetPositionAndRotation(cenital.transform.position, cenital.transform.rotation);
                camara.orthographicSize = TamanoCenital;
                camara.orthographic = true;
                GuardarCaptura(camara, Path.Combine(carpeta, "Plaza_Bicentenario_Cenital.png"));
                Debug.Log($"Vistas guardadas en {carpeta}.");
            }
            finally
            {
                camara.transform.SetPositionAndRotation(pose.position, pose.rotation);
                camara.orthographicSize = tamano;
                camara.orthographic = ortografica;
            }
        }

        [MenuItem("Supervivencia/Capturar vistas de la plaza", true)]
        private static bool PuedeCapturar()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode && EditorSceneManager.GetActiveScene().path == RutaEscena;
        }

        private static void GuardarCaptura(Camera camara, string destino)
        {
            var render = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            var foto = new Texture2D(render.width, render.height, TextureFormat.RGB24, false);
            RenderTexture anterior = RenderTexture.active;
            try
            {
                var solicitud = new RenderPipeline.StandardRequest { destination = render };
                if (!RenderPipeline.SupportsRenderRequest(camara, solicitud))
                    throw new InvalidOperationException("La cámara necesita Universal Render Pipeline.");
                RenderPipeline.SubmitRenderRequest(camara, solicitud);
                RenderTexture.active = render;
                foto.ReadPixels(new Rect(0, 0, render.width, render.height), 0, 0);
                foto.Apply();
                File.WriteAllBytes(destino, foto.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = anterior;
                UnityEngine.Object.DestroyImmediate(foto);
                UnityEngine.Object.DestroyImmediate(render);
            }
        }

        private static Transform Grupo(string nombre, Transform padre = null, Vector3 posicion = default, float giro = 0)
        {
            Transform grupo = new GameObject(nombre).transform;
            grupo.SetParent(padre, false);
            grupo.localPosition = posicion;
            grupo.localRotation = Quaternion.Euler(0, giro, 0);
            return grupo;
        }

        private void Guardar(Geometria geometria, Transform grupo, bool proyectaSombra = true)
        {
            (Mesh malla, Material[] superficies) = geometria.Exportar(grupo.name);
            if (mallasCreadas)
                AssetDatabase.AddObjectToAsset(malla, RutaMallas);
            else
            {
                AssetDatabase.CreateAsset(malla, RutaMallas);
                mallasCreadas = true;
            }

            grupo.gameObject.AddComponent<MeshFilter>().sharedMesh = malla;
            var renderer = grupo.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = superficies;
            renderer.shadowCastingMode = proyectaSombra ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }

        private Material MaterialLit(string nombre, string color, float brillo, Texture2D mapa = null)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = nombre };
            material.SetColor("_BaseColor", ColorHex(color));
            material.SetFloat("_Smoothness", brillo);
            if (mapa != null)
                material.SetTexture("_BaseMap", mapa);
            material = GuardarActivo(material, $"{Raiz}/Materiales/{nombre}.mat");
            materiales.Add(nombre, material);
            return material;
        }

        private static T GuardarActivo<T>(T nuevo, string ruta) where T : UnityEngine.Object
        {
            T existente = AssetDatabase.LoadAssetAtPath<T>(ruta);
            if (existente == null)
            {
                AssetDatabase.CreateAsset(nuevo, ruta);
                return nuevo;
            }
            EditorUtility.CopySerialized(nuevo, existente);
            UnityEngine.Object.DestroyImmediate(nuevo);
            return existente;
        }

        private static void Colision(Transform grupo, Vector3 centro, Vector3 tamano)
        {
            var collider = grupo.gameObject.AddComponent<BoxCollider>();
            collider.center = centro;
            collider.size = tamano;
        }

        private static void Rotulo(Transform local, string nombre, string texto, Vector3 posicion, float tamano, Color color, float giro = 0f)
        {
            Transform nodo = Grupo(nombre, local, posicion, giro);
            var letra = nodo.gameObject.AddComponent<TextMesh>();
            letra.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            letra.text = texto;
            letra.color = color;
            letra.anchor = TextAnchor.MiddleCenter;
            letra.alignment = TextAlignment.Center;
            letra.fontSize = 90;
            letra.fontStyle = FontStyle.Bold;
            letra.characterSize = tamano / 9f;
            var renderer = nodo.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = letra.font.material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private void Suelo(Geometria malla, float minX, float maxX, float minZ, float maxZ, float y, float modulo)
        {
            malla.Cuadrado(materiales["Mosaico"],
                new Vector3(minX, y, minZ), new Vector3(minX, y, maxZ), new Vector3(maxX, y, maxZ), new Vector3(maxX, y, minZ),
                new Vector2(minX, minZ) / modulo, new Vector2(minX, maxZ) / modulo,
                new Vector2(maxX, maxZ) / modulo, new Vector2(maxX, minZ) / modulo);
        }

        private static Texture2D Textura(string nombre, int ancho, int alto, Func<float, float, Color32> pixel)
        {
            var textura = new Texture2D(ancho, alto, TextureFormat.RGB24, true) { name = nombre };
            var pixeles = new Color32[ancho * alto];
            for (int y = 0; y < alto; y++)
            for (int x = 0; x < ancho; x++)
                pixeles[y * ancho + x] = pixel((x + 0.5f) / ancho, (y + 0.5f) / alto);
            textura.SetPixels32(pixeles);
            textura.Apply();
            string ruta = $"{Raiz}/Texturas/{nombre}.png";
            File.WriteAllBytes(ruta, textura.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(textura);
            AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceSynchronousImport);
            var importador = (TextureImporter)AssetImporter.GetAtPath(ruta);
            importador.filterMode = FilterMode.Trilinear;
            importador.wrapMode = TextureWrapMode.Repeat;
            importador.anisoLevel = 8;
            importador.textureCompression = TextureImporterCompression.CompressedHQ;
            importador.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        }

        private static Color32 PixelPiso(float u, float v)
        {
            int columna = Mathf.FloorToInt(u * 8), fila = Mathf.FloorToInt(v * 8);
            int anillo = Mathf.Min(Mathf.Min(columna, 7 - columna), Mathf.Min(fila, 7 - fila));
            bool bandaOscura = anillo == 1 || anillo == 3;
            float sx = Mathf.Repeat(u * 8f, 1);
            float sy = Mathf.Repeat(v * 8f, 1);
            float junta = Mathf.Min(Mathf.Min(sx, 1 - sx), Mathf.Min(sy, 1 - sy));
            float losa = Mathf.PerlinNoise(columna * 3.7f + .4f, fila * 5.1f + .2f) - .5f;
            float veta = Mathf.PerlinNoise(u * 32f + v * 6f, v * 46f) - .5f;
            float grano = Mathf.PerlinNoise(u * 411f + 2f, v * 385f) - .5f;
            float tono = (bandaOscura ? 139f : 194f) + 19f * losa + 14f * veta + 26f * grano;
            tono = Mathf.Lerp(tono - 37f, tono, Mathf.SmoothStep(0, 1, junta / .014f));
            return new Color32((byte)(tono - 1f), (byte)tono, (byte)(tono + 1f), 255);
        }

        private static Color32 PixelMuro(float u, float v)
        {
            float hilada = Mathf.Floor(v * 9);
            float sx = Mathf.Repeat(u * 8 + hilada * 0.5f, 1);
            float sy = Mathf.Repeat(v * 9, 1);
            float mancha = Mathf.PerlinNoise(u * 33f, v * 26f) * 14f + Mathf.PerlinNoise(u * 153f, v * 123f) * 11f;
            float tono = 222 + mancha;
            if (Mathf.Min(sx, 1 - sx) < 0.012f || Mathf.Min(sy, 1 - sy) < 0.018f)
                tono -= 20;
            return new Color32((byte)tono, (byte)tono, (byte)tono, 255);
        }

        private static Color ColorHex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            return color;
        }
    }
}
