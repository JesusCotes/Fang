using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum BattleState { Start, PlayerTurn, EnemyTurn, Busy, Won, Lost }

public class BattleManager : Singleton<BattleManager>
{
    public BattleState state;

    [Header("Datos de Batalla")]
    private CharacterData currentEnemyData; // Ahora usa CharacterData

    private BattleUnit playerUnit;
    private BattleUnit enemyUnit;
    private BaseCharacter activeWorldEnemy; // Ahora referencia a BaseCharacter

    [Header("Configuración de Colisiones")]
    public LayerMask obstacleLayer;
    public float characterRadius = 0.3f; // Define qué tan "ancho" es el personaje para ver si cabe

    public void SetupBattle(BaseCharacter worldEnemy) // Ahora acepta BaseCharacter
    {
        state = BattleState.Start;
        activeWorldEnemy = worldEnemy;
        currentEnemyData = worldEnemy.characterData; // Usa characterData
        StartCoroutine(SetupRoutine());
    }

    private IEnumerator SetupRoutine()
    {
        // 1. Calcular punto central del encuentro
        Vector3 playerPos = BaseCharacter.PlayerInstance.transform.position;
        Vector3 enemyPos = activeWorldEnemy.transform.position;
        Vector3 centerPoint = (playerPos + enemyPos) / 2f;
        CharacterData playerData = BaseCharacter.PlayerInstance.characterData; // Obtiene el CharacterData del jugador

        // 2. Calcular posiciones seguras (que no estén dentro de paredes)
        Vector3 playerTargetPos = GetSafePosition(centerPoint, Vector3.left, 1.5f);
        Vector3 enemyTargetPos = GetSafePosition(centerPoint, Vector3.right, 1.5f);

        // 3. Caminata física hacia las posiciones
        float moveSpeed = 3f;
        float timeout = 2f; // Seguridad por si se traban con algo
        float timer = 0f;

        while (timer < timeout)
        {
            timer += Time.deltaTime;
            bool playerArrived = MoveTowardsTarget(BaseCharacter.PlayerInstance.rb, BaseCharacter.PlayerInstance, playerTargetPos, moveSpeed); // Pasa BaseCharacter
            bool enemyArrived = MoveTowardsTarget(activeWorldEnemy.rb, activeWorldEnemy, enemyTargetPos, moveSpeed); // Pasa BaseCharacter

            if (playerArrived && enemyArrived) break;
            yield return new WaitForFixedUpdate();
        }
        StopUnit(BaseCharacter.PlayerInstance.rb, BaseCharacter.PlayerInstance);
        StopUnit(activeWorldEnemy.rb, activeWorldEnemy);

        // 4. Añadimos el componente de batalla a los objetos ya existentes
        playerUnit = BaseCharacter.PlayerInstance.gameObject.GetComponent<BattleUnit>() ?? BaseCharacter.PlayerInstance.gameObject.AddComponent<BattleUnit>();
        playerUnit.SetStats(BaseCharacter.PlayerInstance.characterData); // Usa characterData

        enemyUnit = activeWorldEnemy.gameObject.GetComponent<BattleUnit>() ?? activeWorldEnemy.gameObject.AddComponent<BattleUnit>();
        enemyUnit.SetStats(currentEnemyData); // Usa characterData

        Debug.Log($"Combate iniciado en el entorno: {currentEnemyData.characterName}");

        yield return new WaitForSeconds(1f);

        // 5. Determinar turno por Velocidad
        if (playerData.speed >= currentEnemyData.speed) // Usa playerData
        {
            PlayerTurn();
        }
        else
        {
            StartCoroutine(EnemyTurnRoutine());
        }
    }

    private Vector3 GetSafePosition(Vector3 center, Vector3 direction, float distance)
    {
        // CircleCast simula el volumen del personaje moviéndose en una dirección
        RaycastHit2D hit = Physics2D.CircleCast(center, characterRadius, direction, distance, obstacleLayer);

        if (hit.collider != null)
        {
            // El centroid es la posición donde el centro del círculo choca con el muro
            return (Vector3)hit.centroid;
        }

        return center + direction * distance;
    }

    private bool MoveTowardsTarget(Rigidbody2D rb, BaseCharacter character, Vector3 target, float speed) // Ahora acepta BaseCharacter
    {
        float distance = Vector3.Distance(rb.transform.position, target);
        if (distance < 0.1f) 
        {
            rb.linearVelocity = Vector2.zero;
            return true;
        }
        Vector2 direction = (target - rb.transform.position).normalized;
        rb.linearVelocity = direction * speed;

        character.UpdateAnimations(direction); // Usa el método UpdateAnimations del personaje
        
        return false;
    }

    private void StopUnit(Rigidbody2D rb, BaseCharacter character) // Ahora acepta BaseCharacter
    {
        rb.linearVelocity = Vector2.zero; // Detiene el movimiento físico
        character.UpdateAnimations(Vector2.zero); // Fuerza la animación de Idle
    }

    void PlayerTurn()
    {
        state = BattleState.PlayerTurn;
        Debug.Log("Esperando acción del jugador...");
        // Aquí activarías tu Menú de Batalla (Atacar, Magia, etc.)
    }

    public void OnAttackButton() // Ejemplo de llamada desde UI
    {
        if (state != BattleState.PlayerTurn) return;
        // Lógica de ataque al enemigo...
        StartCoroutine(EnemyTurnRoutine());
    }

    private IEnumerator EnemyTurnRoutine()
    {
        state = BattleState.EnemyTurn;
        yield return new WaitForSeconds(1f);
        Debug.Log($"{currentEnemyData.characterName} ataca!"); // Usa currentEnemyData
        playerUnit.TakeDamage(currentEnemyData.attackPower); // Usa currentEnemyData
        yield return new WaitForSeconds(1f);
        PlayerTurn();
    }
}