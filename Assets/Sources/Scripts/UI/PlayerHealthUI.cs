using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Slider healthSlider;

    private void Update()
    {
        // Verificamos que exista una instancia del jugador y que el slider esté asignado
        if (BaseCharacter.PlayerInstance != null && healthSlider != null)
        {
            // Sincronizamos los valores máximo y actual
            // El máximo viene del CharacterData, la actual de la instancia
            healthSlider.maxValue = BaseCharacter.PlayerInstance.characterData.maxHealth;
            healthSlider.value = BaseCharacter.PlayerInstance.currentHealth;
        }
        else if (healthSlider != null)
        {
            // Si no hay jugador (por ejemplo en pantallas de carga), la barra se vacía
            healthSlider.value = 0;
        }
    }
}