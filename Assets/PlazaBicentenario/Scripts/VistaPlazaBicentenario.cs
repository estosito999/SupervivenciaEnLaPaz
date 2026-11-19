using UnityEngine;
using UnityEngine.InputSystem;

namespace SupervivenciaEnLaPaz
{
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class VistaPlazaBicentenario : MonoBehaviour
    {
        [SerializeField] private Transform vistaCenital;
        [SerializeField, Min(1f)] private float tamanoCenital = 21f;

        private Camera camara;
        private Pose vistaFrontal;

        private void Awake()
        {
            camara = GetComponent<Camera>();
            vistaFrontal = new Pose(transform.position, transform.rotation);
        }

        private void Update()
        {
            if (vistaCenital == null)
                return;

            if (Keyboard.current?.tabKey.wasPressedThisFrame == true)
            {
                camara.orthographic = !camara.orthographic;
                Pose pose = camara.orthographic
                    ? new Pose(vistaCenital.position, vistaCenital.rotation)
                    : vistaFrontal;
                transform.SetPositionAndRotation(pose.position, pose.rotation);
            }

            if (camara.orthographic)
                camara.orthographicSize = Mathf.Max(tamanoCenital, 29f / camara.aspect);
        }
    }
}
