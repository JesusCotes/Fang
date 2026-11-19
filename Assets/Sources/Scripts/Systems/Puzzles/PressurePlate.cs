using UnityEngine;

// Placa de presión: emite una señal de puzle mientras algo descansa encima.
//
// Detecta por solape de física consultado en Update, no por OnTriggerStay2D. La caja
// empujable es Kinematic y se mueve por velocidad, y así no dependemos de callbacks de
// física con cuerpos kinematic ni de que la placa tenga collider.
//
// Cuenta como "peso" cualquier PushableObject, y opcionalmente cualquier BaseCharacter
// (para que Link también pueda mantener una placa pisándola).
[RequireComponent(typeof(Transform))]
public class PressurePlate : MonoBehaviour
{
    [Header("Señal")]
    [Tooltip("Canal del puzle que emite esta placa.")]
    [PuzzleSignalId]
    [SerializeField] private string signalId = "placa";

    [Header("Detección")]
    [Tooltip("Tamaño del área de detección en unidades de mundo.")]
    [SerializeField] private Vector2 detectionSize = new Vector2(1.1f, 1.1f);
    [Tooltip("Desplazamiento del área respecto al pivote. Sube el área para alcanzar el collider del personaje, que va sobre los pies.")]
    [SerializeField] private Vector2 detectionOffset = new Vector2(0f, 0.35f);
    [Tooltip("Capas que se consultan: Obstacles para la caja, Default para Link.")]
    [SerializeField] private LayerMask detectionMask;
    [Tooltip("Si Link de pie sobre la placa también la activa.")]
    [SerializeField] private bool acceptsCharacters = true;

    [Header("Feedback")]
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private Color pressedColor = new Color(1f, 0.85f, 0.35f, 1f);
    [SerializeField] private Vector2 pressedOffset = new Vector2(0f, -0.05f);

    private ContactFilter2D filter;
    private readonly Collider2D[] hits = new Collider2D[8];

    private bool isPressed;
    private Color baseColor = Color.white;
    private Vector3 baseVisualPosition;

    public bool IsPressed => isPressed;

    private void Reset()
    {
        detectionMask = LayerMask.GetMask("Obstacles", "Default");
    }

    private void Awake()
    {
        if (visual == null) visual = GetComponentInChildren<SpriteRenderer>();

        if (visual != null)
        {
            baseColor = visual.color;
            baseVisualPosition = visual.transform.localPosition;
        }

        filter = new ContactFilter2D { useTriggers = false, useLayerMask = true };
        filter.SetLayerMask(detectionMask);
    }

    private void Start()
    {
        isPressed = IsOccupied();
        ApplyVisual();
        PuzzleSignalBus.SetSignal(signalId, isPressed);
    }

    private void Update()
    {
        bool occupied = IsOccupied();
        if (occupied == isPressed) return;

        isPressed = occupied;
        ApplyVisual();
        PuzzleSignalBus.SetSignal(signalId, isPressed);
    }

    private bool IsOccupied()
    {
        Vector2 center = (Vector2)transform.position + detectionOffset;
        int count = Physics2D.OverlapBox(center, detectionSize, 0f, filter, hits);

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null) continue;

            // Los colliders propios (si la placa llegara a tenerlos) no cuentan.
            if (hit.transform.IsChildOf(transform)) continue;

            if (hit.GetComponentInParent<PushableObject>() != null) return true;

            if (acceptsCharacters && hit.GetComponentInParent<BaseCharacter>() != null) return true;
        }

        return false;
    }

    private void ApplyVisual()
    {
        if (visual == null) return;

        visual.color = isPressed ? pressedColor : baseColor;
        visual.transform.localPosition = baseVisualPosition + (isPressed ? (Vector3)pressedOffset : Vector3.zero);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isPressed ? Color.green : Color.cyan;
        Gizmos.DrawWireCube((Vector2)transform.position + detectionOffset, detectionSize);
    }
}
