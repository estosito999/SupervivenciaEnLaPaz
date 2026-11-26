// Adaptado e integrado para SupervivenciaEnLaPaz a partir de PlayerController.cs de SanAndreasUnity.
using UnityEngine;
using UnityEngine.InputSystem;

namespace SupervivenciaEnLaPaz
{
    /// <summary>
    /// Traduce las entradas del Unity Input System hacia la máquina de estados del <see cref="Ped"/>
    /// manteniendo movimiento relativo a la cámara y controles estilo GTA San Andreas.
    /// </summary>
    [DefaultExecutionOrder(-5)]
    [DisallowMultipleComponent, RequireComponent(typeof(Ped))]
    public sealed class PlayerController : MonoBehaviour
    {
        private Ped ped;
        private VistaPlazaBicentenario vista;

        private void Awake()
        {
            ped = GetComponent<Ped>();
            ped.Rol = Utilidades.PedestrianRole.Jugador;
        }

        private void Start()
        {
            ResolverCamara();
        }

        public void EstablecerVista(VistaPlazaBicentenario camaraVista)
        {
            vista = camaraVista;
        }

        private void ResolverCamara()
        {
            if (vista != null)
                return;

            Camera mainCam = Camera.main;
            if (mainCam != null)
                vista = mainCam.GetComponent<VistaPlazaBicentenario>();
        }

        private void Update()
        {
            if (ped == null || ped.Controlador == null || !ped.Controlador.enabled)
                return;

            ResolverCamara();

            Keyboard teclado = Keyboard.current;
            Mouse raton = Mouse.current;
            Gamepad mando = Gamepad.current;

            ped.ResetearEntrada();

            Vector2 ejes = LeerMovimiento(teclado, mando);
            Vector3 entradaLocal = Vector3.ClampMagnitude(new Vector3(ejes.x, 0f, ejes.y), 1f);
            Quaternion orientacion = vista != null ? vista.OrientacionHorizontal : Quaternion.identity;
            Vector3 direccionMundo = orientacion * entradaLocal;

            ped.EntradaMovimiento = direccionMundo;
            if (direccionMundo.sqrMagnitude > 0.001f)
                ped.DireccionMirada = direccionMundo.normalized;

            bool caminar = teclado != null && teclado.leftAltKey.isPressed;
            bool sprint = (teclado != null && (teclado.leftShiftKey.isPressed || teclado.rightShiftKey.isPressed)) ||
                          (mando != null && mando.leftStickButton.isPressed);

            if (entradaLocal.sqrMagnitude > 0.001f)
            {
                if (caminar) ped.IsWalkOn = true;
                else if (sprint) ped.IsSprintOn = true;
                else ped.IsRunOn = true;
            }

            if ((teclado != null && teclado.spaceKey.wasPressedThisFrame) ||
                (mando != null && mando.buttonSouth.wasPressedThisFrame))
            {
                ped.IsJumpPressedThisFrame = true;
                ped.AlPulsarSalto();
            }

            if ((teclado != null && teclado.cKey.wasPressedThisFrame) ||
                (mando != null && mando.buttonEast.wasPressedThisFrame))
            {
                ped.AlPulsarAgacharse();
            }

            if (teclado != null && teclado.qKey.wasPressedThisFrame)
            {
                ped.AlPulsarRodar(true);
            }
            else if (teclado != null && teclado.eKey.wasPressedThisFrame)
            {
                ped.AlPulsarRodar(false);
            }

            bool esquivaPulsada = (teclado != null && teclado.leftCtrlKey.wasPressedThisFrame) ||
                                  (raton != null && raton.rightButton.wasPressedThisFrame && Cursor.lockState == CursorLockMode.Locked);
            if (esquivaPulsada)
            {
                Vector3 dirEsquiva = direccionMundo.sqrMagnitude > 0.001f ? direccionMundo : -transform.forward;
                ped.AlPulsarEsquivar(dirEsquiva);
            }
        }

        private static Vector2 LeerMovimiento(Keyboard teclado, Gamepad mando)
        {
            float x = 0f;
            float y = 0f;

            if (teclado != null)
            {
                if (teclado.dKey.isPressed || teclado.rightArrowKey.isPressed) x += 1f;
                if (teclado.aKey.isPressed || teclado.leftArrowKey.isPressed) x -= 1f;
                if (teclado.wKey.isPressed || teclado.upArrowKey.isPressed) y += 1f;
                if (teclado.sKey.isPressed || teclado.downArrowKey.isPressed) y -= 1f;
            }

            if (mando != null)
            {
                Vector2 stick = mando.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.02f)
                {
                    x += stick.x;
                    y += stick.y;
                }
            }

            return new Vector2(x, y);
        }
    }
}
