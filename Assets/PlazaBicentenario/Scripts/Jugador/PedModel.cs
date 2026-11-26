// Adaptado e integrado para SupervivenciaEnLaPaz a partir de PedModel.cs de SanAndreasUnity.
using UnityEngine;

namespace SupervivenciaEnLaPaz
{
    /// <summary>
    /// Controlador del modelo humanoide y del Animator de Unity 6.
    /// Reemplaza la decodificación binaria (.dff/.ifp) de SanAndreasUnity por
    /// parámetros Mecanim seguros y compensaciones posturales no destructivas.
    /// </summary>
    [DefaultExecutionOrder(20)]
    [DisallowMultipleComponent]
    public sealed class PedModel : MonoBehaviour
    {
        private static readonly int HashSpeed = Animator.StringToHash("Speed");
        private static readonly int HashEnCombate = Animator.StringToHash("EnCombate");
        private static readonly int HashGrounded = Animator.StringToHash("Grounded");
        private static readonly int HashVerticalSpeed = Animator.StringToHash("VerticalSpeed");
        private static readonly int HashCrouching = Animator.StringToHash("Crouching");
        private static readonly int HashPanic = Animator.StringToHash("Panic");
        private static readonly int HashRollDir = Animator.StringToHash("RollDir");
        private static readonly int HashJump = Animator.StringToHash("Jump");
        private static readonly int HashRoll = Animator.StringToHash("Roll");
        private static readonly int HashDodge = Animator.StringToHash("Dodge");
        private static readonly int HashHit = Animator.StringToHash("Hit");

        private Animator animador;
        private SkinnedMeshRenderer malla;

        private bool tieneSpeed;
        private bool tieneEnCombate;
        private bool tieneGrounded;
        private bool tieneVerticalSpeed;
        private bool tieneCrouching;
        private bool tienePanic;
        private bool tieneRollDir;
        private bool tieneJump;
        private bool tieneRoll;
        private bool tieneDodge;
        private bool tieneHit;

        // Pose procedural temporal aplicada en LateUpdate sobre el resultado del Animator.
        private bool poseActiva;
        private float offsetVerticalHips;
        private Quaternion rotacionExtraHips = Quaternion.identity;
        private Quaternion rotacionExtraEspina = Quaternion.identity;

        public Animator Animador => animador;
        public SkinnedMeshRenderer Malla => malla;

        public Transform Hips { get; private set; }
        public Transform Spine { get; private set; }
        public Transform Chest { get; private set; }
        public Transform Head { get; private set; }
        public Transform RightHand { get; private set; }
        public Transform LeftHand { get; private set; }

        private void Awake()
        {
            InicializarComponentes();
        }

        public void InicializarComponentes()
        {
            if (animador == null)
                animador = GetComponentInChildren<Animator>(true);
            if (malla == null)
                malla = GetComponentInChildren<SkinnedMeshRenderer>(true);

            if (animador == null)
                return;

            // CharacterController es la única autoridad del movimiento físico.
            animador.applyRootMotion = false;
            CachearParametros();
            CachearHuesos();
        }

        private void CachearParametros()
        {
            tieneSpeed = tieneEnCombate = tieneGrounded = tieneVerticalSpeed = false;
            tieneCrouching = tienePanic = tieneRollDir = false;
            tieneJump = tieneRoll = tieneDodge = tieneHit = false;

            if (animador == null || animador.runtimeAnimatorController == null)
                return;

            foreach (AnimatorControllerParameter p in animador.parameters)
            {
                if (p.nameHash == HashSpeed && p.type == AnimatorControllerParameterType.Float) tieneSpeed = true;
                else if (p.nameHash == HashEnCombate && p.type == AnimatorControllerParameterType.Bool) tieneEnCombate = true;
                else if (p.nameHash == HashGrounded && p.type == AnimatorControllerParameterType.Bool) tieneGrounded = true;
                else if (p.nameHash == HashVerticalSpeed && p.type == AnimatorControllerParameterType.Float) tieneVerticalSpeed = true;
                else if (p.nameHash == HashCrouching && p.type == AnimatorControllerParameterType.Bool) tieneCrouching = true;
                else if (p.nameHash == HashPanic && p.type == AnimatorControllerParameterType.Bool) tienePanic = true;
                else if (p.nameHash == HashRollDir && p.type == AnimatorControllerParameterType.Float) tieneRollDir = true;
                else if (p.nameHash == HashJump && p.type == AnimatorControllerParameterType.Trigger) tieneJump = true;
                else if (p.nameHash == HashRoll && p.type == AnimatorControllerParameterType.Trigger) tieneRoll = true;
                else if (p.nameHash == HashDodge && p.type == AnimatorControllerParameterType.Trigger) tieneDodge = true;
                else if (p.nameHash == HashHit && p.type == AnimatorControllerParameterType.Trigger) tieneHit = true;
            }
        }

        private void CachearHuesos()
        {
            if (animador == null || !animador.isHuman)
                return;

            Hips = animador.GetBoneTransform(HumanBodyBones.Hips);
            Spine = animador.GetBoneTransform(HumanBodyBones.Spine);
            Chest = animador.GetBoneTransform(HumanBodyBones.Chest);
            Head = animador.GetBoneTransform(HumanBodyBones.Head);
            RightHand = animador.GetBoneTransform(HumanBodyBones.RightHand);
            LeftHand = animador.GetBoneTransform(HumanBodyBones.LeftHand);
        }

        public void ActualizarParametros(
            float speedNormalizada,
            bool enSuelo,
            float velocidadVertical,
            bool agachado,
            bool enCombate,
            bool panico,
            bool inmediato = false)
        {
            if (animador == null)
                return;

            if (tieneSpeed)
            {
                if (inmediato || Time.deltaTime <= 0f)
                    animador.SetFloat(HashSpeed, speedNormalizada);
                else
                    animador.SetFloat(HashSpeed, speedNormalizada, 0.08f, Time.deltaTime);
            }

            if (tieneGrounded) animador.SetBool(HashGrounded, enSuelo);
            if (tieneVerticalSpeed) animador.SetFloat(HashVerticalSpeed, velocidadVertical);
            if (tieneCrouching) animador.SetBool(HashCrouching, agachado);
            if (tieneEnCombate) animador.SetBool(HashEnCombate, enCombate);
            if (tienePanic) animador.SetBool(HashPanic, panico);
        }

        public void EstablecerEnCombate(bool enCombate)
        {
            if (animador != null && tieneEnCombate)
                animador.SetBool(HashEnCombate, enCombate);
        }

        public void DispararSalto()
        {
            if (animador != null && tieneJump)
                animador.SetTrigger(HashJump);
        }

        public void DispararRodar(float direccion)
        {
            if (animador == null)
                return;

            if (tieneRollDir)
                animador.SetFloat(HashRollDir, direccion);
            if (tieneRoll)
                animador.SetTrigger(HashRoll);
        }

        public void DispararEsquiva()
        {
            if (animador != null && tieneDodge)
                animador.SetTrigger(HashDodge);
        }

        public void DispararImpacto()
        {
            if (animador != null && tieneHit)
                animador.SetTrigger(HashHit);
        }

        public void AplicarGiroRodar(float progreso, float signo)
        {
            poseActiva = true;
            float angulo = Mathf.Lerp(0f, 360f, progreso) * signo;
            rotacionExtraHips = Quaternion.AngleAxis(angulo, Vector3.forward);
            rotacionExtraEspina = Quaternion.Euler(22f * Mathf.Sin(progreso * Mathf.PI), 0f, 0f);
            offsetVerticalHips = -0.26f * Mathf.Sin(progreso * Mathf.PI);
        }

        public void AplicarInclinacionEsquiva(float progreso, Vector3 direccionLocal)
        {
            poseActiva = true;
            float onda = Mathf.Sin(progreso * Mathf.PI);
            float roll = -direccionLocal.x * 20f * onda;
            float pitch = direccionLocal.z * 14f * onda;
            rotacionExtraHips = Quaternion.Euler(pitch, 0f, roll);
            rotacionExtraEspina = Quaternion.Euler(pitch * 0.5f, 0f, roll * 0.5f);
            offsetVerticalHips = -0.09f * onda;
        }

        public void AplicarRetrocesoImpacto(float progreso)
        {
            poseActiva = true;
            float onda = Mathf.Sin(progreso * Mathf.PI);
            rotacionExtraHips = Quaternion.identity;
            rotacionExtraEspina = Quaternion.Euler(-20f * onda, 0f, 0f);
            offsetVerticalHips = -0.04f * onda;
        }

        public void AplicarAgachadoAereoOAterrizaje(float descenso)
        {
            poseActiva = true;
            rotacionExtraHips = Quaternion.identity;
            rotacionExtraEspina = Quaternion.Euler(descenso * 45f, 0f, 0f);
            offsetVerticalHips = -Mathf.Abs(descenso);
        }

        public void LimpiarPoseTemporal()
        {
            poseActiva = false;
            offsetVerticalHips = 0f;
            rotacionExtraHips = Quaternion.identity;
            rotacionExtraEspina = Quaternion.identity;
        }

        private void LateUpdate()
        {
            if (!poseActiva)
                return;

            // El Animator ya evaluó la pose del cuadro actual; aplicamos un ajuste no acumulativo.
            if (Hips != null)
            {
                if (Mathf.Abs(offsetVerticalHips) > 0.0001f)
                    Hips.position += Vector3.up * offsetVerticalHips;
                if (rotacionExtraHips != Quaternion.identity)
                    Hips.rotation = transform.rotation * rotacionExtraHips * Quaternion.Inverse(transform.rotation) * Hips.rotation;
            }

            if (Spine != null && rotacionExtraEspina != Quaternion.identity)
            {
                // Se mantiene intacto el brazo derecho para no interferir con CombateArena.PosarGolpe.
                Spine.localRotation *= rotacionExtraEspina;
            }
        }

        private void OnDisable()
        {
            LimpiarPoseTemporal();
        }
    }
}
