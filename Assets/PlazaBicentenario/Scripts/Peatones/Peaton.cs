using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace SupervivenciaEnLaPaz
{
    /// <summary>
    /// Peatón civil autónomo. Adapta la idea de WalkAroundState/EscapeState del
    /// módulo de SanAndreasUnity: pasea por la malla de navegación, se detiene a
    /// descansar y huye cuando hay una pelea cerca.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(CharacterController), typeof(Animator))]
    public sealed class Peaton : MonoBehaviour
    {
        private enum Estado { Paseo, Reposo, Huida, Observar }

        [Header("Movimiento")]
        [SerializeField, Min(0.1f)] private float velocidadMinima = 1.45f;
        [SerializeField, Min(0.1f)] private float velocidadMaxima = 2.1f;
        [SerializeField, Min(0.1f)] private float velocidadHuida = 2.9f;
        [SerializeField, Min(1f)] private float velocidadGiro = 420f;
        [SerializeField, Min(0.1f)] private float radioSeparacion = 1.15f;
        [SerializeField, Min(0f)] private float empujeSeparacion = 1.5f;
        [SerializeField, Min(0.1f)] private float referenciaAnimacion = 3.2f;

        [Header("Rutina")]
        [SerializeField, Min(0.1f)] private float distanciaLlegada = 0.55f;
        [SerializeField, Min(0f)] private float reposoMinimo = 1.2f;
        [SerializeField, Min(0f)] private float reposoMaximo = 5.5f;
        [SerializeField, Min(0.1f)] private float radioReaccion = 11f;
        [SerializeField, Min(0.1f)] private float duracionHuida = 4.5f;

        private static readonly List<Peaton> activos = new();
        public static IReadOnlyList<Peaton> Activos => activos;

        private CharacterController controlador;
        private Animator animador;
        private SkinnedMeshRenderer malla;
        private NavegacionPlaza navegacion;
        private GestorPeatones gestor;

        private readonly Vector3[] esquinas = new Vector3[64];
        private NavMeshPath ruta;

        private Estado estado;
        private Vector3 destino;
        private Vector3 centroPeligro;
        private bool tieneRuta;
        private bool enCombate;
        private int cantidadEsquinas;
        private int siguienteEsquina;
        private float velocidad;
        private float velocidadVertical;
        private float finReposo;
        private float finHuida;
        private float proximoRecalculo;
        private float proximoIntentoDestino;
        private float proximoChequeoAtasco;
        private Vector3 posicionAtasco;
        private int intentosAtasco;
        private float siguienteMirada;
        private bool mirandoJugador;
        private bool tieneParametroVelocidad;

        public bool Huyendo => estado == Estado.Huida;

        private void Awake()
        {
            controlador = GetComponent<CharacterController>();
            animador = GetComponent<Animator>();
            malla = GetComponentInChildren<SkinnedMeshRenderer>();
            navegacion = NavegacionPlaza.Instancia;
            ruta = new NavMeshPath();

            foreach (AnimatorControllerParameter parametro in animador.parameters)
                if (parametro.nameHash == Animator.StringToHash("Speed"))
                    tieneParametroVelocidad = true;
        }

        private void OnEnable() => activos.Add(this);
        private void OnDisable() => activos.Remove(this);

        private void Start()
        {
            velocidad = Random.Range(velocidadMinima, velocidadMaxima);
            estado = Estado.Reposo;
            finReposo = Time.time + Random.Range(0.25f, 2.2f);
            posicionAtasco = transform.position;
            proximoChequeoAtasco = Time.time + 1.2f;
            AplicarAnimacion(0f, true);
        }

        public void Configurar(GestorPeatones nuevoGestor)
        {
            gestor = nuevoGestor;
            velocidad = Random.Range(velocidadMinima, velocidadMaxima);
        }

        /// <summary>Cambia la ropa, la piel y el cabello sin compartir materiales entre peatones.</summary>
        public void ConfigurarAspecto(Material chaqueta, Material piel, Material cabello)
        {
            if (malla == null)
                return;
            Material[] actuales = malla.sharedMaterials;
            if (actuales.Length < 4)
                return;
            actuales[1] = chaqueta;
            actuales[2] = piel;
            actuales[3] = cabello;
            malla.sharedMaterials = actuales;
        }

        private void Update()
        {
            if (gestor == null)
                return;

            float dt = Time.deltaTime;
            Vector3 direccion = Vector3.zero;
            Vector3 mirada = Vector3.zero;
            float velocidadObjetivo = 0f;

            switch (estado)
            {
                case Estado.Paseo:
                    ActualizarPaseo(ref direccion, ref mirada, ref velocidadObjetivo);
                    break;
                case Estado.Reposo:
                    ActualizarReposo(ref direccion, ref mirada, ref velocidadObjetivo, dt);
                    break;
                case Estado.Huida:
                    ActualizarHuida(ref direccion, ref mirada, ref velocidadObjetivo);
                    break;
                case Estado.Observar:
                    mirada = centroPeligro - transform.position;
                    if (!enCombate)
                        VolverAReposo(Random.Range(0.6f, 2f));
                    break;
            }

            if (direccion.sqrMagnitude > 0.0001f)
            {
                Vector3 separacion = CalcularSeparacion();
                if (separacion.sqrMagnitude > 0.0001f)
                    direccion = (direccion.normalized + separacion * empujeSeparacion).normalized;
                mirada = direccion;
            }

            GirarHacia(mirada, dt, velocidadObjetivo > 0.1f ? velocidadGiro : 90f);
            Mover(direccion, velocidadObjetivo, dt);
            ComprobarAtasco();
        }

        private void ActualizarPaseo(ref Vector3 direccion, ref Vector3 mirada, ref float velocidadObjetivo)
        {
            if (!tieneRuta || Time.time >= proximoRecalculo)
            {
                if (!CalcularRuta(destino))
                {
                    if (Time.time >= proximoIntentoDestino)
                    {
                        proximoIntentoDestino = Time.time + 0.8f;
                        ElegirPaseo();
                    }
                    return;
                }
            }

            direccion = DireccionDeRuta();
            if (direccion.sqrMagnitude < 0.0001f)
            {
                VolverAReposo(Random.Range(reposoMinimo, reposoMaximo));
                return;
            }

            velocidadObjetivo = velocidad;
            mirada = direccion;
        }

        private void ActualizarReposo(ref Vector3 direccion, ref Vector3 mirada, ref float velocidadObjetivo, float dt)
        {
            if (Time.time >= finReposo)
            {
                ElegirPaseo();
                return;
            }

            if (Time.time >= siguienteMirada)
            {
                siguienteMirada = Time.time + Random.Range(1.8f, 4.5f);
                Transform jugador = gestor != null ? gestor.Jugador : null;
                mirandoJugador = jugador != null &&
                    DistanciaHorizontal(jugador.position - transform.position) < 6f && Random.value < 0.6f;
            }

            Transform objetivo = mirandoJugador && gestor != null ? gestor.Jugador : null;
            if (objetivo != null)
            {
                mirada = objetivo.position - transform.position;
                mirada.y = 0f;
            }

            // Si el jugador se acerca demasiado, el peatón da un paso lateral.
            if (gestor != null && gestor.Jugador != null)
            {
                Vector3 empuje = EmpujeDesde(gestor.Jugador.position, 1.05f);
                if (empuje.sqrMagnitude > 0.02f)
                {
                    direccion = empuje.normalized;
                    mirada = direccion;
                    velocidadObjetivo = velocidadMinima * 0.75f;
                }
            }
        }

        private void ActualizarHuida(ref Vector3 direccion, ref Vector3 mirada, ref float velocidadObjetivo)
        {
            if (Time.time >= finHuida)
            {
                estado = Estado.Observar;
                tieneRuta = false;
                return;
            }

            if (!tieneRuta || Time.time >= proximoRecalculo)
                CalcularRuta(destino);

            direccion = DireccionDeRuta();
            if (direccion.sqrMagnitude < 0.0001f)
            {
                estado = Estado.Observar;
                tieneRuta = false;
                return;
            }

            velocidadObjetivo = velocidadHuida;
            mirada = direccion;
        }

        private void ElegirPaseo()
        {
            if (gestor != null &&
                gestor.BuscarDestino(transform.position, gestor.DistanciaMinimaPaseo, gestor.DistanciaMaximaPaseo,
                    out Vector3 nuevoDestino))
            {
                destino = nuevoDestino;
                if (CalcularRuta(destino))
                {
                    estado = Estado.Paseo;
                    velocidad = Random.Range(velocidadMinima, velocidadMaxima);
                    return;
                }
            }

            estado = Estado.Reposo;
            finReposo = Time.time + Random.Range(0.8f, 1.8f);
        }

        private void VolverAReposo(float pausa)
        {
            estado = Estado.Reposo;
            tieneRuta = false;
            finReposo = Time.time + pausa;
            siguienteMirada = Time.time + Random.Range(0.4f, 1.2f);
        }

        private bool CalcularRuta(Vector3 objetivo)
        {
            if (navegacion == null || !navegacion.CalcularRuta(transform.position, objetivo, ruta))
            {
                tieneRuta = false;
                return false;
            }

            cantidadEsquinas = ruta.GetCornersNonAlloc(esquinas);
            siguienteEsquina = cantidadEsquinas > 1 ? 1 : 0;
            tieneRuta = cantidadEsquinas > 0;
            proximoRecalculo = Time.time + Random.Range(1.1f, 1.7f);
            return tieneRuta;
        }

        private Vector3 DireccionDeRuta()
        {
            if (!tieneRuta || cantidadEsquinas == 0)
                return Vector3.zero;

            while (siguienteEsquina < cantidadEsquinas - 1 &&
                   DistanciaHorizontal(esquinas[siguienteEsquina] - transform.position) < 0.3f)
                siguienteEsquina++;

            Vector3 diferencia = esquinas[siguienteEsquina] - transform.position;
            diferencia.y = 0f;
            float distancia = diferencia.magnitude;
            if (distancia < distanciaLlegada && siguienteEsquina >= cantidadEsquinas - 1)
                return Vector3.zero;
            return diferencia / Mathf.Max(distancia, 0.001f);
        }

        public void ReaccionarAlCombate(Vector3 centro, float radio)
        {
            if (radio > 0f)
                radioReaccion = radio;

            if (DistanciaHorizontal(centro - transform.position) > radioReaccion)
                return;

            enCombate = true;
            centroPeligro = centro;

            if (estado == Estado.Huida || estado == Estado.Observar)
                return;

            Vector3 alejamiento = transform.position - centro;
            alejamiento.y = 0f;
            if (alejamiento.sqrMagnitude < 0.01f)
                alejamiento = Random.insideUnitSphere;

            if (gestor != null && gestor.BuscarPuntoHuida(centro, transform.position, out Vector3 punto))
            {
                destino = punto;
                if (!CalcularRuta(destino))
                {
                    estado = Estado.Observar;
                    return;
                }
            }
            else
            {
                Vector3 candidato = centro + alejamiento.normalized * (radioReaccion + 4f);
                if (!navegacion.Muestrear(candidato, out destino, 4f) || !CalcularRuta(destino))
                {
                    estado = Estado.Observar;
                    return;
                }
            }

            estado = Estado.Huida;
            finHuida = Time.time + duracionHuida;
        }

        public void Calmarse()
        {
            enCombate = false;
            if (estado == Estado.Huida || estado == Estado.Observar)
                VolverAReposo(Random.Range(1f, 2.6f));
        }

        public void Reubicar(Vector3 punto)
        {
            controlador.enabled = false;
            transform.position = punto;
            Physics.SyncTransforms();
            controlador.enabled = true;

            velocidadVertical = 0f;
            posicionAtasco = punto;
            intentosAtasco = 0;
            tieneRuta = false;
            enCombate = CombateArena.EnCombate;
            if (enCombate)
                estado = Estado.Observar;
            else
                VolverAReposo(Random.Range(0.6f, 1.8f));
        }

        private void ComprobarAtasco()
        {
            if (Time.time < proximoChequeoAtasco)
                return;

            float avance = DistanciaHorizontal(transform.position - posicionAtasco);
            bool caminando = estado == Estado.Paseo || estado == Estado.Huida;
            if (caminando && avance < 0.18f)
            {
                intentosAtasco++;
                if (intentosAtasco >= 3)
                {
                    intentosAtasco = 0;
                    if (gestor != null)
                        gestor.Reubicar(this);
                }
                else if (estado == Estado.Paseo)
                {
                    ElegirPaseo();
                }
                else
                {
                    CalcularRuta(destino);
                }
            }
            else if (avance >= 0.18f)
            {
                intentosAtasco = 0;
            }

            posicionAtasco = transform.position;
            proximoChequeoAtasco = Time.time + 1.2f;
        }

        private Vector3 CalcularSeparacion()
        {
            Vector3 empuje = Vector3.zero;

            for (int i = 0; i < activos.Count; i++)
            {
                Peaton otro = activos[i];
                if (otro == null || otro == this)
                    continue;
                empuje += EmpujeDesde(otro.transform.position, radioSeparacion);
            }

            if (gestor != null)
            {
                if (gestor.Jugador != null)
                    empuje += EmpujeDesde(gestor.Jugador.position, 1.2f);
                if (gestor.Rival != null && gestor.Rival.gameObject.activeInHierarchy)
                    empuje += EmpujeDesde(gestor.Rival.position, 1.2f);
            }

            return Vector3.ClampMagnitude(empuje, 1.5f);
        }

        private Vector3 EmpujeDesde(Vector3 posicion, float radio)
        {
            Vector3 diferencia = transform.position - posicion;
            diferencia.y = 0f;
            float distancia = diferencia.magnitude;
            if (distancia < 0.001f || distancia >= radio)
                return Vector3.zero;
            return diferencia / distancia * (1f - distancia / radio);
        }

        private void GirarHacia(Vector3 direccion, float dt, float gradosPorSegundo)
        {
            direccion.y = 0f;
            if (direccion.sqrMagnitude < 0.0001f)
                return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(direccion.normalized), gradosPorSegundo * dt);
        }

        private void Mover(Vector3 direccion, float velocidadObjetivo, float dt)
        {
            if (controlador.isGrounded && velocidadVertical < 0f)
                velocidadVertical = -2f;
            velocidadVertical += Physics.gravity.y * dt;

            Vector3 movimiento = direccion * velocidadObjetivo + Vector3.up * velocidadVertical;
            controlador.Move(movimiento * dt);
            AplicarAnimacion(velocidadObjetivo > 0.05f ? velocidadObjetivo / referenciaAnimacion : 0f, false);
        }

        private void AplicarAnimacion(float valor, bool inmediato)
        {
            if (!tieneParametroVelocidad)
                return;
            if (inmediato)
                animador.SetFloat("Speed", valor);
            else
                animador.SetFloat("Speed", valor, 0.12f, Time.deltaTime);
        }

        private static float DistanciaHorizontal(Vector3 diferencia)
        {
            diferencia.y = 0f;
            return diferencia.magnitude;
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
                return;
            Gizmos.color = estado == Estado.Huida ? new Color(1f, 0.45f, 0.35f) : new Color(0.4f, 0.9f, 0.7f);
            if (tieneRuta)
                Gizmos.DrawLine(transform.position, destino);
            Gizmos.DrawWireSphere(destino, 0.35f);
        }
    }
}
