using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : BaseCharacter
{
    // Este script ahora representa un NPC que puede actuar como enemigo.
    // Su rol puede cambiar, pero este script proporciona el comportamiento "enemigo".

    [Header("Configuración de Movimiento")]
    public float minMoveTime = 1f;
    public float maxMoveTime = 3f;
    public float minWaitTime = 1f; // Tiempo mínimo de espera
    public float maxWaitTime = 4f; // Tiempo máximo de espera
    public float minPatrolDistance = 5f; // Evita que los puntos salgan muy cerca
    public float patrolRadius = 20f; // Radio máximo ajustado (50 era quizás demasiado para una sola pantalla)

    [Header("Estado del Jugador")]
    public bool isPlayerNear = false;

    private NPCRole currentRole; // Rastrea el rol actual del NPC

    protected override void Awake()
    {
        base.Awake(); // Llama al Awake de BaseCharacter
        // Inicializa el rol actual desde CharacterData
        if (characterData != null)
        {
            currentRole = characterData.initialNPCRole;
        }
        agent.enabled = true; // Los enemigos suelen usar NavMeshAgent para el movimiento
        StartCoroutine(MovementRoutine());
    }

    void Update()
    {
        // Al no pasar parámetros, el método usará internamente la velocidad del agente
        UpdateAnimations(); 

        // Si GameManager no está en modo exploración, detiene la animación
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameState.Exploration)
        {
            bool isPositioning = GameManager.Instance.currentState == GameState.Battle && BattleManager.Instance.state == BattleState.Start;
            if (!isPositioning)
            {
                UpdateAnimations(Vector2.zero); // Fuerza la animación de Idle
            }
            return;
        }
    }

    // Sobrescribe UpdateAnimations para usar la velocidad del agente
    public override void UpdateAnimations(Vector2 velocity = default)
    {
        base.UpdateAnimations(agent.velocity);
    }

    public void SetNPCRole(NPCRole newRole)
    {
        currentRole = newRole;
        Debug.Log($"{characterData.characterName} cambió de rol a: {newRole}");
        // Potencialmente reiniciar MovementRoutine o cambiar el comportamiento según el nuevo rol
        // Por ejemplo, si se convierte en Aliado, podría seguir al jugador.
    }

    void FixedUpdate()
    {
        // Si el GameManager no está en modo exploración, detenemos el movimiento físico
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameState.Exploration)
        {
            bool isPositioning = GameManager.Instance.currentState == GameState.Battle && BattleManager.Instance.state == BattleState.Start;
            if (!isPositioning)
            {
                if (agent.enabled) agent.isStopped = true;
                rb.linearVelocity = Vector2.zero; // Detiene el movimiento físico
            }
            return;
        }
    }

    // Corrutina para manejar el flujo de: Moverse -> Esperar -> Moverse
    IEnumerator MovementRoutine()
    {
        while (true)
        {
            if (currentRole == NPCRole.Enemy && isPlayerNear) // Solo persigue si es un enemigo y el jugador está cerca
            {
                // Comportamiento de persecución
                if (RPGMovement.Instance != null && agent.enabled) 
                {
                    Vector2 playerPosition = RPGMovement.Instance.transform.position;
                    agent.speed = characterData.runSpeed; // Usa la velocidad de CharacterData
                    agent.SetDestination(playerPosition);
                }
                else
                {
                    // Si la instancia del jugador no está disponible, detener el movimiento
                    if (agent.enabled) agent.isStopped = true;
                }
                yield return null; // Perseguir continuamente (cada frame)
            }
            else
            {
                // Comportamiento de patrulla con NavMesh (u otro comportamiento de NPC)
                agent.speed = characterData.walkSpeed; // Usa la velocidad de CharacterData
                Vector3 patrolPoint = GetRandomPatrolPoint();
                agent.SetDestination(patrolPoint);
                isMoving = true;

                // Esperar a llegar al punto
                while (agent.enabled && (agent.pathPending || agent.remainingDistance > 0.1f))
                {
                    if (isPlayerNear) break;
                    yield return null;
                }

                // Fase 2: Detenerse y esperar
                isMoving = false;
                float randomWait = Random.Range(minWaitTime, maxWaitTime);
                yield return new WaitForSeconds(randomWait);
            }
        }
    }

    Vector3 GetRandomPatrolPoint()
    {
        int maxAttempts = 10; // Aumentamos los intentos para encontrar un lugar despejado
        for (int i = 0; i < maxAttempts; i++)
        {
            // En lugar de un círculo, elegimos una de las 4 direcciones cardinales
            Vector2[] cardinalDirections = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
            Vector2 chosenDir = cardinalDirections[Random.Range(0, cardinalDirections.Length)];
            
            // Elegimos una distancia aleatoria dentro del radio de patrulla
            float distance = Random.Range(minPatrolDistance, patrolRadius); // Usa el patrolRadius local
            Vector3 targetPos = transform.position + (Vector3)(chosenDir * distance);

            NavMeshHit hit;
            // 1. Buscamos el punto más cercano en el NavMesh respecto a nuestro punto cardinal
            if (NavMesh.SamplePosition(targetPos, out hit, 1.0f, NavMesh.AllAreas))
            {
                // 2. Verificamos si el camino hacia ese punto es alcanzable y completo
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    // 3. Verificación física: ¿Hay un collider en ese punto exacto?
                    // Usamos characterRadius para simular el espacio que ocupa el enemigo
                    Collider2D overlap = Physics2D.OverlapCircle(hit.position, characterData.characterRadius, characterData.obstacleLayer);

                    if (overlap == null)
                    {
                        Debug.Log($"Punto de patrulla válido y despejado encontrado en: {hit.position}");
                        return hit.position;
                    }
                    
                    Debug.LogWarning($"Punto {hit.position} rechazado: Obstruido por {overlap.name}");
                }
            }
        }

        // Si tras agotar los intentos no hay un punto válido, se queda quieto
        return transform.position;
    }
    
    // --- TRIGGER PARA DETECTAR AL JUGADOR ---
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (currentRole == NPCRole.Enemy && collision.CompareTag("Player")) // Solo reacciona si es un enemigo
        {
            isPlayerNear = true;
            StopAllCoroutines(); // Detiene la patrulla/persecución inmediatamente
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EnterBattle(this);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (currentRole == NPCRole.Enemy && collision.CompareTag("Player")) // Solo reacciona si es un enemigo
        {
            isPlayerNear = false;
            Debug.Log("El jugador se ha alejado.");
        }
    }
}