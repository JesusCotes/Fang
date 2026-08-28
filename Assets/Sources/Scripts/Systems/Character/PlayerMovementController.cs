using UnityEngine;

// Input, caminar/correr y detección de obstáculos del Player.
// BaseCharacter llama a Tick() en su Update() solo cuando currentType == Player y no está en Battle.
[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(CharacterAnimator))]
public class PlayerMovementController : MonoBehaviour
{
    private BaseCharacter baseCharacter;
    private Rigidbody2D rb;
    private Animator anim;
    private CharacterAnimator characterAnimator;
    private float speed;

    [Header("Debug")]
    public bool showObstacleDetectionGizmo = true; // Dibuja el Raycast de HasObstacleAhead en la vista Scene
    private Vector2 lastObstacleCheckOrigin;
    private Vector2 lastObstacleCheckDirection;
    private float lastObstacleCheckDistance;
    private bool lastObstacleCheckHit;

    private void Awake()
    {
        baseCharacter = GetComponent<BaseCharacter>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        characterAnimator = GetComponent<CharacterAnimator>();
    }

    // Lee el input del jugador, decide caminar/correr, bloquea el movimiento si hay un obstáculo delante
    // y aplica la velocidad final al Rigidbody2D.
    public void Tick()
    {
        CharacterData data = baseCharacter.characterData;
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 input = new Vector2(h, v);

        if (Input.GetKey("z") || Input.GetButton("Fire1"))
        {
            speed = data.runSpeed;
            anim.SetBool("Run", true);
        }
        else
        {
            input *= 0.5f; // Al caminar se reduce la magnitud del input (movimiento más suave que correr)
            speed = data.walkSpeed;
            anim.SetBool("Run", false);
        }

        // Detección de obstáculos: si hay algo en la dirección del movimiento, se cancela el input de este frame
        if (input.sqrMagnitude > 0.001f && HasObstacleAhead(input.normalized, data.detectionDistance, data.obstacleLayer, data.raycastOffset))
        {
            input = Vector2.zero;
        }

        characterAnimator.UpdateAnimations(input);
        rb.linearVelocity = input * speed;
    }

    // Origen fijo (transform.position + raycastOffset en espacio de mundo, sin rotar/flipear con la orientación),
    // por eso el punto de detección no sigue automáticamente hacia dónde mira el personaje.
    private bool HasObstacleAhead(Vector2 direction, float distance, LayerMask mask, Vector2 offset)
    {
        Vector2 origin = (Vector2)transform.position + offset;
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, mask);

        // Guarda los datos del último cast para dibujarlo en OnDrawGizmos
        lastObstacleCheckOrigin = origin;
        lastObstacleCheckDirection = direction;
        lastObstacleCheckDistance = distance;
        lastObstacleCheckHit = hit.collider != null;

        Debug.DrawRay(origin, direction * distance, lastObstacleCheckHit ? Color.red : Color.green);

        return hit.collider != null;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showObstacleDetectionGizmo || lastObstacleCheckDistance <= 0f) return;

        Gizmos.color = lastObstacleCheckHit ? Color.red : Color.green;
        Vector2 endPoint = lastObstacleCheckOrigin + lastObstacleCheckDirection * lastObstacleCheckDistance;
        Gizmos.DrawLine(lastObstacleCheckOrigin, endPoint);
    }
}
