using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SupervivenciaEnLaPaz
{
    /// <summary>
    /// Minimapa circular estilo GTA San Andreas para la Plaza del Bicentenario.
    /// Muestra la orientación de la cámara, la silueta de la plaza, peatones activos,
    /// rivales y el objetivo real de <see cref="GestorMisiones"/> sin solapar la guía de misión.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MinimapaGTA : MonoBehaviour
    {
        [SerializeField, Min(80f)] private float diametroBase = 136f;
        [SerializeField, Min(10f)] private float radioMundoCercano = 22f;
        [SerializeField, Min(15f)] private float radioMundoAmplio = 38f;

        private Texture2D texDisco;
        private Texture2D texBorde;
        private Texture2D texPunto;
        private GUIStyle estiloNorte;

        private VistaPlazaBicentenario vista;
        private GestorMisiones misiones;
        private CombateArena combate;
        private float siguienteBusqueda;
        private bool vistaAmplia;

        private void Awake()
        {
            CrearTexturas();
        }

        private void OnDestroy()
        {
            if (texDisco != null) Destroy(texDisco);
            if (texBorde != null) Destroy(texBorde);
            if (texPunto != null) Destroy(texPunto);
        }

        private void Update()
        {
            if (Keyboard.current?.mKey.wasPressedThisFrame == true)
                vistaAmplia = !vistaAmplia;

            if ((vista == null || misiones == null || combate == null) && Time.unscaledTime >= siguienteBusqueda)
            {
                if (vista == null && Camera.main != null) vista = Camera.main.GetComponent<VistaPlazaBicentenario>();
                if (misiones == null) misiones = FindAnyObjectByType<GestorMisiones>();
                if (combate == null) combate = FindAnyObjectByType<CombateArena>();
                siguienteBusqueda = Time.unscaledTime + 0.8f;
            }
        }

        private void OnGUI()
        {
            if (!isActiveAndEnabled || Event.current.type != EventType.Repaint)
                return;

            if (estiloNorte == null)
            {
                estiloNorte = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                estiloNorte.normal.textColor = new Color(0.95f, 0.88f, 0.65f);
            }

            float escala = Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 720f), 0.65f, 1.6f);
            Matrix4x4 anterior = GUI.matrix;
            Color colorAnterior = GUI.color;

            try
            {
                GUI.matrix = Matrix4x4.Scale(new Vector3(escala, escala, 1f));

                float diametro = diametroBase;
                float radioPx = diametro * 0.5f;
                // Ubicado justo encima de "Guía de la plaza" (esquina inferior izquierda) sin solapamiento.
                float xCentro = 20f + radioPx;
                float yCentro = Screen.height / escala - 102f - radioPx;

                float radioMundo = vistaAmplia ? radioMundoAmplio : radioMundoCercano;
                float anguloCamara = vista != null ? vista.OrientacionHorizontal.eulerAngles.y : 0f;

                // Fondo circular y borde metálico
                DibujarTexturaCentrada(texDisco, xCentro, yCentro, diametro, new Color(0.055f, 0.085f, 0.115f, 0.84f));
                DibujarTexturaCentrada(texBorde, xCentro, yCentro, diametro + 4f, new Color(0.84f, 0.72f, 0.42f, 0.92f));

                // Ejes de referencia de la Plaza del Bicentenario (puntos clave del entorno real)
                DibujarZonaMundo(new Vector3(0f, 0f, -1.3f), xCentro, yCentro, radioPx - 8f, radioMundo, anguloCamara, 7f, new Color(0.24f, 0.34f, 0.42f, 0.45f));
                DibujarZonaMundo(new Vector3(-17.05f, 0f, -10.1f), xCentro, yCentro, radioPx - 8f, radioMundo, anguloCamara, 8f, new Color(0.55f, 0.38f, 0.25f, 0.55f));
                DibujarZonaMundo(new Vector3(0f, 0f, 19.3f), xCentro, yCentro, radioPx - 8f, radioMundo, anguloCamara, 9f, new Color(0.60f, 0.56f, 0.48f, 0.45f));

                // Peatones reales activos
                IReadOnlyList<Peaton> peatones = Peaton.Activos;
                for (int i = 0; i < peatones.Count; i++)
                {
                    Peaton p = peatones[i];
                    if (p == null) continue;
                    Color c = p.Huyendo ? new Color(1f, 0.55f, 0.22f, 0.95f) : new Color(0.62f, 0.84f, 0.95f, 0.78f);
                    DibujarBlip(p.transform.position, xCentro, yCentro, radioPx - 7f, radioMundo, anguloCamara, 4.5f, c, false);
                }

                // Rival activo o ladrón en persecución
                if (combate != null && CombateArena.EnCombate && combate.ObjetivoActual != null)
                {
                    DibujarBlip(combate.ObjetivoActual.position, xCentro, yCentro, radioPx - 7f, radioMundo, anguloCamara, 7.5f,
                        new Color(0.95f, 0.25f, 0.28f, 1f), true);
                }
                else if (misiones != null && misiones.Actual == GestorMisiones.Mision.PeleaMonoblock && combate != null && combate.RivalInicial != null)
                {
                    DibujarBlip(combate.RivalInicial.position, xCentro, yCentro, radioPx - 7f, radioMundo, anguloCamara, 7f,
                        new Color(0.95f, 0.28f, 0.30f, 0.95f), true);
                }

                if (misiones != null && misiones.LadronActivo != null)
                {
                    DibujarBlip(misiones.LadronActivo.position, xCentro, yCentro, radioPx - 7f, radioMundo, anguloCamara, 8f,
                        new Color(1f, 0.55f, 0.12f, 1f), true);
                }

                // Objetivo actual de la misión
                if (misiones != null && misiones.TieneObjetivoActivo)
                {
                    float pulso = 7.5f + Mathf.Sin(Time.time * 5f) * 1.3f;
                    DibujarBlip(misiones.ObjetivoActualPosicion, xCentro, yCentro, radioPx - 6f, radioMundo, anguloCamara,
                        pulso, new Color(0.96f, 0.80f, 0.24f, 1f), true);
                }

                // Norte cardinal rotado respecto a la cámara
                Vector2 norteRadar = RotarARadar(Vector3.forward, anguloCamara);
                float nx = xCentro + norteRadar.x * (radioPx - 2f);
                float ny = yCentro - norteRadar.y * (radioPx - 2f);
                DibujarTexturaCentrada(texPunto, nx, ny, 14f, new Color(0.08f, 0.11f, 0.14f, 0.95f));
                GUI.color = Color.white;
                GUI.Label(new Rect(nx - 8f, ny - 9f, 16f, 18f), "N", estiloNorte);

                // Jugador en el centro + indicador de dirección del cuerpo
                Vector3 dirJugador = transform.forward;
                Vector2 dirRadar = RotarARadar(dirJugador, anguloCamara);
                DibujarTexturaCentrada(texPunto, xCentro + dirRadar.x * 6f, yCentro - dirRadar.y * 6f, 5f,
                    new Color(0.24f, 0.92f, 0.78f, 0.95f));
                DibujarTexturaCentrada(texPunto, xCentro, yCentro, 8f, Color.white);
            }
            finally
            {
                GUI.color = colorAnterior;
                GUI.matrix = anterior;
            }
        }

        private void DibujarZonaMundo(
            Vector3 posMundo,
            float xCentro,
            float yCentro,
            float radioPx,
            float radioMundo,
            float anguloCamara,
            float tamano,
            Color color)
        {
            Vector3 diff = posMundo - transform.position;
            diff.y = 0f;
            Vector2 local = RotarARadar(diff, anguloCamara) / radioMundo * radioPx;
            if (local.magnitude > radioPx - 4f)
                return;

            DibujarTexturaCentrada(texPunto, xCentro + local.x, yCentro - local.y, tamano, color);
        }

        private void DibujarBlip(
            Vector3 posMundo,
            float xCentro,
            float yCentro,
            float radioPx,
            float radioMundo,
            float anguloCamara,
            float tamano,
            Color color,
            bool fijarEnBorde)
        {
            Vector3 diff = posMundo - transform.position;
            diff.y = 0f;
            Vector2 local = RotarARadar(diff, anguloCamara) / radioMundo * radioPx;
            float mag = local.magnitude;
            if (mag > radioPx)
            {
                if (!fijarEnBorde)
                    return;
                local = (local / mag) * radioPx;
            }

            DibujarTexturaCentrada(texPunto, xCentro + local.x, yCentro - local.y, tamano, color);
        }

        private static Vector2 RotarARadar(Vector3 mundo, float anguloCamaraDeg)
        {
            float rad = anguloCamaraDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            float x = mundo.x * cos - mundo.z * sin;
            float y = mundo.x * sin + mundo.z * cos;
            return new Vector2(x, y);
        }

        private static void DibujarTexturaCentrada(Texture2D tex, float cx, float cy, float size, Color color)
        {
            if (tex == null) return;
            GUI.color = color;
            GUI.DrawTexture(new Rect(cx - size * 0.5f, cy - size * 0.5f, size, size), tex);
        }

        private void CrearTexturas()
        {
            texDisco = CrearTexturaCirculo(128, false);
            texBorde = CrearTexturaCirculo(128, true);
            texPunto = CrearTexturaCirculo(32, false);
        }

        private static Texture2D CrearTexturaCirculo(int size, bool soloAnillo)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float centro = (size - 1) * 0.5f;
            float radio = size * 0.48f;
            float grosor = size * 0.045f;
            var pixeles = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - centro;
                    float dy = y - centro;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha;

                    if (soloAnillo)
                    {
                        float dBorde = Mathf.Abs(dist - (radio - grosor * 0.5f));
                        alpha = Mathf.Clamp01(1f - (dBorde - grosor * 0.45f) * 0.7f);
                    }
                    else
                    {
                        alpha = Mathf.Clamp01(radio - dist + 0.5f);
                    }

                    pixeles[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            tex.SetPixels32(pixeles);
            tex.Apply();
            return tex;
        }
    }
}

