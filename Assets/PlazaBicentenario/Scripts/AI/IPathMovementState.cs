// Adaptado e integrado para SupervivenciaEnLaPaz a partir de IPathMovementState.cs de SanAndreasUnity.
using SupervivenciaEnLaPaz.Core;

namespace SupervivenciaEnLaPaz.AI
{
    public interface IPathMovementState : IState
    {
        PathMovementData PathMovementData { get; }
    }
}
