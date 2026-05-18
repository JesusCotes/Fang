using UnityEngine;

// Asegúrate de que CharacterEnums.cs esté en tu proyecto o define los enums aquí.
// public enum CharacterType { Player, NPC }
// public enum NPCRole { Ally, Enemy, Neutral }

[CreateAssetMenu(fileName = "NewCharacterData", menuName = "RPG/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("General Info")]
    public string characterName;
    public CharacterType characterType; // Define si es un jugador o un NPC
    public NPCRole initialNPCRole; // Rol inicial si es un NPC (Aliado, Enemigo, Neutral)

    [Header("Stats")]
    public float maxHealth;
    public float attackPower;
    public float defense;
    public float speed; // Velocidad base para cálculos de turno, etc.

    [Header("Movement Settings")]
    public float walkSpeed = 0.75f;
    public float runSpeed = 1.5f;
    public float characterRadius = 0.25f; // Radio para detección de colisiones (OverlapCircle, CircleCast)
    public float detectionDistance = 0.2f; // Distancia para detección de obstáculos
    public Vector2 raycastOffset; // Offset para el origen de los raycasts
    public LayerMask obstacleLayer; // Capa de obstáculos para las detecciones físicas

    [Header("AI Settings")]
    public float minWaitTime = 1f;
    public float maxWaitTime = 4f;
    public float minPatrolDistance = 5f;
    public float patrolRadius = 20f;

    [Header("Visuals")]
    public GameObject battlePrefab; // Prefab que se usará en la escena de batalla
}