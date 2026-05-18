using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterStats", menuName = "RPG/CharacterStats")]
public class CharacterStats : ScriptableObject
{
    public string characterName;
    public float maxHealth;
    public float currentHealth;
    public float attackPower;
    public float defense;
    public float speed;

    [Header("Visuals")]
    public GameObject battlePrefab; // Prefab que se usará en la escena de batalla
}