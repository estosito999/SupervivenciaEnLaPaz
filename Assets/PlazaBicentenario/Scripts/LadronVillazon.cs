using UnityEngine;

namespace SupervivenciaEnLaPaz
{
    [DisallowMultipleComponent, RequireComponent(typeof(RivalPlaza))]
    public sealed class LadronVillazon : MonoBehaviour
    {
        private RivalPlaza motor;
        private Vector3[] recorrido;
        private Vector3 destino;
        private Vector3 ultimaPosicion;
        private int siguiente;
        private float velocidad;
        private float proximaRevision;
        private float contacto;

        internal bool Iniciar(Vector3[] puntos, float paso)
        {
            motor = GetComponent<RivalPlaza>();
            recorrido = puntos;
            velocidad = paso;
            siguiente = 1; // El primer punto es la aparición junto a las gradas.
            return TomarSiguienteTramo();
        }

        private bool TomarSiguienteTramo()
        {
            for (int i = 0; i < recorrido.Length; i++)
            {
                Vector3 candidato = recorrido[siguiente++];
                // Al alcanzar Villazón continúa entre las jardineras de ese costado.
                if (siguiente == recorrido.Length) siguiente = Mathf.Max(0, recorrido.Length - 4);
                if (!NavegacionPlaza.Instancia.Muestrear(candidato, out destino, 1.2f) ||
                    (destino - transform.position).sqrMagnitude < 0.36f || !motor.IrA(destino, velocidad))
                    continue;
                ultimaPosicion = transform.position;
                proximaRevision = Time.time + 1.5f;
                return true;
            }
            return false;
        }

        private void Update()
        {
            if (recorrido == null) return;
            if ((destino - transform.position).sqrMagnitude < 0.36f) TomarSiguienteTramo();
            else if (Time.time >= proximaRevision)
            {
                if ((transform.position - ultimaPosicion).sqrMagnitude < 0.09f) TomarSiguienteTramo();
                ultimaPosicion = transform.position;
                proximaRevision = Time.time + 1.5f;
            }
        }

        internal bool CapturadoPor(Transform jugador)
        {
            Vector3 altura = Vector3.up;
            bool cerca = (jugador.position - transform.position).sqrMagnitude < 1.5f * 1.5f;
            bool contactoLibre = cerca && !Physics.Linecast(jugador.position + altura, transform.position + altura,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            contacto = contactoLibre ? contacto + Time.deltaTime : 0f;
            return contacto >= 1f;
        }
    }
}
