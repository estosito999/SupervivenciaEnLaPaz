using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SupervivenciaEnLaPaz.Editor
{
    // Extensión del constructor original. No regenera ni reemplaza la escena.
    internal sealed partial class ConstructorPlaza
    {
        [MenuItem("Supervivencia/Mejorar fachada del Monoblock (V2)")]
        private static void MejorarMonoblockV2()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene escena = SceneManager.GetActiveScene();
            Transform raiz = null;
            foreach (GameObject objeto in escena.GetRootGameObjects())
                if (objeto.name == "Monoblock Central · Universidad Mayor de San Andrés")
                { raiz = objeto.transform; break; }
            if (raiz == null)
            {
                EditorUtility.DisplayDialog("Monoblock V2", "Abre la escena de la Plaza Bicentenario primero.", "Aceptar");
                return;
            }
            Transform atrio = raiz.Find("Frontis y acceso principal");
            Transform torre = raiz.Find("Torre y coronación");
            if (atrio == null || torre == null ||
                !atrio.TryGetComponent<MeshFilter>(out var filtroAtrio) ||
                !torre.TryGetComponent<MeshFilter>(out var filtroTorre) ||
                !atrio.TryGetComponent<MeshRenderer>(out var rendererAtrio) ||
                !torre.TryGetComponent<MeshRenderer>(out var rendererTorre))
            {
                EditorUtility.DisplayDialog("Monoblock V2", "No se encontraron las dos mallas originales. No se modificó la escena.", "Aceptar");
                return;
            }
            if (!EditorUtility.DisplayDialog("Monoblock V2", "Se reemplazarán solo las dos mallas visuales del Monoblock. Se conservan posiciones, colisiones y objetos de jugabilidad. Guarda una copia de tu escena antes de continuar.", "Aplicar", "Cancelar")) return;
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("Monoblock V2 requiere el shader URP/Lit del proyecto original.");
                return;
            }
            var materialesV2 = new System.Collections.Generic.List<Material>();
            Material Mat(string nombre, string hex, float brillo = .18f, float metal = 0f)
            {
                var m = new Material(shader) { name = nombre };
                m.SetColor("_BaseColor", ColorHex(hex));
                m.SetFloat("_Smoothness", brillo);
                m.SetFloat("_Metallic", metal);
                materialesV2.Add(m);
                return m;
            }
            Material beige = Mat("V2 Revoque beige", "C9AC86");
            Material claro = Mat("V2 Franjas claras", "D8C3A5");
            Material sombra = Mat("V2 Cornisas", "9C8066");
            Material piedra = Mat("V2 Piedra del atrio", "B5ADA1");
            Material panel = Mat("V2 Paneles de piedra", "B7A088");
            Material relieve = Mat("V2 Relieve", "D2BFA5");
            Material oscuro = Mat("V2 Juntas", "625B52");
            Material granito = Mat("V2 Granito oscuro", "FFFFFF", .32f);
            Material marco = Mat("V2 Marcos metálicos", "A19B8D", .38f, .2f);
            Material vidrio = Mat("V2 Vidrio azul gris", "58778A", .64f, .12f);
            Material vidrio2 = Mat("V2 Vidrio en sombra", "3D5263", .55f, .1f);
            Material cortina = Mat("V2 Cortina clara", "CED2CF", .22f);
            Texture2D texturaGranito = new Texture2D(128, 128, TextureFormat.RGB24, true) { name = "V2 Granito procedural", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            var rng = new System.Random(1975);
            var px = new Color32[128 * 128];
            for (int i = 0; i < px.Length; i++)
            {
                int v = rng.Next(47, 111);
                if (rng.NextDouble() < .09) v = rng.Next(125, 166);
                px[i] = new Color32((byte)v, (byte)(v + 3), (byte)(v + 6), 255);
            }
            texturaGranito.SetPixels32(px);
            texturaGranito.Apply(true);
            granito.SetTexture("_BaseMap", texturaGranito);
            granito.SetTextureScale("_BaseMap", new Vector2(5, 5));

            void Caja(Geometria g, float x, float y, float z, float w, float h, float d, Material m, float giro = 0f)
                => g.Caja(new Vector3(x, y, z), new Vector3(w, h, d), m, giro);

            void Ventanal(Geometria g, Vector3 centro, float w, float h, int hojas, int semilla, float giro = 0, bool conCortina = false)
            {
                Quaternion r = Quaternion.Euler(0, giro, 0);
                void Pieza(float x, float y, float z, float a, float b, float c, Material m)
                    => g.Caja(centro + r * new Vector3(x, y, z), new Vector3(a, b, c), m, giro);
                Pieza(0, 0, 0, w, h, .09f, oscuro);
                float vano = (w - .12f) / hojas;
                for (int i = 0; i < hojas; i++)
                {
                    float x = -w / 2 + .06f + vano * (i + .5f);
                    Pieza(x, 0, -.061f, vano - .045f, h - .12f, .032f, (i + semilla) % 4 == 0 ? vidrio2 : vidrio);
                    if (conCortina && (i + semilla) % 3 == 0)
                        Pieza(x - vano * .19f, -.03f, -.083f, vano * .48f, h * .77f, .012f, cortina);
                    if (i > 0) Pieza(x - vano / 2, 0, -.092f, .045f, h - .06f, .05f, marco);
                }
                for (int lado = -1; lado <= 1; lado += 2)
                {
                    Pieza(lado * (w / 2 - .026f), 0, -.087f, .052f, h, .06f, marco);
                    Pieza(0, lado * (h / 2 - .026f), -.087f, w, .052f, .06f, marco);
                }
                Pieza(0, h * .24f, -.097f, w, .045f, .04f, marco);
                Pieza(0, -h / 2 - .045f, -.08f, w + .15f, .08f, .19f, piedra);
            }

            // Bloque inferior: mismas dimensiones y origen que el constructor original.
            var baseV2 = new Geometria();
            Caja(baseV2, 0, 3.42f, 5.6f, 23f, 6.84f, 11.2f, piedra);
            Caja(baseV2, 0, 6.94f, 5.18f, 23.55f, .25f, 12.25f, piedra);
            Caja(baseV2, 0, 3.0f, -.09f, 18.2f, 5.6f, .18f, oscuro);
            for (int vano = 0; vano < 3; vano++)
            {
                float x = (vano - 1) * 5.72f;
                // Ventanales grandes y puertas inferiores; franjas macizas entre ellos.
                Ventanal(baseV2, new Vector3(x, 4.03f, -.26f), 5.08f, 1.83f, 5, vano, 0, true);
                Ventanal(baseV2, new Vector3(x, 1.02f, -.26f), 5.08f, 1.94f, 5, vano + 2);
                for (int franja = 0; franja < 4; franja++)
                    Caja(baseV2, x, 2.12f + franja * .285f, -.25f, 5.15f, .26f, .20f, panel);
                for (int ranura = 0; ranura < 12; ranura++)
                    Caja(baseV2, x - 2.32f + ranura * .42f, 2.13f, -.37f, .065f, .25f, .045f, sombra);
            }
            for (int i = 0; i < 4; i++)
            {
                float x = -8.58f + i * 5.72f;
                Caja(baseV2, x, 2.64f, -.35f, .57f, 5.28f, .57f, granito);
                Caja(baseV2, x, 5.26f, -.35f, .77f, .19f, .65f, granito);
            }
            // Entablamento de piedra y friso geométrico aproximado, no calco del relieve original.
            Caja(baseV2, 0, 6.0f, -.16f, 23f, 1.65f, .32f, piedra);
            for (int i = -4; i <= 4; i++)
            {
                float x = i * 2.0f;
                Caja(baseV2, x, 5.48f, -.36f, 1.72f, .43f, .10f, relieve);
                for (int j = 0; j < 3; j++)
                {
                    float w = 1.32f - j * .31f;
                    Caja(baseV2, x, 5.62f - j * .10f, -.423f, w, .047f, .035f, sombra);
                    Caja(baseV2, x - w / 2, 5.57f - j * .10f, -.423f, .045f, .14f, .035f, sombra);
                }
            }
            // Alas laterales: se conservan sus posiciones y volumen.
            for (int lado = -1; lado <= 1; lado += 2)
            {
                float x = lado * 22.15f;
                Caja(baseV2, x, 2.85f, 6.15f, 21.75f, 5.70f, 12.3f, piedra);
                Caja(baseV2, x, 5.71f, 6.07f, 22f, .20f, 12.6f, piedra);
                for (int i = 0; i < 7; i++)
                    Ventanal(baseV2, new Vector3(lado * (12.62f + i * 2.78f), 3.88f, -.10f), 1.8f, 1.04f, 3, i);
                for (int nivel = 0; nivel < 3; nivel++)
                    Caja(baseV2, x, 1.30f + nivel * .31f, -.07f, 21.70f, .055f, .13f, sombra);
            }

            // Torre: conserva la altura, profundidad y los 13 niveles centrales existentes.
            var torreV2 = new Geometria();
            Caja(torreV2, 0, 24.16f, 7.15f, 20.8f, 34.58f, 14.10f, beige);
            Caja(torreV2, 0, 25.95f, 6.88f, 13.38f, 38.16f, 14.03f, beige);
            Caja(torreV2, 0, 44.82f, 6.9f, 13.93f, .40f, 14.22f, claro);
            Caja(torreV2, 0, 44.93f, 9.22f, 3.9f, .38f, 4f, oscuro);
            torreV2.Tubo(new Vector3(.12f, 45f, 9.35f), new Vector3(.12f, 47.33f, 9.35f), .031f, marco);
            for (int lado = -1; lado <= 1; lado += 2)
            {
                Caja(torreV2, lado * 6.11f, 25.84f, -.24f, 1.15f, 38.0f, .34f, beige);
                Caja(torreV2, lado * 8.53f, 41.51f, 7.18f, 3.75f, .30f, 14.18f, claro);
            }
            for (int nivel = 0; nivel < 13; nivel++)
            {
                float y = 9.02f + nivel * 2.69f;
                Caja(torreV2, 0, y - .94f, -.21f, 11.08f, 1.03f, .15f, claro);
                Caja(torreV2, 0, y - 1.43f, -.30f, 11.12f, .09f, .26f, sombra);
                for (int i = -2; i <= 2; i++)
                {
                    float x = i * 2.22f;
                    Ventanal(torreV2, new Vector3(x, y, -.25f), 1.91f, 1.04f, 3, i + nivel + 6, 0, nivel % 3 == 0);
                    if (nivel >= 11)
                    {
                        Caja(torreV2, x, y - .88f, -.34f, 1.42f, .43f, .14f, relieve);
                        // Motivo escalonado y rombo ornamental simplificado.
                        for (int j = 0; j < 3; j++)
                        {
                            float dx = .48f - j * .16f;
                            Caja(torreV2, x - dx, y - .98f + j * .10f, -.43f, .18f, .07f, .04f, sombra);
                            Caja(torreV2, x + dx, y - .98f + j * .10f, -.43f, .18f, .07f, .04f, sombra);
                        }
                        Caja(torreV2, x, y - .87f, -.43f, .14f, .24f, .04f, sombra);
                    }
                }
                if (nivel == 12) continue;
                for (int lado = -1; lado <= 1; lado += 2)
                {
                    Caja(torreV2, lado * 8.55f, y - .94f, .015f, 3.65f, 1.03f, .15f, claro);
                    Caja(torreV2, lado * 8.55f, y - 1.43f, -.06f, 3.65f, .09f, .24f, sombra);
                    Ventanal(torreV2, new Vector3(lado * 8.56f, y, -.015f), 2.17f, 1.05f, 3, nivel + lado + 2);
                    for (int i = 0; i < 4; i++)
                    {
                        float z = 1.75f + i * 3.55f;
                        Ventanal(torreV2, new Vector3(lado * 10.46f, y, z), 1.72f, 1.08f, 3, i + nivel, lado == 1 ? -90 : 90);
                    }
                }
            }
            Caja(torreV2, 0, 37.35f, -.34f, 11.1f, .15f, .30f, relieve);
            for (int i = 0; i < 38; i++)
                Caja(torreV2, -5.4f + i * .292f, 37.35f, -.52f, .12f, .10f, .06f, sombra);

            var (meshAtrio, matsAtrio) = baseV2.Exportar("Monoblock V2 - Frontis");
            var (meshTorre, matsTorre) = torreV2.Exportar("Monoblock V2 - Torre");
            const string carpeta = Raiz + "/Geometria";
            if (!AssetDatabase.IsValidFolder(carpeta)) AssetDatabase.CreateFolder(Raiz, "Geometria");
            string ruta = AssetDatabase.GenerateUniqueAssetPath(carpeta + "/MonoblockV2.asset");
            AssetDatabase.CreateAsset(meshAtrio, ruta);
            AssetDatabase.AddObjectToAsset(meshTorre, ruta);
            AssetDatabase.AddObjectToAsset(texturaGranito, ruta);
            foreach (Material m in materialesV2) AssetDatabase.AddObjectToAsset(m, ruta);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(ruta);

            Undo.IncrementCurrentGroup();
            int grupoUndo = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Mejorar fachada del Monoblock V2");
            Undo.RecordObjects(new UnityEngine.Object[] { filtroAtrio, filtroTorre, rendererAtrio, rendererTorre }, "Cambiar mallas y materiales");
            filtroAtrio.sharedMesh = meshAtrio;
            rendererAtrio.sharedMaterials = matsAtrio;
            filtroTorre.sharedMesh = meshTorre;
            rendererTorre.sharedMaterials = matsTorre;
            Transform rotulo = atrio.Find("Identificación de la UMSA");
            if (rotulo != null)
            {
                Undo.RecordObject(rotulo, "Reubicar rótulo");
                rotulo.localPosition = new Vector3(0, 6.20f, -.55f);
            }
            Undo.CollapseUndoOperations(grupoUndo);
            EditorSceneManager.MarkSceneDirty(escena);
            Selection.activeGameObject = raiz.gameObject;
            Debug.Log("Monoblock V2 aplicado. Revisa el resultado y guarda la escena solo si estás conforme. Recursos nuevos: " + ruta);
        }
    }
}
