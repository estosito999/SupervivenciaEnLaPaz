using SupervivenciaEnLaPaz.Core;
using UnityEngine;

namespace SupervivenciaEnLaPaz
{
    /// <summary>
    /// HUD estilo GTA San Andreas alimentado por sistemas reales del juego:
    /// vida, resistencia física, estado de movimiento, velocidad, reloj de La Paz,
    /// dinero en bolivianos, nivel de alerta pública e indicador compacto de misión.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HudGTA : MonoBehaviour
    {
        private RecorridoPlaza jugador;
        private Ped ped;
        private CombateArena combate;
        private GestorMisiones misiones;
        private GUIStyle estiloTitulo, estiloEtiqueta, estiloDerecha, estiloDinero, estiloReloj, estiloAlerta, estiloMision;
        private float siguienteBusqueda;

        private void Awake()
        {
            jugador = GetComponent<RecorridoPlaza>();
            ped = GetComponent<Ped>();
        }

        private void Update()
        {
            if ((combate == null || misiones == null) && Time.unscaledTime >= siguienteBusqueda)
            {
                if (combate == null) combate = FindAnyObjectByType<CombateArena>();
                if (misiones == null) misiones = FindAnyObjectByType<GestorMisiones>();
                siguienteBusqueda = Time.unscaledTime + 0.8f;
            }
        }

        private void OnGUI()
        {
            if (jugador == null || CombateArena.EnCombate || !jugador.isActiveAndEnabled)
                return;
            if (Event.current.type != EventType.Repaint)
                return;

            AsegurarEstilos();

            float escala = Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 720f), 0.65f, 1.6f);
            Matrix4x4 anterior = GUI.matrix;
            Color colorAnterior = GUI.color;

            try
            {
                GUI.matrix = Matrix4x4.Scale(new Vector3(escala, escala, 1f));

                float ancho = 244f;
                float alto = 152f;
                float x = Screen.width / escala - ancho - 18f;
                float y = 18f;

                Rellenar(new Rect(x, y, ancho, alto), new Color(0.055f, 0.068f, 0.086f, 0.88f));
                Rellenar(new Rect(x, y, 3f, alto), new Color(0.88f, 0.72f, 0.38f));

                EstadoMundoPlaza mundo = EstadoMundoPlaza.Instancia;
                string hora = mundo != null ? mundo.HoraFormateada : "16:30";
                int dineroBs = mundo != null ? mundo.DineroBs : 0;
                int alerta = mundo != null ? mundo.NivelAlerta : 0;

                // Encabezado y reloj real
                GUI.color = Color.white;
                GUI.Label(new Rect(x + 12f, y + 6f, 152f, 22f), "LA PAZ · BICENTENARIO", estiloTitulo);
                GUI.Label(new Rect(x + 162f, y + 6f, 70f, 22f), hora, estiloReloj);

                // Dinero en bolivianos y estrellas de alerta
                GUI.Label(new Rect(x + 12f, y + 27f, 120f, 22f), $"Bs. {dineroBs}", estiloDinero);
                DibujarEstrellasAlerta(x + 132f, y + 28f, alerta, mundo);

                // Barra de vida real
                int vidaActual = combate != null ? combate.VidaJugador : (ped != null && ped.Salud != null ? Mathf.RoundToInt(ped.Salud.SaludActual) : 100);
                int vidaMax = combate != null ? combate.VidaMaximaJugador : (ped != null && ped.Salud != null ? Mathf.RoundToInt(ped.Salud.SaludMaxima) : 100);
                float fracVida = vidaMax > 0 ? Mathf.Clamp01((float)vidaActual / vidaMax) : 0f;

                GUI.color = Color.white;
                GUI.Label(new Rect(x + 12f, y + 51f, 95f, 16f), "VIDA", estiloEtiqueta);
                GUI.Label(new Rect(x + 120f, y + 51f, 112f, 16f), $"{vidaActual}/{vidaMax}", estiloDerecha);
                DibujarBarra(new Rect(x + 12f, y + 69f, ancho - 24f, 8f), fracVida, new Color(0.86f, 0.24f, 0.24f));

                // Barra de resistencia real
                float fracResistencia = ped != null ? ped.FraccionResistencia : 1f;
                Color colorResistencia = ped != null && ped.EstaAgotado
                    ? new Color(0.92f, 0.45f, 0.16f)
                    : new Color(0.27f, 0.78f, 0.65f);

                GUI.color = Color.white;
                GUI.Label(new Rect(x + 12f, y + 81f, 110f, 16f), "RESISTENCIA", estiloEtiqueta);
                GUI.Label(new Rect(x + 120f, y + 81f, 112f, 16f), $"{Mathf.RoundToInt(fracResistencia * 100f)}%", estiloDerecha);
                DibujarBarra(new Rect(x + 12f, y + 99f, ancho - 24f, 7f), fracResistencia, colorResistencia);

                // Estado del personaje y velocidad horizontal real
                string estado = ped != null ? ped.NombreEstadoActual.ToUpperInvariant() : jugador.EstadoActual.ToString().ToUpperInvariant();
                GUI.color = Color.white;
                GUI.Label(new Rect(x + 12f, y + 111f, 130f, 18f), estado, estiloEtiqueta);
                GUI.Label(new Rect(x + 142f, y + 111f, 90f, 18f), $"{jugador.VelocidadActual * 3.6f:0.0} km/h", estiloDerecha);

                // Indicador compacto de misión y distancia al objetivo real
                string textoMision = "Recorrido libre";
                if (misiones != null)
                {
                    textoMision = misiones.NombreMisionCorto;
                    if (misiones.TieneObjetivoActivo)
                    {
                        float dist = Vector3.Distance(
                            Vector3.ProjectOnPlane(transform.position, Vector3.up),
                            Vector3.ProjectOnPlane(misiones.ObjetivoActualPosicion, Vector3.up));
                        textoMision = $"{textoMision} ({dist:0} m)";
                    }
                }
                GUI.Label(new Rect(x + 12f, y + 129f, ancho - 24f, 18f), textoMision, estiloMision);
            }
            finally
            {
                GUI.color = colorAnterior;
                GUI.matrix = anterior;
            }
        }

        private void DibujarEstrellasAlerta(float x, float y, int nivel, EstadoMundoPlaza mundo)
        {
            const int maxEstrellas = 3;
            for (int i = 0; i < maxEstrellas; i++)
            {
                bool activa = i < nivel;
                estiloAlerta.normal.textColor = activa
                    ? new Color(0.98f, 0.78f, 0.20f)
                    : new Color(0.32f, 0.36f, 0.42f, 0.75f);
                GUI.Label(new Rect(x + i * 19f, y, 18f, 20f), "★", estiloAlerta);
            }

            if (nivel > 0 && mundo != null)
            {
                DibujarBarra(new Rect(x, y + 19f, maxEstrellas * 19f - 4f, 3f),
                    mundo.FraccionEnfriamientoAlerta, new Color(0.98f, 0.78f, 0.20f));
            }
        }

        private void AsegurarEstilos()
        {
            if (estiloTitulo != null)
                return;

            estiloTitulo = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
            estiloTitulo.normal.textColor = Color.white;

            estiloEtiqueta = new GUIStyle(GUI.skin.label) { fontSize = 11 };
            estiloEtiqueta.normal.textColor = new Color(0.82f, 0.86f, 0.90f);

            estiloDerecha = new GUIStyle(estiloEtiqueta)
            {
                alignment = TextAnchor.MiddleRight,
                fontStyle = FontStyle.Bold
            };

            estiloDinero = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
            estiloDinero.normal.textColor = new Color(0.42f, 0.86f, 0.48f);

            estiloReloj = new GUIStyle(estiloTitulo) { alignment = TextAnchor.MiddleRight };
            estiloReloj.normal.textColor = new Color(0.95f, 0.87f, 0.65f);

            estiloAlerta = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            estiloMision = new GUIStyle(estiloEtiqueta) { fontSize = 11, fontStyle = FontStyle.Italic };
            estiloMision.normal.textColor = new Color(0.88f, 0.76f, 0.44f);
        }

        private static void DibujarBarra(Rect rect, float fraccion, Color colorRelleno)
        {
            Rellenar(rect, new Color(0.18f, 0.20f, 0.24f, 0.95f));
            if (fraccion > 0.001f)
                Rellenar(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fraccion), rect.height), colorRelleno);
        }

        private static void Rellenar(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
        }
    }
}
