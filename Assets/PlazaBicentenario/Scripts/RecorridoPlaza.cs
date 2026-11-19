using UnityEngine;
using UnityEngine.InputSystem;

namespace SupervivenciaEnLaPaz
{
    [DisallowMultipleComponent, RequireComponent(typeof(CharacterController))]
    public sealed class RecorridoPlaza : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float velocidad = 5f;

        private CharacterController controlador;
        private float velocidadVertical;

        private void Awake()
        {
            controlador = GetComponent<CharacterController>();
        }

        private void Update()
        {
            Keyboard teclado = Keyboard.current;
            Vector3 direccion = Vector3.zero;
            if (teclado != null)
            {
                bool derecha = teclado.dKey.isPressed || teclado.rightArrowKey.isPressed;
                bool izquierda = teclado.aKey.isPressed || teclado.leftArrowKey.isPressed;
                bool adelante = teclado.wKey.isPressed || teclado.upArrowKey.isPressed;
                bool atras = teclado.sKey.isPressed || teclado.downArrowKey.isPressed;
                direccion = Vector3.ClampMagnitude(
                    new Vector3((derecha ? 1 : 0) - (izquierda ? 1 : 0), 0, (adelante ? 1 : 0) - (atras ? 1 : 0)),
                    1f);
            }

            if (direccion.sqrMagnitude > 0)
                transform.rotation = Quaternion.LookRotation(direccion);

            if (controlador.isGrounded && velocidadVertical < 0)
                velocidadVertical = -2f;

            float dt = Time.deltaTime;
            velocidadVertical += Physics.gravity.y * dt;
            controlador.Move((direccion * velocidad + Vector3.up * velocidadVertical) * dt);
        }
    }
}
