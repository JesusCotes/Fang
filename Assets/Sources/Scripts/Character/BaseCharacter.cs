using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(NavMeshAgent))]
public class BaseCharacter : MonoBehaviour
{
    [Header("Base Components")]
    public Rigidbody2D rb;
    public Animator anim;
    public NavMeshAgent agent;

    [Header("Base Settings")]
    public CharacterData characterData;
    public NPCRole currentRole;

    public static BaseCharacter PlayerInstance { get; protected set; }
    private CharacterType currentType;
    private Coroutine npcRoutine;

    protected bool isMoving; // Usado principalmente por la IA para rastrear estado
    protected bool isPlayerNear = false;
    protected float speed;

    protected virtual void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (anim == null) anim = GetComponent<Animator>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }
    }

    protected virtual void Start()
    {
        if (characterData != null) InitCharacter();
    }

    public void InitCharacter()
    {
        currentRole = characterData.initialNPCRole;
        SetCharacterType(characterData.characterType);
    }

    public void SetCharacterType(CharacterType newType)
    {
        currentType = newType;
        
        if (currentType == CharacterType.Player)
        {
            PlayerInstance = this;
            if (agent != null) agent.enabled = false;
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
            npcRoutine = StartCoroutine(MovementRoutine());

            // Marcar Freeze Position en X y Y para el NPC
            if (rb != null)
            {
                rb.constraints |= (RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY);
            }
        }
    }

    protected virtual void Update()
    {
        if (characterData == null) return;

        // Si GameManager no está en modo exploración, detenemos todo
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameState.Exploration)
        {
            bool isPositioning = GameManager.Instance.currentState == GameState.Battle && BattleManager.Instance.state == BattleState.Start;
            if (!isPositioning)
            {
                UpdateAnimations(Vector2.zero);
                if (agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = true;
                rb.linearVelocity = Vector2.zero;
                return;
            }
        }

        if (currentType == CharacterType.Player)
        {
            HandlePlayerInput();
        }
        else
        {
            // Solo usamos la velocidad del agente si está activo
            if (agent.isActiveAndEnabled)
                UpdateAnimations(agent.velocity);
            else
                UpdateAnimations(Vector2.zero);
        }
    }

    private void HandlePlayerInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 input = new Vector2(h, v);

        if (Input.GetKey("z") || Input.GetButton("Fire1"))
            speed = characterData.runSpeed;
        else
        {
            input *= 0.5f;
            speed = characterData.walkSpeed;
        }

        if (input.sqrMagnitude > 0.001f && HasObstacleAhead(input.normalized, characterData.detectionDistance, characterData.characterRadius, characterData.obstacleLayer, characterData.raycastOffset))
        {
            input = Vector2.zero;
        }

        UpdateAnimations(input);
        rb.linearVelocity = input * speed;
    }

    private IEnumerator MovementRoutine()
    {
        while (currentType == CharacterType.NPC)
        {
            if (currentRole == NPCRole.Enemy && isPlayerNear && PlayerInstance != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.speed = characterData.runSpeed;
                agent.SetDestination(PlayerInstance.transform.position);
                yield return null;
            }
            else if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.speed = characterData.walkSpeed;
                Vector3 patrolPoint = GetRandomPatrolPoint();
                agent.SetDestination(patrolPoint);
                isMoving = true;

                while (agent.enabled && (agent.pathPending || agent.remainingDistance > 0.1f))
                {
                    if (currentRole == NPCRole.Enemy && isPlayerNear) break;
                    yield return null;
                }

                isMoving = false;
                yield return new WaitForSeconds(Random.Range(characterData.minWaitTime, characterData.maxWaitTime));
            }
        }
    }

    public void SetNPCRole(NPCRole newRole)
    {
        currentRole = newRole;
        Debug.Log($"{characterData.characterName} ahora es {newRole}");
    }

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
                    return hit.position;
            }
        }
        return transform.position;
    }

    protected bool HasObstacleAhead(Vector2 direction, float distance, float radius, LayerMask mask, Vector2 offset)
    {
        Vector2 origin = (Vector2)transform.position + offset;
        RaycastHit2D hit = Physics2D.CircleCast(origin, radius, direction, distance, mask);
        return hit.collider != null;
    }

    // La lógica de detección de triggers se mueve a BaseCharacter
    // para que EnemyAI pueda ser una clase vacía.
    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        // Solo los NPCs de tipo Enemigo reaccionan a la colisión con el jugador
        if (characterData.characterType == CharacterType.NPC && currentRole == NPCRole.Enemy && collision.CompareTag("Player"))
        {
            isPlayerNear = true;
            if (GameManager.Instance != null) GameManager.Instance.EnterBattle(this);
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D collision)
    {
        if (characterData.characterType == CharacterType.NPC && currentRole == NPCRole.Enemy && collision.CompareTag("Player"))
        {
            isPlayerNear = false;
        }
    }
}