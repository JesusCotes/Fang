using UnityEngine;
using UnityEngine.AI;
using System.Collections;

// Clase base para Player y NPC: centraliza movimiento, animación e IA de patrulla/combate.
// Player y Enemy (u otras clases) heredan de aquí y solo agregan su lógica específica.
[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(NavMeshAgent))]
public class BaseCharacter : MonoBehaviour
{
    [Header("Base Components")]
    public Rigidbody2D rb;
    public Animator anim;
    public NavMeshAgent agent; // Solo se usa para mover NPCs; el Player se mueve por Rigidbody2D

    [Header("Base Settings")]
    public CharacterData characterData; // ScriptableObject con stats y parámetros de movimiento/IA
    public NPCRole currentRole;
    public CharacterState characterState = CharacterState.Exploration; // Exploration = mundo abierto, Battle = combate por turnos

    // Referencia global al jugador activo, accesible desde cualquier NPC (ej. para perseguirlo)
    public static BaseCharacter PlayerInstance { get; protected set; }
    private CharacterType currentType;
    private Coroutine npcRoutine; // Corrutina de IA de patrulla/persecución, solo corre en NPCs
    public float currentHealth;

    protected bool isMoving; // Usado principalmente por la IA para rastrear estado
    protected bool isPlayerNear = false; // Se activa/desactiva con los triggers 2D de abajo
    protected float speed;

    [Header("Debug")]
    public bool showObstacleDetectionGizmo = true; // Dibuja el Raycast de HasObstacleAhead en la vista Scene
    private Vector2 lastObstacleCheckOrigin;
    private Vector2 lastObstacleCheckDirection;
    private float lastObstacleCheckDistance;
    private bool lastObstacleCheckHit;

    protected virtual void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (anim == null) anim = GetComponent<Animator>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            // Se desactivan porque el juego es 2D top-down: la rotación/eje Y del agent no aplican
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }
    }

    protected virtual void Start()
    {
        if (characterData != null) InitCharacter();
    }

    // Punto de entrada para inicializar el personaje a partir de su CharacterData
    public void InitCharacter()
    {
        currentRole = characterData.initialNPCRole;
        currentHealth = characterData.maxHealth;
        SetCharacterType(characterData.characterType); // Decide si se configura como Player o NPC
    }

    // Cambia el comportamiento del personaje entre Player (input manual) y NPC (IA por NavMeshAgent)
    public void SetCharacterType(CharacterType newType)
    {
        currentType = newType;
        
        if (currentType == CharacterType.Player)
        {
            PlayerInstance = this;
            if (agent != null) agent.enabled = false; // El Player no usa pathfinding, se mueve por input
            if (npcRoutine != null) StopCoroutine(npcRoutine);
            gameObject.tag = "Player";

            // Desmarcar Freeze Position en X y Y para el jugador
            if (rb != null)
            {
                rb.constraints &= ~(RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY);
            }

            // Notificamos a la cámara que hay un nuevo objetivo
            if (CameraController.Instance != null) CameraController.Instance.SetPlayerCameraFollow();
        }
        else
        {
            if (PlayerInstance == this) PlayerInstance = null;
            if (agent != null) agent.enabled = true;
            gameObject.tag = "Untagged"; // O el tag que uses para NPCs
            if (npcRoutine != null) StopCoroutine(npcRoutine);
            npcRoutine = StartCoroutine(MovementRoutine()); // Arranca la IA de patrulla/persecución

            // Marcar Freeze Position en X y Y para el NPC (el Rigidbody2D no debe moverlo, lo mueve el agent)
            if (rb != null)
            {
                rb.constraints |= (RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY);
            }
        }
    }

    protected virtual void Update()
    {
        if (characterData == null) return;

        // Lógica específica para cuando el personaje sabe que está en combate
        if (characterState == CharacterState.Battle)
        {
            // Mientras el BattleManager está posicionando personajes, se permite seguir animando el movimiento del agent
            bool isPositioning = BattleManager.Instance != null && BattleManager.Instance.state == BattleState.Start;
            
            if (isPositioning)
            {
                UpdateAnimations(agent.velocity);
                return;
            }
            
            // En combate por turnos (estático), detenemos movimiento e input manual
            UpdateAnimations(Vector2.zero);
            if (agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = true;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Lógica de Exploración (Solo si no está en combate)
        if (currentType == CharacterType.Player)
            HandlePlayerInput(); // Input del teclado -> velocidad del Rigidbody2D
        else if (agent.isActiveAndEnabled)
            UpdateAnimations(agent.velocity); // NPC: la animación sigue la velocidad calculada por el NavMeshAgent
        else
            UpdateAnimations(Vector2.zero);
    }

    // Lee el input del jugador, decide caminar/correr, bloquea el movimiento si hay un obstáculo delante
    // y aplica la velocidad final al Rigidbody2D.
    private void HandlePlayerInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 input = new Vector2(h, v);

        if (Input.GetKey("z") || Input.GetButton("Fire1")) {
            speed = characterData.runSpeed;
            anim.SetBool("Run", true);
        }

        else
        {
            input *= 0.5f; // Al caminar se reduce la magnitud del input (movimiento más suave que correr)
            speed = characterData.walkSpeed;
            anim.SetBool("Run", false);
        }

        // Detección de obstáculos: si hay algo en la dirección del movimiento, se cancela el input de este frame
        if (input.sqrMagnitude > 0.001f && HasObstacleAhead(input.normalized, characterData.detectionDistance, characterData.obstacleLayer, characterData.raycastOffset))
        {
            input = Vector2.zero;
        }

        UpdateAnimations(input);
        rb.linearVelocity = input * speed;
    }

    // IA de los NPC: persigue al jugador si es Enemy y lo tiene cerca, o patrulla puntos aleatorios y espera entre viajes.
    // Se detiene automáticamente al entrar en combate y se reanuda al salir.
    private IEnumerator MovementRoutine()
    {
        while (currentType == CharacterType.NPC)
        {
            // Si el personaje entra en batalla, pausamos su IA de mundo abierto
            if (characterState == CharacterState.Battle)
            {
                isMoving = false;
                yield return new WaitForSeconds(1f);
                continue;
            }

            if (currentRole == NPCRole.Enemy && isPlayerNear && PlayerInstance != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                // Modo persecución: enemigo con el jugador dentro de su trigger
                agent.speed = characterData.runSpeed;
                agent.SetDestination(PlayerInstance.transform.position);
                yield return null;
            }
            else if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                // Modo patrulla: elige un punto aleatorio y camina hacia él
                agent.speed = characterData.walkSpeed;
                Vector3 patrolPoint = GetRandomPatrolPoint();
                agent.SetDestination(patrolPoint);
                isMoving = true;

                while (agent.enabled && (agent.pathPending || agent.remainingDistance > 0.1f))
                {
                    if (currentRole == NPCRole.Enemy && isPlayerNear) break; // Interrumpe la patrulla para perseguir
                    if (characterState == CharacterState.Battle) break;
                    yield return null;
                }

                isMoving = false;
                yield return new WaitForSeconds(Random.Range(characterData.minWaitTime, characterData.maxWaitTime)); // Pausa antes del próximo punto de patrulla
            }
        }
    }

    public void SetNPCRole(NPCRole newRole)
    {
        currentRole = newRole;
        Debug.Log($"{characterData.characterName} ahora es {newRole}");
    }

    // Traduce un vector de velocidad/input a los parámetros Horizontal/Vertical del Animator
    // (elige el eje dominante para animaciones de 4 direcciones tipo top-down).
    public virtual void UpdateAnimations(Vector2 velocity = default)
    {
        if (anim == null) return;

        if (velocity.sqrMagnitude > 0.01f)
        {
            if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y))
            {
                anim.SetFloat("Horizontal", velocity.x > 0 ? 1 : -1);
                anim.SetFloat("Vertical", 0);
            }
            else
            {
                anim.SetFloat("Horizontal", 0);
                anim.SetFloat("Vertical", velocity.y > 0 ? 1 : -1);
            }
        }
        else
        {
            anim.SetFloat("Horizontal", 0);
            anim.SetFloat("Vertical", 0);
        }
    }

    // Busca un punto de patrulla válido: dirección cardinal aleatoria + distancia aleatoria,
    // validado contra el NavMesh y libre de obstáculos físicos.
    private Vector3 GetRandomPatrolPoint()
    {
        Vector2[] cardinalDirections = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        Vector2 chosenDir = cardinalDirections[Random.Range(0, cardinalDirections.Length)];
        float distance = Random.Range(characterData.minPatrolDistance, characterData.patrolRadius);
        Vector3 targetPos = transform.position + (Vector3)(chosenDir * distance);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, 1.0f, NavMesh.AllAreas))
        {
            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                if (Physics2D.OverlapCircle(hit.position, characterData.characterRadius, characterData.obstacleLayer) == null)
                    return hit.position; // Punto válido: alcanzable por NavMesh y sin obstáculos
            }
        }
        return transform.position; // Fallback: se queda quieto si no encontró un punto válido
    }

    // Origen fijo (transform.position + raycastOffset en espacio de mundo, sin rotar/flipear con la orientación),
    // por eso el punto de detección no sigue automáticamente hacia dónde mira el personaje.
    protected bool HasObstacleAhead(Vector2 direction, float distance, LayerMask mask, Vector2 offset)
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

    // La lógica de detección de triggers se mueve a BaseCharacter
    // para que EnemyAI pueda ser una clase vacía.
    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        // Solo los NPCs de tipo Enemigo reaccionan a la colisión con el jugador
        if (characterData.characterType == CharacterType.NPC && currentRole == NPCRole.Enemy && collision.CompareTag("Player"))
        {
            isPlayerNear = true;
            if (GameManager.Instance != null) GameManager.Instance.EnterBattle(this); // Dispara el inicio de combate
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D collision)
    {
        if (characterData.characterType == CharacterType.NPC && currentRole == NPCRole.Enemy && collision.CompareTag("Player"))
        {
            isPlayerNear = false; // El enemigo deja de perseguir y vuelve a patrullar
        }
    }
}