using UnityEngine;

public enum GameState
{
    Exploration, // Navegación libre
    Battle,      // Modo combate
    Dialogue,    // Hablando con NPCs
    Menu,        // Inventario
    Paused       // Estado de pausa (tiempo detenido)
}

public class GameManager : Singleton<GameManager>
{
    [Header("Estado del Juego")]
    public GameState currentState;

    private void Start()
    {
        ChangeState(GameState.Exploration);
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;
        
        switch (currentState)
        {
            case GameState.Exploration:
                Time.timeScale = 1f;
                // Habilitar controles de movimiento
                break;
            case GameState.Battle:
                // Aquí podrías pausar el movimiento del jugador
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                break;
        }
        Debug.Log($"Estado del juego cambiado a: {newState}");
    }

    // Método puente para iniciar el combate desde el EnemyAI
    public void EnterBattle(BaseCharacter enemy) // Ahora acepta BaseCharacter
    {
        if (currentState == GameState.Battle) return;
        
        ChangeState(GameState.Battle);
        
        // Enviar datos al BattleManager para iniciar la lógica
        BattleManager.Instance.SetupBattle(enemy); 
    }
}