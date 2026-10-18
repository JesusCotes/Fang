using UnityEngine;
using UnityEngine.Events;

// Implementación genérica de IInteractable configurable desde el Inspector.
// Sirve para validar el detector de Fase 0 y como base para objetos simples
// (carteles, interruptores) sin escribir una clase nueva por cada uno.
public class SimpleInteractable : MonoBehaviour, IInteractable
{
    [Header("Prompt")]
    [SerializeField] private string promptText = "Examinar";
    [Tooltip("Si está activo, el objeto solo puede usarse una vez.")]
    [SerializeField] private bool oneShot = false;

    [Header("Respuesta")]
    [SerializeField] private UnityEvent onInteract;

    private bool used;

    public bool CanInteract(BaseCharacter character) => !(oneShot && used);

    public string GetPrompt() => promptText;

    public void Interact(BaseCharacter character)
    {
        used = true;
        Debug.Log($"[Interaction] {name}: {promptText}");
        onInteract?.Invoke();
    }
}
