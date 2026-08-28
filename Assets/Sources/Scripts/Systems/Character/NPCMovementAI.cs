using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// IA de patrulla/persecución para NPCs: persigue al jugador si es Enemy y lo tiene cerca,
// o patrulla puntos aleatorios y espera entre viajes. BaseCharacter arranca/detiene esto
// desde SetCharacterType con StartAI()/StopAI().
[RequireComponent(typeof(NavMeshAgent))]
public class NPCMovementAI : MonoBehaviour
{
    private BaseCharacter baseCharacter;
    private NavMeshAgent agent;
    private Coroutine routine;
    private bool isPlayerNear; // Se activa/desactiva desde EnemyCombatTrigger
    private bool isMoving;

    private void Awake()
    {
        baseCharacter = GetComponent<BaseCharacter>();
        agent = GetComponent<NavMeshAgent>();
    }

    public void SetPlayerNear(bool near) => isPlayerNear = near;

    public void StartAI()
    {
        StopAI();
        routine = StartCoroutine(MovementRoutine());
    }

    public void StopAI()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        isMoving = false;
    }

    private IEnumerator MovementRoutine()
    {
        CharacterData data = baseCharacter.characterData;

        while (true)
        {
            // Si el personaje entra en batalla, pausamos su IA de mundo abierto
            if (baseCharacter.characterState == CharacterState.Battle)
            {
                isMoving = false;
                yield return new WaitForSeconds(1f);
                continue;
            }

            if (baseCharacter.currentRole == NPCRole.Enemy && isPlayerNear && BaseCharacter.PlayerInstance != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                // Modo persecución: enemigo con el jugador dentro de su trigger
                agent.speed = data.runSpeed;
                agent.SetDestination(BaseCharacter.PlayerInstance.transform.position);
                yield return null;
            }
            else if (agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                // Modo patrulla: elige un punto aleatorio y camina hacia él
                agent.speed = data.walkSpeed;
                Vector3 patrolPoint = GetRandomPatrolPoint(data);
                agent.SetDestination(patrolPoint);
                isMoving = true;

                while (agent.enabled && (agent.pathPending || agent.remainingDistance > 0.1f))
                {
                    if (baseCharacter.currentRole == NPCRole.Enemy && isPlayerNear) break; // Interrumpe la patrulla para perseguir
                    if (baseCharacter.characterState == CharacterState.Battle) break;
                    yield return null;
                }

                isMoving = false;
                yield return new WaitForSeconds(Random.Range(data.minWaitTime, data.maxWaitTime)); // Pausa antes del próximo punto de patrulla
            }
            else
            {
                yield return null; // El agent aún no está listo (desactivado o fuera del NavMesh)
            }
        }
    }

    // Busca un punto de patrulla válido: dirección cardinal aleatoria + distancia aleatoria,
    // validado contra el NavMesh y libre de obstáculos físicos.
    private Vector3 GetRandomPatrolPoint(CharacterData data)
    {
        Vector2[] cardinalDirections = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        Vector2 chosenDir = cardinalDirections[Random.Range(0, cardinalDirections.Length)];
        float distance = Random.Range(data.minPatrolDistance, data.patrolRadius);
        Vector3 targetPos = transform.position + (Vector3)(chosenDir * distance);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPos, out hit, 1.0f, NavMesh.AllAreas))
        {
            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                if (Physics2D.OverlapCircle(hit.position, data.characterRadius, data.obstacleLayer) == null)
                    return hit.position; // Punto válido: alcanzable por NavMesh y sin obstáculos
            }
        }
        return transform.position; // Fallback: se queda quieto si no encontró un punto válido
    }
}
