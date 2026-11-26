using UnityEngine;
using UnityEngine.Rendering;

namespace SupervivenciaEnLaPaz
{
    internal sealed class MarcadorMision : MonoBehaviour
    {
        private Transform flecha;

        internal void Dibujar(Material material)
        {
            const int segmentos = 48;
            var aro = new Vector3[segmentos];
            for (int i = 0; i < segmentos; i++)
            {
                float angulo = 2f * Mathf.PI * i / segmentos;
                aro[i] = new Vector3(Mathf.Cos(angulo) * 0.86f, 0.07f, Mathf.Sin(angulo) * 0.86f);
            }
            Trazo(gameObject, material, aro, true);
            flecha = new GameObject("Indicador").transform;
            flecha.SetParent(transform, false);
            Trazo(flecha.gameObject, material, new[] { new Vector3(-0.26f, 0.30f, 0), Vector3.zero,
                new Vector3(0.26f, 0.30f, 0) }, false);
        }

        private static void Trazo(GameObject objeto, Material material, Vector3[] puntos, bool cerrar)
        {
            var linea = objeto.AddComponent<LineRenderer>();
            linea.sharedMaterial = material;
            linea.useWorldSpace = false;
            linea.loop = cerrar;
            linea.widthMultiplier = 0.085f;
            linea.numCornerVertices = 3;
            linea.shadowCastingMode = ShadowCastingMode.Off;
            linea.receiveShadows = false;
            linea.positionCount = puntos.Length;
            linea.SetPositions(puntos);
        }

        internal void Indicar(Vector3 posicion, Camera camara)
        {
            transform.position = posicion;
            flecha.localPosition = Vector3.up * (2.05f + 0.12f * Mathf.Sin(Time.time * 3f));
            flecha.rotation = Quaternion.Euler(0f, camara.transform.eulerAngles.y, 0f);
        }
    }
}
