// Adaptado e integrado para SupervivenciaEnLaPaz a partir de BaseScriptState.cs y BaseMovementState.cs de SanAndreasUnity.
using System;
using SupervivenciaEnLaPaz.Core;
using UnityEngine;

namespace SupervivenciaEnLaPaz.Movimiento.States
{
    /// <summary>
    /// Clase base para los estados físicos y de locomoción controlados por <see cref="Ped"/>.
    /// </summary>
    public abstract class BaseMovementState : MonoBehaviour, IState
    {
        protected Ped m_ped;
        protected PedModel m_model => m_ped != null ? m_ped.Modelo : null;

        public object ParameterForEnteringState { get; set; }
        public double LastTimeWhenActivated { get; set; }
        public double LastTimeWhenDeactivated { get; set; }

        public bool IsActiveState => m_ped != null && ReferenceEquals(m_ped.EstadoActualMaquina, this);
        public double TimeSinceActivated => Time.timeAsDouble - LastTimeWhenActivated;
        public double TimeSinceDeactivated => Time.timeAsDouble - LastTimeWhenDeactivated;

        public virtual float TiempoMinimoSalida => m_ped != null ? m_ped.GestorConfig.tiempoMinimoSalidaEstado : 0.08f;
        public virtual float TiempoMinimoEntrada => m_ped != null ? m_ped.GestorConfig.tiempoMinimoReentradaEstado : 0.08f;

        public virtual bool EsAgachado => false;
        public virtual bool PermiteAtaque => true;
        public virtual bool PermiteGiro => true;
        public virtual bool PermiteEntradaMovimiento => true;
        public virtual bool ConsumeResistencia => false;

        public abstract RecorridoPlaza.EstadoLocomocion EstadoLegacy { get; }
        public abstract string NombreEstadoHud { get; }

        protected virtual void Awake()
        {
            m_ped = GetComponentInParent<Ped>();
        }

        public void AsignarPed(Ped ped)
        {
            m_ped = ped;
        }

        public virtual void OnBecameActive() { }

        public virtual void OnBecameInactive() { }

        public bool RepresentsState(Type type)
        {
            if (type == null) return false;
            Type myType = GetType();
            return myType == type || myType.IsSubclassOf(type);
        }

        public bool RepresentsState<T>() where T : IState => RepresentsState(typeof(T));

        public virtual void UpdateState()
        {
            if (!IsActiveState)
                return;

            EvaluarTransiciones();
        }

        public virtual void LateUpdateState()
        {
            if (!IsActiveState)
                return;

            ActualizarAnimaciones();
        }

        public virtual void FixedUpdateState() { }

        protected virtual void EvaluarTransiciones()
        {
            Type objetivo = ObtenerEstadoSiguienteSegunEntrada(m_ped);
            if (objetivo != null && objetivo != GetType())
                IntentarCambiarA(objetivo);
        }

        public static bool TiempoSuficienteParaCambiar(BaseMovementState actual, BaseMovementState destino)
        {
            if (actual == null || destino == null)
                return false;

            if (actual.TimeSinceActivated < actual.TiempoMinimoSalida)
                return false;

            if (destino.TimeSinceDeactivated < destino.TiempoMinimoEntrada)
                return false;

            return true;
        }

        public bool IntentarCambiarA(Type tipoEstado)
        {
            if (m_ped == null || tipoEstado == null)
                return false;

            var destino = m_ped.ObtenerEstado(tipoEstado);
            if (destino == null || ReferenceEquals(destino, this))
                return false;

            if (!TiempoSuficienteParaCambiar(this, destino))
                return false;

            m_ped.CambiarEstado(tipoEstado);
            return true;
        }

        public static Type ObtenerEstadoSiguienteSegunEntrada(Ped ped)
        {
            if (ped == null)
                return typeof(StandState);

            if (!ped.EnSuelo)
            {
                var jump = ped.ObtenerEstado<JumpState>();
                if (jump != null)
                    return typeof(JumpState);
            }

            if (ped.EstaEnCombate)
            {
                return ped.TieneEntradaMovimiento ? typeof(RunState) : typeof(StandState);
            }

            if (ped.IsPanicOn && ped.TieneEntradaMovimiento)
                return typeof(PanicState);

            if (ped.IsCrouchOn)
                return ped.TieneEntradaMovimiento ? typeof(CrouchMoveState) : typeof(CrouchState);

            if (!ped.TieneEntradaMovimiento)
                return typeof(StandState);

            if (ped.IsWalkOn)
                return typeof(WalkState);

            if (ped.IsSprintOn && ped.PuedeEsprintar)
                return typeof(SprintState);

            return typeof(RunState);
        }

        public virtual void AlPulsarSalto()
        {
            if (m_ped == null || m_ped.EstaEnCombate)
                return;

            var jump = m_ped.ObtenerEstado<JumpState>();
            if (jump != null && jump.PuedeSaltar() && TiempoSuficienteParaCambiar(this, jump))
                m_ped.CambiarEstado<JumpState>();
        }

        public virtual void AlPulsarAgacharse()
        {
            if (m_ped == null || m_ped.EstaEnCombate || !m_ped.EnSuelo)
                return;

            m_ped.IsCrouchOn = !m_ped.IsCrouchOn;
            Type destino = m_ped.TieneEntradaMovimiento
                ? (m_ped.IsCrouchOn ? typeof(CrouchMoveState) : typeof(RunState))
                : (m_ped.IsCrouchOn ? typeof(CrouchState) : typeof(StandState));

            if (m_ped.IsCrouchOn || m_ped.PuedeLevantarse())
                IntentarCambiarA(destino);
            else
                m_ped.IsCrouchOn = true;
        }

        public virtual void AlPulsarRodar(bool izquierda)
        {
            if (m_ped == null || !m_ped.EnSuelo || m_ped.IsCrouchOn)
                return;

            var roll = m_ped.ObtenerEstado<RollState>();
            if (roll != null && roll.PuedeRodar() && TiempoSuficienteParaCambiar(this, roll))
                roll.IniciarRodar(izquierda);
        }

        public virtual void AlPulsarEsquivar(Vector3 direccionMundo)
        {
            if (m_ped == null || !m_ped.EnSuelo)
                return;

            var dodge = m_ped.ObtenerEstado<DodgeState>();
            if (dodge != null && dodge.PuedeEsquivar() && TiempoSuficienteParaCambiar(this, dodge))
                dodge.IniciarEsquiva(direccionMundo);
        }

        public virtual void AlRecibirDano(DamageInfo info)
        {
            if (m_ped == null)
                return;

            var hit = m_ped.ObtenerEstado<HitReactionState>();
            if (hit != null && !ReferenceEquals(this, hit))
                m_ped.CambiarEstadoConParametro<HitReactionState>(info);
        }

        public virtual float CalcularVelocidadObjetivo()
        {
            return m_ped != null && m_ped.TieneEntradaMovimiento ? m_ped.VelocidadCorrer : 0f;
        }

        protected virtual void ActualizarAnimaciones()
        {
            if (m_ped == null || m_model == null)
                return;

            float refVelocidad = Mathf.Max(0.1f, m_ped.VelocidadCorrer);
            float valorSpeed = Mathf.Clamp(m_ped.VelocidadHorizontalActual / refVelocidad, 0f, 1f);
            if (m_ped.VelocidadHorizontalActual > m_ped.VelocidadCorrer && m_ped.VelocidadSprint > m_ped.VelocidadCorrer)
            {
                valorSpeed = Mathf.Lerp(1f, 1.55f,
                    Mathf.InverseLerp(m_ped.VelocidadCorrer, m_ped.VelocidadSprint, m_ped.VelocidadHorizontalActual));
            }

            m_model.ActualizarParametros(
                valorSpeed,
                m_ped.EnSuelo,
                m_ped.VelocidadVertical,
                EsAgachado,
                m_ped.EstaEnCombate,
                false);
        }
    }
}
