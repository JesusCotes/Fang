using UnityEngine;

// Contrato mínimo de todo objeto con el que el jugador puede interactuar
// (puertas, cofres, palancas, botones, carteles...).
// InteractionDetector busca este componente en los colliders dentro de su radio,
// no en la capa Obstacles, así que un interactuable nunca bloquea el movimiento.
public interface IInteractable
{
    // ¿Puede usarse ahora mismo? (puerta cerrada con llave, cofre ya abierto, palanca en uso...)
    bool CanInteract(BaseCharacter character);

    // Texto que se muestra en el prompt: "Abrir", "Leer", "Tirar", "Hablar"...
    string GetPrompt();

    // Se llama una sola vez cuando el jugador pulsa la tecla de interacción apuntando al objeto.
    void Interact(BaseCharacter character);
}
