using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum BattleState { Start, PlayerTurn, EnemyTurn, Busy, Won, Lost }

public class BattleManager : Singleton<BattleManager>
{
    public BattleState state;

    [Header("Participantes de la Batalla")]
    public List<BaseCharacter> playerParty = new List<BaseCharacter>();
    public List<BaseCharacter> enemyParty = new List<BaseCharacter>();

    [Header("Datos de Batalla")]
    private CharacterData currentEnemyData; // Ahora usa CharacterData

    [SerializeField]
    private GameObject battlepointPrefab; // Prefab para visualizar los puntos de batalla
    private List<BattleUnit> playerUnits = new List<BattleUnit>();
    private List<BattleUnit> enemyUnits = new List<BattleUnit>();

    [Header("Configuración de Colisiones")]
    public LayerMask obstacleLayer;
    public float characterRadius = 0.3f; // Define qué tan "ancho" es el personaje para ver si cabe

    [SerializeField] 
    private float battleSpacing = 3f; // Distancia prudente desde el centro para cada personaje

    public void SetupBattle(BaseCharacter worldEnemy) // Ahora acepta BaseCharacter
    {
        state = BattleState.Start;

        // Limpiamos listas previas
        playerParty.Clear();
        enemyParty.Clear();

        // Añadimos a los participantes iniciales
        playerParty.Add(BaseCharacter.PlayerInstance);
        enemyParty.Add(worldEnemy);

        // Sincronizamos el estado de los personajes para que "sepan" que han entrado en combate
        foreach (var character in playerParty) character.characterState = CharacterState.Battle;
        foreach (var character in enemyParty) character.characterState = CharacterState.Battle;

        currentEnemyData = worldEnemy.characterData; // Usa characterData
        StartCoroutine(SetupRoutine());
    }

    private IEnumerator SetupRoutine()
    {
        // 1. Calcular punto central del encuentro
        BaseCharacter playerCharacter = playerParty[0];
        BaseCharacter enemyCharacter = enemyParty[0];

        Vector3 playerPos = playerCharacter.transform.position;
        Vector3 enemyPos = enemyCharacter.transform.position;
        Vector3 centerPoint = (playerPos + enemyPos) / 2f;
        CharacterData playerData = playerCharacter.characterData; // Obtiene el CharacterData del jugador

        // 1.1 Calcular la dirección natural del encuentro para permitir ejes verticales o diagonales
        Vector3 dirToPlayer = (playerPos - centerPoint).normalized;
        // Fallback de seguridad por si las posiciones coinciden exactamente
        if (dirToPlayer.sqrMagnitude < 0.001f) dirToPlayer = Vector3.left;
        Vector3 dirToEnemy = -dirToPlayer;

        Debug.DrawRay(centerPoint, Vector3.up * 0.5f, Color.red, 2f); // Visualiza el punto central
        Debug.Log($"Punto central calculado en: {centerPoint}");

        // 2. Calcular posiciones seguras (que no estén dentro de paredes)
        Vector3 playerTargetPos = GetSafePosition(centerPoint, dirToPlayer, battleSpacing);
        Debug.DrawRay(playerTargetPos, Vector3.up * 0.5f, Color.green, 2f); // Visualiza la posición del jugador
        Vector3 enemyTargetPos = GetSafePosition(centerPoint, dirToEnemy, battleSpacing);
        try
        {
            Instantiate(battlepointPrefab, playerTargetPos, Quaternion.identity); // Visualiza el punto de batalla del jugador
            Instantiate(battlepointPrefab, enemyTargetPos, Quaternion.identity); // Visualiza el punto de batalla del enemigo
        }
        catch (System.Exception ex)
        {
            Debug.Log($"Error al instanciar los puntos de batalla: {ex.Message}"); 
        }

        Debug.DrawRay(enemyTargetPos, Vector3.up * 0.5f, Color.blue, 2f); // Visualiza la posición del enemigo
        Debug.Log($"Posición segura para el jugador: {playerTargetPos}");
        Debug.Log($"Posición segura para el enemigo: {enemyTargetPos}");
        
        // 3. Caminata usando NavMeshAgent hacia las posiciones
        // Activamos temporalmente el agente del jugador para el posicionamiento
        playerCharacter.agent.enabled = true;
        playerCharacter.agent.updateRotation = false; // Evitamos que el agente rote el objeto en 2D
        playerCharacter.agent.speed = playerCharacter.characterData.walkSpeed;
        playerCharacter.agent.acceleration = 1000f; // Aceleración casi instantánea
        playerCharacter.agent.autoBraking = false;  // Evita que desacelere al llegar
        playerCharacter.agent.SetDestination(playerTargetPos);

        // Configuramos el agente del enemigo para el destino de batalla
        enemyCharacter.agent.updateRotation = false; // Evitamos que el agente rote el objeto en 2D
        enemyCharacter.agent.speed = enemyCharacter.characterData.walkSpeed;
        enemyCharacter.agent.acceleration = 1000f; // Aceleración casi instantánea
        enemyCharacter.agent.autoBraking = false;  // Evita que desacelere al llegar
        enemyCharacter.agent.SetDestination(enemyTargetPos);
        if (enemyCharacter.agent.isStopped) enemyCharacter.agent.isStopped = false;

        // Ajustar la cámara para que enfoque a ambos personajes
        if (CameraController.Instance != null) {
            CameraController.Instance.SetBattleCamera(playerParty, enemyParty);
        }

        float timeout = 3f; // Aumentado para dar tiempo a llegar
        float timer = 0f;
        bool arrived = false;

        while (timer < timeout && !arrived)
        {
            timer += Time.deltaTime;
            
            // Comprobamos si ambos agentes han llegado a sus destinos
            bool pArrived = !playerCharacter.agent.pathPending && playerCharacter.agent.remainingDistance <= playerCharacter.agent.stoppingDistance + 0.1f;
            bool eArrived = !enemyCharacter.agent.pathPending && enemyCharacter.agent.remainingDistance <= enemyCharacter.agent.stoppingDistance + 0.1f;

            if (pArrived && eArrived) arrived = true;
            yield return null;
        }

        // Detenemos y deshabilitamos los agentes para tomar control manual de la orientación
        playerCharacter.agent.isStopped = true;
        playerCharacter.agent.enabled = false;
        
        enemyCharacter.agent.isStopped = true;
        enemyCharacter.agent.enabled = false;

        // 3.5 Orientar personajes para que se miren
        // Usamos las direcciones ideales calculadas al inicio (dirToEnemy y dirToPlayer) para asegurar una alineación perfecta
        Vector2 playerFacingDir = new Vector2(dirToEnemy.x, dirToEnemy.y);
        Vector2 enemyFacingDir = new Vector2(dirToPlayer.x, dirToPlayer.y);

        playerCharacter.UpdateAnimations(playerFacingDir);
        enemyCharacter.UpdateAnimations(enemyFacingDir);

        // 4. Inicializar BattleUnits para todos los participantes
        playerUnits.Clear();
        foreach (var character in playerParty)
        {
            BattleUnit unit = character.gameObject.GetComponent<BattleUnit>() ?? character.gameObject.AddComponent<BattleUnit>();
            unit.SetStats(character.characterData);
            playerUnits.Add(unit);
        }

        enemyUnits.Clear();
        foreach (var character in enemyParty)
        {
            BattleUnit unit = character.gameObject.GetComponent<BattleUnit>() ?? character.gameObject.AddComponent<BattleUnit>();
            unit.SetStats(character.characterData);
            enemyUnits.Add(unit);
        }

        Debug.Log($"Combate iniciado en el entorno: {currentEnemyData.characterName}");

        yield return new WaitForSeconds(0.1f);

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

        // Si choca con algo Y la distancia es mayor a 0 (para no chocar con nosotros mismos al empezar)
        if (hit.collider != null && hit.distance > 0.01f)
        {
            // El centroid es la posición donde el centro del círculo choca con el muro
            return (Vector3)hit.centroid;
        }

        return center + direction * distance;
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
        
        // Por ahora el enemigo siempre ataca al primer miembro del grupo (el jugador)
        if (playerUnits.Count > 0)
            playerUnits[0].TakeDamage(currentEnemyData.attackPower);
            
        yield return new WaitForSeconds(1f);
        PlayerTurn();
    }
}