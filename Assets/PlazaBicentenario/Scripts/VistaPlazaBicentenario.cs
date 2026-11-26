using UnityEngine;
using UnityEngine.InputSystem;

namespace SupervivenciaEnLaPaz
{
    [DefaultExecutionOrder(-20)]
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class VistaPlazaBicentenario : MonoBehaviour
    {
        [SerializeField] private Transform vistaCenital;
        [SerializeField, Min(1f)] private float tamanoCenital = 21f;
        [SerializeField, Min(1f)] private float distancia = 4.4f;
        [SerializeField, Min(0.01f)] private float sensibilidad = 0.14f;
        [Header("Camara estilo GTA")]
        [SerializeField, Min(0.1f)] private float suavizadoRotacion = 18f;
        [SerializeField, Min(0.1f)] private float suavizadoZoom = 10f;
        [SerializeField, Min(0.2f)] private float distanciaMinima = 2f;
        [SerializeField, Min(0.5f)] private float distanciaMaxima = 7f;
        [SerializeField, Min(0.1f)] private float pasoZoom = 0.55f;
        [SerializeField, Min(0.05f)] private float radioColision = 0.25f;
        [SerializeField] private LayerMask capasColision = Physics.DefaultRaycastLayers;

        private Camera camara;
        private Transform jugador;
        private Transform rival;
        private float giro, giroObjetivo;
        private float inclinacion = 9f, inclinacionObjetivo = 9f;
        private float distanciaActual, zoomObjetivo, zoomActual;
        private readonly RaycastHit[] impactos = new RaycastHit[32];

        internal Quaternion OrientacionHorizontal => camara != null && camara.orthographic
            ? Quaternion.identity : Quaternion.Euler(0f, giro, 0f);

        private void Awake()
        {
            camara = GetComponent<Camera>();
        }

        internal void Seguir(Transform objetivo)
        {
            jugador = objetivo;
            if (jugador == null) return;
            giro = giroObjetivo = jugador.eulerAngles.y;
            zoomActual = zoomObjetivo = Mathf.Clamp(distancia, distanciaMinima, Mathf.Max(distanciaMinima, distanciaMaxima));
            distanciaActual = zoomActual;
            if (!camara.orthographic) ActualizarOrbita();
            CapturarRaton(true);
        }

        internal void EnfocarRival(Transform objetivo)
        {
            rival = objetivo;
            if (rival != null) camara.orthographic = false;
        }

        private void Update()
        {
            Keyboard teclado = Keyboard.current;
            Mouse raton = Mouse.current;
            if (teclado?.escapeKey.wasPressedThisFrame == true) CapturarRaton(false);
            else if (raton?.leftButton.wasPressedThisFrame == true && Cursor.lockState != CursorLockMode.Locked)
                CapturarRaton(true);

            if (rival == null && vistaCenital != null && teclado?.tabKey.wasPressedThisFrame == true)
                camara.orthographic = !camara.orthographic;

            if (!camara.orthographic && raton != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 delta = raton.delta.ReadValue() * sensibilidad;
                giroObjetivo = Mathf.Repeat(giroObjetivo + delta.x, 360f);
                inclinacionObjetivo = Mathf.Clamp(inclinacionObjetivo - delta.y, -48f, 65f);
                // La rueda del nuevo Input System puede reportar 120 por paso en Windows.
                float rueda = raton.scroll.ReadValue().y;
                if (Mathf.Abs(rueda) > 0.001f)
                    zoomObjetivo = Mathf.Clamp(zoomObjetivo - Mathf.Sign(rueda) * pasoZoom,
                        distanciaMinima, Mathf.Max(distanciaMinima, distanciaMaxima));
            }
            if (jugador != null && !camara.orthographic && teclado?.vKey.wasPressedThisFrame == true)
                giroObjetivo = jugador.eulerAngles.y;

            float dt = Time.deltaTime;
            float factor = 1f - Mathf.Exp(-suavizadoRotacion * dt);
            giro = Mathf.LerpAngle(giro, giroObjetivo, factor);
            inclinacion = Mathf.Lerp(inclinacion, inclinacionObjetivo, factor);
            zoomActual = Mathf.Lerp(zoomActual, zoomObjetivo, 1f - Mathf.Exp(-suavizadoZoom * dt));
        }

        private void LateUpdate()
        {
            if (jugador == null) return;
            if (camara.orthographic && vistaCenital != null)
            {
                transform.SetPositionAndRotation(vistaCenital.position, vistaCenital.rotation);
                camara.orthographicSize = Mathf.Max(tamanoCenital, 29f / camara.aspect);
                return;
            }
            ActualizarOrbita();
        }

        private void ActualizarOrbita()
        {
            Vector3 foco = jugador.position + Vector3.up * 1.35f;
            float deseada = zoomActual;
            if (rival != null)
            {
                Vector3 separacion = Vector3.ClampMagnitude(rival.position - jugador.position, 2.8f);
                foco += separacion * 0.5f;
                deseada += separacion.magnitude * 0.3f;
            }
            Quaternion rotacion = Quaternion.Euler(inclinacion, giro, 0f);
            Vector3 atras = rotacion * Vector3.back;
            int n = Physics.SphereCastNonAlloc(foco, radioColision, atras, impactos, deseada,
                capasColision, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var c = impactos[i].collider;
                if (c == null || c.transform == jugador || c.transform.IsChildOf(jugador)) continue;
                if (rival != null && (c.transform == rival || c.transform.IsChildOf(rival))) continue;
                deseada = Mathf.Min(deseada, Mathf.Max(0.02f, impactos[i].distance - 0.04f));
            }
            // Acerca inmediatamente ante obstaculos y recupera suavemente la distancia.
            distanciaActual = Mathf.Min(deseada, distanciaActual + 8f * Time.deltaTime);
            transform.SetPositionAndRotation(foco + atras * distanciaActual, rotacion);
        }

        private static void CapturarRaton(bool capturar)
        {
            Cursor.lockState = capturar ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !capturar;
        }

        private void OnDisable()
        {
            if (jugador != null) CapturarRaton(false);
        }
    }
}
