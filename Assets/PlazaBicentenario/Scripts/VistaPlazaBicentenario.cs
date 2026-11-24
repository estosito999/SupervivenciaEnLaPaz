using UnityEngine;
using UnityEngine.InputSystem;

namespace SupervivenciaEnLaPaz
{
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class VistaPlazaBicentenario : MonoBehaviour
    {
        [SerializeField] private Transform vistaCenital;
        [SerializeField, Min(1f)] private float tamanoCenital = 21f;
        [SerializeField, Min(1f)] private float distancia = 4.4f;
        [SerializeField, Min(0.01f)] private float sensibilidad = 0.14f;

        private Camera camara;
        private Transform jugador;
        private Transform rival;
        private float giro;
        private float inclinacion = 9f;
        private float distanciaActual;

        internal Quaternion OrientacionHorizontal => camara.orthographic
            ? Quaternion.identity : Quaternion.Euler(0f, giro, 0f);

        private void Awake()
        {
            camara = GetComponent<Camera>();
        }

        internal void Seguir(Transform objetivo)
        {
            jugador = objetivo;
            giro = jugador.eulerAngles.y;
            distanciaActual = distancia;
            ActualizarOrbita();
            CapturarRaton(true);
        }

        internal void EnfocarRival(Transform objetivo)
        {
            rival = objetivo;
            if (rival != null)
                camara.orthographic = false;
        }

        private void Update()
        {
            Keyboard teclado = Keyboard.current;
            Mouse raton = Mouse.current;
            if (teclado?.escapeKey.wasPressedThisFrame == true)
                CapturarRaton(false);
            else if (raton?.leftButton.wasPressedThisFrame == true && Cursor.lockState != CursorLockMode.Locked)
                CapturarRaton(true);

            if (rival == null && vistaCenital != null && teclado?.tabKey.wasPressedThisFrame == true)
                camara.orthographic = !camara.orthographic;

            if (!camara.orthographic && raton != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 delta = raton.delta.ReadValue() * sensibilidad;
                giro += delta.x;
                inclinacion = Mathf.Clamp(inclinacion - delta.y, -48f, 65f);
            }
        }

        private void LateUpdate()
        {
            if (jugador == null) return;

            if (camara.orthographic)
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
            float deseada = distancia;
            if (rival != null)
            {
                Vector3 separacion = Vector3.ClampMagnitude(rival.position - jugador.position, 2.8f);
                foco += separacion * 0.5f;
                deseada += separacion.magnitude * 0.3f;
            }

            Quaternion rotacion = Quaternion.Euler(inclinacion, giro, 0f);
            Vector3 atras = rotacion * Vector3.back;
            if (Physics.SphereCast(foco, 0.22f, atras, out RaycastHit tope, deseada,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                deseada = Mathf.Max(0.12f, tope.distance - 0.04f);

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
