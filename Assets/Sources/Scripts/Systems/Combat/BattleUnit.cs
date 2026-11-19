using UnityEngine;

public class BattleUnit : MonoBehaviour
{
    public CharacterData characterData; // Ahora usa CharacterData
    public float currentHealth;

    public void SetStats(CharacterData newStats)
    {
        characterData = newStats;
        currentHealth = characterData.maxHealth;
    }

    public void TakeDamage(float damage)
    {
        // Fórmula simple: Daño - Defensa (mínimo 1)
        float finalDamage = Mathf.Max(1, damage - characterData.defense);
        currentHealth -= finalDamage;
        
        Debug.Log($"{characterData.characterName} recibe {finalDamage} de daño. HP: {currentHealth}");

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Debug.Log($"{characterData.characterName} ha sido derrotado.");
        }
    }
}