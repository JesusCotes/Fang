using UnityEngine;

// Mantiene Horizontal/Vertical para los controllers originales de Player y NPC.
// Los controllers con Blend Trees reciben además orientación cardinal persistente,
// velocidad y una orientación independiente para las acciones bloqueadas.
[RequireComponent(typeof(Animator))]
public class CharacterAnimator : MonoBehaviour
{
    private const string LockedFacingTag = "LockedFacing";
    private static readonly int FacingXHash = Animator.StringToHash("FacingX");
    private static readonly int FacingYHash = Animator.StringToHash("FacingY");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int ActionFacingXHash = Animator.StringToHash("ActionFacingX");
    private static readonly int ActionFacingYHash = Animator.StringToHash("ActionFacingY");

    private Animator anim;
    private RuntimeAnimatorController cachedController;
    private bool parametersCached;
    private bool supportsDirectionalBlendTrees;
    private Vector2 facingDirection = Vector2.down;
    private Vector2 actionFacingDirection = Vector2.down;
    private float facingScaleX;
    private float blendSpeed;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        facingScaleX = Mathf.Abs(transform.localScale.x);
        RefreshControllerParameters();
    }

    private void LateUpdate()
    {
        if (anim == null) return;
        RefreshControllerParameters();
        if (!supportsDirectionalBlendTrees || anim.layerCount == 0) return;

        // Los clips derechos existentes animan scale.x=-1; los otros no siempre
        // tienen esa curva. Asignar el signo (sin multiplicarlo) evita que un hijo
        // del árbol herede el espejo anterior, sin cambiar los clips compartidos.
        Vector2 direction = anim.GetCurrentAnimatorStateInfo(0).IsTag(LockedFacingTag)
            ? actionFacingDirection : facingDirection;
        Vector3 scale = transform.localScale;
        scale.x = direction.x > 0f ? -facingScaleX : facingScaleX;
        transform.localScale = scale;
    }

    public void UpdateAnimations(Vector2 velocity = default)
    {
        if (anim == null) return;
        RefreshControllerParameters();

        bool isMoving = velocity.sqrMagnitude > 0.01f;
        Vector2 direction = isMoving ? ToCardinalDirection(velocity) : Vector2.zero;

        // Se conserva exactamente el contrato anterior para Player/NPC sin árboles.
        anim.SetFloat("Horizontal", direction.x);
        anim.SetFloat("Vertical", direction.y);

        if (isMoving) facingDirection = direction;
        // El eje dominante conserva walk=.5 y run=1 también con input diagonal.
        blendSpeed = isMoving ? Mathf.Max(Mathf.Abs(velocity.x), Mathf.Abs(velocity.y)) : 0f;
        WriteBlendTreeParameters();
    }

    public void SetFacingDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f) return;
        facingDirection = ToCardinalDirection(direction);
        blendSpeed = 0f;
        RefreshControllerParameters();
        WriteBlendTreeParameters();
    }

    public void SetActionFacingDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.001f) return;
        actionFacingDirection = ToCardinalDirection(direction);
        SetFacingDirection(actionFacingDirection);
    }

    private void RefreshControllerParameters()
    {
        if (anim == null) return;
        RuntimeAnimatorController controller = anim.runtimeAnimatorController;
        if (parametersCached && cachedController == controller) return;

        cachedController = controller;
        parametersCached = true;
        int supportedParameters = 0;
        if (controller != null)
        {
            foreach (AnimatorControllerParameter parameter in anim.parameters)
            {
                if (parameter.type != AnimatorControllerParameterType.Float) continue;
                int hash = parameter.nameHash;
                if (hash == FacingXHash || hash == FacingYHash || hash == SpeedHash ||
                    hash == ActionFacingXHash || hash == ActionFacingYHash)
                    supportedParameters++;
            }
        }
        supportsDirectionalBlendTrees = supportedParameters == 5;
        WriteBlendTreeParameters();
    }

    private void WriteBlendTreeParameters()
    {
        if (anim == null || !supportsDirectionalBlendTrees) return;
        anim.SetFloat(FacingXHash, facingDirection.x);
        anim.SetFloat(FacingYHash, facingDirection.y);
        anim.SetFloat(SpeedHash, blendSpeed);
        anim.SetFloat(ActionFacingXHash, actionFacingDirection.x);
        anim.SetFloat(ActionFacingYHash, actionFacingDirection.y);
    }

    private static Vector2 ToCardinalDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x > 0f ? Vector2.right : Vector2.left;
        return direction.y > 0f ? Vector2.up : Vector2.down;
    }
}
