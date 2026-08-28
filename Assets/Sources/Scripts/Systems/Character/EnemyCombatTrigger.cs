using UnityEngine;

// Detecta cuándo el jugador entra/sale del rango de un NPC Enemy: activa la persecución
// de NPCMovementAI y dispara el inicio de combate en GameManager.
public class EnemyCombatTrigger : MonoBehaviour
{
    private BaseCharacter baseCharacter;
    private NPCMovementAI npcMovementAI;

    private void Awake()
    {
        baseCharacter = GetComponent<BaseCharacter>();
        npcMovementAI = GetComponent<NPCMovementAI>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Solo los NPCs de tipo Enemigo reaccionan a la colisión con el jugador
        if (baseCharacter.characterData.characterType == CharacterType.NPC && baseCharacter.currentRole == NPCRole.Enemy && collision.CompareTag("Player"))
        {
            npcMovementAI.SetPlayerNear(true);
            if (GameManager.Instance != null) GameManager.Instance.EnterBattle(baseCharacter); // Dispara el inicio de combate
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (baseCharacter.characterData.characterType == CharacterType.NPC && baseCharacter.currentRole == NPCRole.Enemy && collision.CompareTag("Player"))
        {
            npcMovementAI.SetPlayerNear(false); // El enemigo deja de perseguir y vuelve a patrullar
        }
    }
}
