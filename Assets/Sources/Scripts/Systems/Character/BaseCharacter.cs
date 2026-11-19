using UnityEngine;
using UnityEngine.AI;

// Orquestador universal para todo character (Player y NPC): mismos componentes en todos los prefabs,
// solo varían characterData, sprites y Animator Controller. Centraliza identidad, salud, estado de
// combate y decide qué otro componente (Player/NPC) está activo según SetCharacterType.
[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(NavMeshAgent))]
[RequireComponent(typeof(CharacterAnimator), typeof(PlayerMovementController), typeof(NPCMovementAI))]
[RequireComponent(typeof(EnemyCombatTrigger))]
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
    public CharacterType CurrentType { get; private set; }
    public float currentHealth;

    private CharacterAnimator characterAnimator;
    private PlayerMovementController playerMovement;
    private NPCMovementAI npcMovementAI;

    protected virtual void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (anim == null) anim = GetComponent<Animator>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        characterAnimator = GetComponent<CharacterAnimator>();
        playerMovement = GetComponent<PlayerMovementController>();
        npcMovementAI = GetComponent<NPCMovementAI>();

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
        CurrentType = newType;

        if (CurrentType == CharacterType.Player)
        {
            PlayerInstance = this;
            if (agent != null) agent.enabled = false; // El Player no usa pathfinding, se mueve por input
            npcMovementAI.StopAI();
            playerMovement.enabled = true;
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
            playerMovement.enabled = false;
            npcMovementAI.StartAI(); // Arranca la IA de patrulla/persecución

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
                characterAnimator.UpdateAnimations(agent.velocity);
                return;
            }

            // En combate por turnos (estático), detenemos movimiento e input manual
            characterAnimator.UpdateAnimations(Vector2.zero);
            if (agent.isActiveAndEnabled && agent.isOnNavMesh) agent.isStopped = true;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Lógica de Exploración (Solo si no está en combate)
        if (CurrentType == CharacterType.Player)
            playerMovement.Tick(); // Input del teclado -> velocidad del Rigidbody2D
        else if (agent.isActiveAndEnabled)
            characterAnimator.UpdateAnimations(agent.velocity); // NPC: la animación sigue la velocidad calculada por el NavMeshAgent
        else
            characterAnimator.UpdateAnimations(Vector2.zero);
    }

    public void SetNPCRole(NPCRole newRole)
    {
        currentRole = newRole;
        Debug.Log($"{characterData.characterName} ahora es {newRole}");
    }

    // Passthrough para que otros sistemas (ej. BattleManager) animen sin conocer CharacterAnimator directamente
    public void UpdateAnimations(Vector2 velocity = default) => characterAnimator.UpdateAnimations(velocity);
}