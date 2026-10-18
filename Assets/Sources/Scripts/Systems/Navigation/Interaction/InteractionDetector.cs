using UnityEngine;

// Detecta objetos IInteractable alrededor del jugador, muestra el prompt, resalta
// el objetivo y lo ejecuta con una tecla.
// A diferencia de PlayerMovementController.HasObstacleAhead(), aquí la detección es
// por radio alrededor del cuerpo, así que funciona con muros, objetos grandes y
// objetos que no deben bloquear el paso (todos ellos en la capa Interactable).
[RequireComponent(typeof(BaseCharacter))]
public class InteractionDetector : MonoBehaviour
{
    [Header("Detección")]
    [Tooltip("Radio de búsqueda alrededor del origen.")]
    [SerializeField] private float range = 1.1f;
    [Tooltip("Offset del origen respecto al pivote del jugador. Link usa (0, 0.7), el mismo de su collider.")]
    [SerializeField] private Vector2 originOffset = new Vector2(0f, 0.7f);
    [Tooltip("Capas donde viven los objetos interactuables. Debe incluir la capa 'Interactable'.")]
    [SerializeField] private LayerMask interactableLayers;
    [Tooltip("Producto punto mínimo entre la dirección al objeto y la orientación del jugador para considerarlo 'enfrente'.")]
    [SerializeField] private float facingThreshold = 0.35f;

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Bloqueo")]
    [Tooltip("Tiempo sin control tras interactuar: feedback y margen para que el objeto responda.")]
    [SerializeField] private float inputLockDuration = 0.2f;

    [Header("Resaltado")]
    [SerializeField] private Color highlightColor = new Color(1f, 0.94f, 0.6f, 1f);

    private BaseCharacter character;
    private PlayerMovementController movement;
    private CharacterAnimator characterAnimator;

    private IInteractable current;
    private SpriteRenderer currentRenderer;
    private Color currentBaseColor;
    private float lockTimer;

    public IInteractable Current => current;

    private void Awake()
    {
        character = GetComponent<BaseCharacter>();
        movement = GetComponent<PlayerMovementController>();
        characterAnimator = GetComponent<CharacterAnimator>();
    }

    private void OnDisable()
    {
        ClearTarget();
        if (movement != null) movement.SetInputLocked(false);
        lockTimer = 0f;
    }

    private void Update()
    {
        if (character == null || character.CurrentType != CharacterType.Player) return;

        TickLock();

        // Durante Roll/Climb/Grab no se busca objetivo ni se muestra prompt:
        // el jugador ya está comprometido con otra acción.
        if (movement != null && movement.IsBusy)
        {
            ClearTarget();
            return;
        }

        IInteractable found = FindBest();
        if (!ReferenceEquals(found, current)) SetTarget(found);

        if (current != null && lockTimer <= 0f && Input.GetKeyDown(interactKey)) Execute();
    }

    // Devuelve el interactuable más cercano que esté enfrente y disponible, o null.
    private IInteractable FindBest()
    {
        Vector2 origin = GetOrigin();
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range, interactableLayers);
        Vector2 facing = characterAnimator != null ? characterAnimator.FacingDirection : Vector2.down;

        IInteractable best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            IInteractable candidate = hits[i].GetComponentInParent<IInteractable>();
            if (candidate == null) continue;

            MonoBehaviour behaviour = candidate as MonoBehaviour;
            if (behaviour == null || !behaviour.isActiveAndEnabled) continue;
            if (!candidate.CanInteract(character)) continue;

            // ClosestPoint en vez de transform.position: los objetos grandes (puertas,
            // portones) tienen el pivote lejos del punto donde está el jugador.
            Vector2 closest = hits[i].ClosestPoint(origin);
            Vector2 delta = closest - origin;
            if (delta.sqrMagnitude > 0.0001f && Vector2.Dot(delta.normalized, facing) < facingThreshold) continue;

            float distance = delta.magnitude;
            if (distance >= bestDistance) continue; // también descarta colliders repetidos del mismo objeto

            bestDistance = distance;
            best = candidate;
        }

        return best;
    }

    private void SetTarget(IInteractable target)
    {
        RestoreHighlight();
        current = target;

        if (current == null)
        {
            InteractionPromptUI prompt = InteractionPromptUI.Instance;
            if (prompt != null) prompt.Hide();
            return;
        }

        MonoBehaviour behaviour = current as MonoBehaviour;
        currentRenderer = behaviour != null ? behaviour.GetComponentInChildren<SpriteRenderer>() : null;
        if (currentRenderer != null)
        {
            currentBaseColor = currentRenderer.color;
            currentRenderer.color = highlightColor;
        }

        InteractionPromptUI ui = InteractionPromptUI.Instance;
        if (ui != null) ui.Show(current.GetPrompt(), interactKey);
    }

    private void ClearTarget() => SetTarget(null);

    private void RestoreHighlight()
    {
        if (currentRenderer != null) currentRenderer.color = currentBaseColor;
        currentRenderer = null;
    }

    private void Execute()
    {
        IInteractable target = current;
        if (target == null) return;

        if (movement != null)
        {
            movement.SetInputLocked(true);
            lockTimer = inputLockDuration;
        }

        target.Interact(character);

        // Si el objeto se destruyó o desactivó (cofre consumido, puerta usada),
        // se limpia el objetivo sin esperar al siguiente frame.
        if (target is Object unityObject && unityObject == null) ClearTarget();
    }

    private void TickLock()
    {
        if (lockTimer <= 0f) return;

        lockTimer -= Time.deltaTime;
        if (lockTimer <= 0f && movement != null) movement.SetInputLocked(false);
    }

    private Vector2 GetOrigin() => (Vector2)transform.position + originOffset;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 0.9f, 0.9f);
        Gizmos.DrawWireSphere(GetOrigin(), range);
    }
}
