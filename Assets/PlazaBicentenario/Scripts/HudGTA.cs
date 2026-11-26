using UnityEngine;

namespace SupervivenciaEnLaPaz
{
    // HUD independiente; no reemplaza los paneles de mision ni las barras del duelo.
    [DisallowMultipleComponent]
    public sealed class HudGTA : MonoBehaviour
    {
        private RecorridoPlaza jugador;
        private CombateArena combate;
        private GUIStyle titulo, texto, derecha;
        private float siguienteBusqueda;

        private void Awake()
        {
            jugador = GetComponent<RecorridoPlaza>();
        }

        private void Update()
        {
            if (combate == null && Time.unscaledTime >= siguienteBusqueda)
            {
                combate = FindFirstObjectByType<CombateArena>();
                siguienteBusqueda = Time.unscaledTime + 1f;
            }
        }

        private void OnGUI()
        {
            if (jugador == null || CombateArena.EnCombate || !jugador.isActiveAndEnabled) return;
            if (Event.current.type != EventType.Repaint) return;
            if (titulo == null)
            {
                titulo = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
                titulo.normal.textColor = Color.white;
                texto = new GUIStyle(GUI.skin.label) { fontSize = 12 };
                texto.normal.textColor = new Color(0.82f, 0.85f, 0.88f);
                derecha = new GUIStyle(texto) { alignment = TextAnchor.MiddleRight, fontStyle = FontStyle.Bold };
            }
            float escala = Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 720f), 0.65f, 1.6f);
            Matrix4x4 anterior = GUI.matrix;
            Color color = GUI.color;
            try
            {
                GUI.matrix = Matrix4x4.Scale(new Vector3(escala, escala, 1f));
                float x = Screen.width / escala - 246f, y = 20f;
                Rect panel = new Rect(x, y, 226f, 100f);
                Rellenar(panel, new Color(0.055f, 0.065f, 0.08f, 0.86f));
                Rellenar(new Rect(x, y, 3f, 100f), new Color(0.88f, 0.72f, 0.38f));
                GUI.color = Color.white;
                GUI.Label(new Rect(x + 12f, y + 6f, 202f, 25f), "SUPERVIVENCIA LA PAZ", titulo);
                GUI.Label(new Rect(x + 12f, y + 33f, 95f, 18f), "VIDA", texto);
                string vida = combate != null ? $"{combate.VidaJugador}/{combate.VidaMaximaJugador}" : "--";
                GUI.Label(new Rect(x + 105f, y + 33f, 107f, 18f), vida, derecha);
                Rellenar(new Rect(x + 12f, y + 53f, 202f, 8f), new Color(0.2f, 0.21f, 0.24f));
                if (combate != null)
                {
                    float fraccion = Mathf.Clamp01((float)combate.VidaJugador / combate.VidaMaximaJugador);
                    Rellenar(new Rect(x + 12f, y + 53f, 202f * fraccion, 8f), new Color(0.85f, 0.23f, 0.24f));
                }
                GUI.color = Color.white;
                GUI.Label(new Rect(x + 12f, y + 72f, 116f, 20f), jugador.EstadoActual.ToString().ToUpperInvariant(), texto);
                GUI.Label(new Rect(x + 128f, y + 72f, 85f, 20f), $"{jugador.VelocidadActual * 3.6f:0} km/h", derecha);
            }
            finally
            {
                GUI.color = color;
                GUI.matrix = anterior;
            }
        }

        private static void Rellenar(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
        }
    }
}
