// Mejorado con referencias de cámara de SanAndreasUnity (BaseScriptState.cs / PlayerController.cs).
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
        [Header("Seguimiento y encuadre")]
        [SerializeField] private float alturaFocoPie = 1.35f;
        [SerializeField] private float alturaFocoAgachado = 0.94f;
        [SerializeField, Min(0f)] private float incrementoFovSprint = 4.5f;
        [SerializeField, Min(0f)] private float sensibilidadMando = 135f;

        private Camera camara;
        private Transform jugador;
        private Ped pedJugador;
        private Transform rival;
        private float giro, giroObjetivo;
        private float inclinacion = 9f, inclinacionObjetivo = 9f;
        private float distanciaActual, zoomObjetivo, zoomActual;
        private float alturaFocoActual;
        private float fovBase = 62f;
        private readonly RaycastHit[] impactos = new RaycastHit[32];

        internal Quaternion OrientacionHorizontal => camara != null && camara.orthographic
            ? Quaternion.identity : Quaternion.Euler(0f, giro, 0f);

        private void Awake()
        {
            camara = GetComponent<Camera>();
            fovBase = camara != null ? camara.fieldOfView : 62f;
            alturaFocoActual = alturaFocoPie;
        }

        internal void Seguir(Transform objetivo)
        {
            jugador = objetivo;
            pedJugador = jugador != null ? jugador.GetComponent<Ped>() : null;
            if (jugador == null) return;

            giro = giroObjetivo = jugador.eulerAngles.y;
            zoomActual = zoomObjetivo = Mathf.Clamp(distancia, distanciaMinima, Mathf.Max(distanciaMinima, distanciaMaxima));
            distanciaActual = zoomActual;
            alturaFocoActual = alturaFocoPie;
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
            Gamepad mando = Gamepad.current;

            if (teclado?.escapeKey.wasPressedThisFrame == true) CapturarRaton(false);
            else if (raton?.leftButton.wasPressedThisFrame == true && Cursor.lockState != CursorLockMode.Locked)
                CapturarRaton(true);

            if (rival == null && vistaCenital != null && teclado?.tabKey.wasPressedThisFrame == true)
                camara.orthographic = !camara.orthographic;

            float dt = Time.deltaTime;

            if (!camara.orthographic)
            {
                if (raton != null && Cursor.lockState == CursorLockMode.Locked)
                {
                    Vector2 delta = raton.delta.ReadValue() * sensibilidad;
                    giroObjetivo = Mathf.Repeat(giroObjetivo + delta.x, 360f);
                    inclinacionObjetivo = Mathf.Clamp(inclinacionObjetivo - delta.y, -48f, 65f);

                    float rueda = raton.scroll.ReadValue().y;
                    if (Mathf.Abs(rueda) > 0.001f)
                    {
                        zoomObjetivo = Mathf.Clamp(zoomObjetivo - Mathf.Sign(rueda) * pasoZoom,
                            distanciaMinima, Mathf.Max(distanciaMinima, distanciaMaxima));
                    }
                }

                if (mando != null)
                {
                    Vector2 stickDer = mando.rightStick.ReadValue();
                    if (stickDer.sqrMagnitude > 0.02f)
                    {
                        giroObjetivo = Mathf.Repeat(giroObjetivo + stickDer.x * sensibilidadMando * dt, 360f);
                        inclinacionObjetivo = Mathf.Clamp(inclinacionObjetivo - stickDer.y * sensibilidadMando * dt, -48f, 65f);
                    }
                }

                if (jugador != null && (teclado?.vKey.wasPressedThisFrame == true || mando?.rightStickButton.wasPressedThisFrame == true))
                    giroObjetivo = jugador.eulerAngles.y;

                // En combate, alinea suavemente sin bloquear la órbita manual.
                if (rival != null && jugador != null && (raton == null || raton.delta.ReadValue().sqrMagnitude < 0.05f))
                {
                    Vector3 haciaRival = Vector3.ProjectOnPlane(rival.position - jugador.position, Vector3.up);
                    if (haciaRival.sqrMagnitude > 0.25f)
                    {
                        float anguloRival = Quaternion.LookRotation(haciaRival).eulerAngles.y;
                        giroObjetivo = Mathf.LerpAngle(giroObjetivo, anguloRival, 1.8f * dt);
                    }
                }
            }

            float factor = 1f - Mathf.Exp(-suavizadoRotacion * dt);
            giro = Mathf.LerpAngle(giro, giroObjetivo, factor);
            inclinacion = Mathf.Lerp(inclinacion, inclinacionObjetivo, factor);
            zoomActual = Mathf.Lerp(zoomActual, zoomObjetivo, 1f - Mathf.Exp(-suavizadoZoom * dt));

            if (pedJugador != null && !camara.orthographic)
            {
                float alturaDeseada = pedJugador.EstaAgachado ? alturaFocoAgachado : alturaFocoPie;
                alturaFocoActual = Mathf.Lerp(alturaFocoActual, alturaDeseada, 1f - Mathf.Exp(-10f * dt));

                bool enSprint = pedJugador.EstadoActual == RecorridoPlaza.EstadoLocomocion.Sprint;
                float fovObjetivo = fovBase + (enSprint ? incrementoFovSprint : 0f);
                camara.fieldOfView = Mathf.Lerp(camara.fieldOfView, fovObjetivo, 1f - Mathf.Exp(-6f * dt));
            }
            else if (camara != null && !camara.orthographic)
            {
                camara.fieldOfView = Mathf.Lerp(camara.fieldOfView, fovBase, 1f - Mathf.Exp(-6f * dt));
            }
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
            Vector3 foco = jugador.position + Vector3.up * alturaFocoActual;
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
                Collider c = impactos[i].collider;
                if (c == null || c.transform == jugador || c.transform.IsChildOf(jugador)) continue;
                if (rival != null && (c.transform == rival || c.transform.IsChildOf(rival))) continue;
                if (c.GetComponentInParent<Peaton>() != null) continue;
                deseada = Mathf.Min(deseada, Mathf.Max(0.18f, impactos[i].distance - 0.04f));
            }

            // Acerca inmediatamente ante obstáculos y recupera suavemente la distancia.
            distanciaActual = Mathf.Min(deseada, distanciaActual + 8f * Time.deltaTime);
            transform.SetPositionAndRotation(foco + atras * distanciaActual, rotacion);
        }

        private static void CapturarRaton(bool capturar)
        {
            Cursor.lockState = capturar ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !capturar;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && jugador != null)
                CapturarRaton(false);
        }

        private void OnDisable()
        {
            if (jugador != null) CapturarRaton(false);
        }
    }
}
