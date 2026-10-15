using UnityEngine;

// Input, caminar/correr, detección de obstáculos y Roll del Player.
// BaseCharacter llama a Tick() en su Update() solo cuando currentType == Player y no está en Battle.
[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(CharacterAnimator))]
public class PlayerMovementController : MonoBehaviour
{
    // Debe coincidir con la convención de RollDirection (Int) del Animator: 0=Front, 1=Back, 2=Left, 3=Right
    private enum RollDirection { Front = 0, Back = 1, Left = 2, Right = 3 }
    private enum MovementMode { Normal, Rolling, ClimbMounting, Climbing, Grabbing }

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

    [Header("Climb")]
    [SerializeField] private float climbSpeed = 3f;
    [Tooltip("Duración de link-climb-start (montarse desde arriba). Debe coincidir con la longitud del clip.")]
    [SerializeField] private float climbMountDuration = 0.4333333f;
    [Tooltip("Cuánto baja el jugador desde la cima mientras se reproduce link-climb-start.")]
    [SerializeField] private float climbMountDropDistance = 0.6f;

    private Ladder currentLadder;
    private RigidbodyType2D previousBodyType;
    private float climbMountTimer;
    private Vector2 climbMountFrom;
    private Vector2 climbMountTo;

    public bool IsClimbing => mode == MovementMode.Climbing || mode == MovementMode.ClimbMounting;

    [Header("Grab")]
    [SerializeField] private KeyCode grabKey = KeyCode.X;
    [Tooltip("Duración de link-grab-start-xxxx; el input de empujar/halar se ignora mientras dura.")]
    [SerializeField] private float grabStartDuration = 0.1f;
    [Tooltip("Alcance extra (sobre detectionDistance) para detectar el objeto a agarrar.")]
    [SerializeField] private float grabReach = 0.3f;

    private PushableObject grabbedObject;
    private Vector2 grabDirection; // Dirección cardinal hacia el objeto agarrado (+ = empujar, - = halar)
    private float grabStartTimer;

    public bool IsGrabbing => mode == MovementMode.Grabbing;

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

        if (mode == MovementMode.ClimbMounting)
        {
            TickClimbMount();
            return;
        }

        if (mode == MovementMode.Climbing)
        {
            TickClimb();
            return;
        }

        if (mode == MovementMode.Grabbing)
        {
            TickGrab();
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

        // Se evalúa antes que el obstáculo: la base/cima de la escalera suele estar pegada a un muro
        if (TryStartClimb(input))
        {
            return;
        }

        if (Input.GetKeyDown(grabKey) && TryStartGrab())
        {
            return;
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
        characterAnimator.SetActionFacingDirection(rollDirection);

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

    // Se engancha a una escalera si el input es principalmente vertical y apunta hacia el interior del tramo
    // (arriba desde la base, abajo desde la cima).
    private bool TryStartClimb(Vector2 input)
    {
        if (Mathf.Abs(input.y) < 0.001f || Mathf.Abs(input.y) <= Mathf.Abs(input.x)) return false;

        Vector2 body = GetBodyPoint();
        Ladder ladder = Ladder.FindAt(body);
        if (ladder == null) return false;

        bool goingUp = input.y > 0f;
        if (goingUp && body.y >= ladder.TopY) return false;
        if (!goingUp && body.y <= ladder.BottomY) return false;

        // Desde la mitad superior del tramo = montarse desde arriba (link-climb-start); desde abajo se engancha directo
        bool fromTop = body.y > (ladder.BottomY + ladder.TopY) * 0.5f;
        if (fromTop) StartClimbMount(ladder);
        else StartClimb(ladder);
        return true;
    }

    // Pasa a Kinematic (para atravesar el muro que cubre la escalera) y limpia los parámetros de locomoción.
    private void AttachToLadder(Ladder ladder)
    {
        currentLadder = ladder;

        previousBodyType = rb.bodyType;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;

        lastMovementDirection = Vector2.up;
        characterAnimator.SetFacingDirection(lastMovementDirection);
        anim.SetBool("Run", false);
        anim.SetFloat("Horizontal", 0f);
        anim.SetFloat("Vertical", 0f);
        anim.SetFloat("ClimbSpeed", 0f);
    }

    // Enganche desde abajo: se alinea con el centro de la escalera y empieza a escalar inmediatamente.
    private void StartClimb(Ladder ladder)
    {
        AttachToLadder(ladder);
        rb.position = new Vector2(ladder.CenterX - baseCharacter.characterData.raycastOffset.x, rb.position.y);
        EnterClimbing();
    }

    // Enganche desde arriba: se reproduce link-climb-start mientras el jugador se desliza hasta el centro
    // de la escalera y baja climbMountDropDistance desde la cima. El input se ignora hasta que termine.
    private void StartClimbMount(Ladder ladder)
    {
        mode = MovementMode.ClimbMounting;
        AttachToLadder(ladder);

        Vector2 offset = baseCharacter.characterData.raycastOffset;
        float targetBodyY = Mathf.Min(GetBodyPoint().y, ladder.TopY) - climbMountDropDistance;
        targetBodyY = Mathf.Max(targetBodyY, ladder.BottomY);

        climbMountFrom = rb.position;
        climbMountTo = new Vector2(ladder.CenterX, targetBodyY) - offset;
        climbMountTimer = 0f;

        anim.SetTrigger("ClimbStart");
    }

    private void TickClimbMount()
    {
        if (currentLadder == null || !currentLadder.isActiveAndEnabled)
        {
            EndClimb();
            return;
        }

        climbMountTimer += Time.deltaTime;
        float t = climbMountDuration > 0f ? Mathf.Clamp01(climbMountTimer / climbMountDuration) : 1f;
        rb.MovePosition(Vector2.Lerp(climbMountFrom, climbMountTo, t));

        if (t >= 1f)
        {
            rb.position = climbMountTo;
            EnterClimbing();
        }
    }

    private void EnterClimbing()
    {
        mode = MovementMode.Climbing;
        rb.linearVelocity = Vector2.zero;
        anim.SetFloat("ClimbSpeed", 0f);
        anim.SetBool("Climb", true);
    }

    // Solo movimiento vertical. La animación avanza al subir, retrocede al bajar y se congela sin input.
    // Se suelta la escalera al rebasar la cima subiendo o la base bajando.
    private void TickClimb()
    {
        if (currentLadder == null || !currentLadder.isActiveAndEnabled)
        {
            EndClimb();
            return;
        }

        float v = Input.GetAxisRaw("Vertical");
        Vector2 body = GetBodyPoint();

        if ((v > 0f && body.y >= currentLadder.TopY) || (v < 0f && body.y <= currentLadder.BottomY))
        {
            EndClimb();
            return;
        }

        rb.linearVelocity = new Vector2(0f, v * climbSpeed);
        anim.SetFloat("ClimbSpeed", v);
    }

    private void EndClimb()
    {
        mode = MovementMode.Normal;
        currentLadder = null;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = previousBodyType;

        anim.ResetTrigger("ClimbStart");
        anim.SetFloat("ClimbSpeed", 1f);
        anim.SetBool("Climb", false); // link-climb -> Idle_Back
    }

    private void OnDisable()
    {
        if (IsClimbing) EndClimb();
        if (IsGrabbing) EndGrab();
    }

    // Agarra el PushableObject que esté justo delante, en la dirección en la que mira el jugador.
    private bool TryStartGrab()
    {
        CharacterData data = baseCharacter.characterData;
        RaycastHit2D hit = Physics2D.Raycast(GetBodyPoint(), lastMovementDirection, data.detectionDistance + grabReach, data.obstacleLayer);
        if (hit.collider == null) return false;

        PushableObject pushable = hit.collider.GetComponentInParent<PushableObject>();
        if (pushable == null) return false;

        StartGrab(pushable);
        return true;
    }

    // link-grab-start-xxxx -> link-grabing-xxxx. El jugador pasa a Kinematic para moverse en bloque con el objeto.
    private void StartGrab(PushableObject pushable)
    {
        mode = MovementMode.Grabbing;
        grabbedObject = pushable;
        grabDirection = lastMovementDirection;
        grabStartTimer = grabStartDuration;
        characterAnimator.SetActionFacingDirection(grabDirection);

        previousBodyType = rb.bodyType;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;

        anim.SetBool("Run", false);
        anim.SetFloat("Horizontal", 0f);
        anim.SetFloat("Vertical", 0f);
        anim.SetInteger("GrabDirection", (int)ToRollDirection(grabDirection));
        anim.SetInteger("GrabMove", 0);
        anim.SetBool("Grab", true);
        anim.SetTrigger("GrabStart");
    }

    // Mientras se mantenga grabKey: input hacia el objeto = empujar, input contrario = halar.
    // El input perpendicular se ignora. Jugador y objeto se mueven con la misma velocidad en el eje del agarre.
    private void TickGrab()
    {
        if (grabbedObject == null || !grabbedObject.isActiveAndEnabled || !Input.GetKey(grabKey))
        {
            EndGrab();
            return;
        }

        if (grabStartTimer > 0f)
        {
            grabStartTimer -= Time.deltaTime;
            SetGrabVelocity(Vector2.zero);
            return;
        }

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        float along = Vector2.Dot(input, grabDirection);
        int move = along > 0.5f ? 1 : (along < -0.5f ? -1 : 0); // 1 = empujar, -1 = halar
        anim.SetInteger("GrabMove", move);

        Vector2 velocity = Vector2.zero;
        if (move != 0)
        {
            CharacterData data = baseCharacter.characterData;
            Vector2 direction = grabDirection * move;
            float moveSpeed = grabbedObject.MoveSpeed;
            float step = moveSpeed * Mathf.Max(Time.deltaTime, Time.fixedDeltaTime);

            // Empujar: lo bloquea el objeto. Halar: también el jugador, que va delante en la dirección del movimiento.
            bool blocked = !grabbedObject.CanMove(direction, step);
            if (!blocked && move < 0)
                blocked = HasObstacleAhead(direction, data.detectionDistance, data.obstacleLayer, data.raycastOffset);

            if (!blocked) velocity = direction * moveSpeed;
        }

        SetGrabVelocity(velocity);
    }

    private void SetGrabVelocity(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
        if (grabbedObject != null) grabbedObject.SetVelocity(velocity);
    }

    private void EndGrab()
    {
        SetGrabVelocity(Vector2.zero);
        grabbedObject = null;
        mode = MovementMode.Normal;

        rb.bodyType = previousBodyType;

        anim.ResetTrigger("GrabStart");
        anim.SetInteger("GrabMove", 0);
        anim.SetBool("Grab", false); // -> Idle_xxxx en la dirección del agarre
    }

    // Punto de referencia del cuerpo (mismo que el origen del Raycast de obstáculos)
    private Vector2 GetBodyPoint() => rb.position + baseCharacter.characterData.raycastOffset;

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
