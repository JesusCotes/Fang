using UnityEngine;

// Input, caminar/correr, detección de obstáculos y Roll del Player.
// BaseCharacter llama a Tick() en su Update() solo cuando currentType == Player y no está en Battle.
[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(CharacterAnimator))]
public class PlayerMovementController : MonoBehaviour
{
    // Debe coincidir con la convención de RollDirection (Int) del Animator: 0=Front, 1=Back, 2=Left, 3=Right
    private enum RollDirection { Front = 0, Back = 1, Left = 2, Right = 3 }
    private enum MovementMode { Normal, Rolling }

    private BaseCharacter baseCharacter;
    private Rigidbody2D rb;
    private Animator anim;
    private CharacterAnimator characterAnimator;
    private float speed;
    private Vector2 lastMovementDirection = Vector2.down; // Última dirección cardinal válida, usada al iniciar el Roll

    [Header("Roll")]
    [SerializeField] private KeyCode rollKey = KeyCode.LeftShift;
    [SerializeField] private float rollDuration = 0.4f;
    [SerializeField] private float rollDistance = 2f;

    private MovementMode mode = MovementMode.Normal;
    private float rollTimer;
    private Vector2 rollDirection; // Dirección congelada al iniciar el Roll; el input se ignora hasta que termine

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

    // Lee el input del jugador, decide caminar/correr, bloquea el movimiento si hay un obstáculo delante,
    // aplica la velocidad final al Rigidbody2D, y delega a TickRoll() mientras el Roll está en curso.
    public void Tick()
    {
        if (mode == MovementMode.Rolling)
        {
            TickRoll();
            return;
        }

        CharacterData data = baseCharacter.characterData;
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 input = new Vector2(h, v);

        if (input.sqrMagnitude > 0.001f)
        {
            lastMovementDirection = Mathf.Abs(input.x) > Mathf.Abs(input.y)
                ? (input.x > 0 ? Vector2.right : Vector2.left)
                : (input.y > 0 ? Vector2.up : Vector2.down);
        }

        if (Input.GetKeyDown(rollKey))
        {
            StartRoll();
            return;
        }

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

    // Congela la dirección del Roll y dispara el Animator; el input normal queda bloqueado hasta que termine.
    private void StartRoll()
    {
        mode = MovementMode.Rolling;
        rollDirection = lastMovementDirection;
        rollTimer = rollDuration;

        anim.SetInteger("RollDirection", (int)ToRollDirection(rollDirection));
        anim.SetTrigger("Roll");
    }

    // Movimiento del Roll: se compromete con rollDirection, respeta obstáculos, y vuelve a Normal cuando el timer expira.
    private void TickRoll()
    {
        CharacterData data = baseCharacter.characterData;
        float rollSpeed = rollDistance / rollDuration;

        Vector2 velocity = rollDirection * rollSpeed;
        if (HasObstacleAhead(rollDirection, data.detectionDistance, data.obstacleLayer, data.raycastOffset))
        {
            velocity = Vector2.zero;
        }
        rb.linearVelocity = velocity;

        rollTimer -= Time.deltaTime;
        if (rollTimer <= 0f)
        {
            mode = MovementMode.Normal;
            rb.linearVelocity = Vector2.zero;
        }
    }

    private RollDirection ToRollDirection(Vector2 direction)
    {
        if (direction == Vector2.up) return RollDirection.Back;
        if (direction == Vector2.down) return RollDirection.Front;
        return direction.x < 0 ? RollDirection.Left : RollDirection.Right;
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
