using UnityEngine;

// Puerta activada por una señal de puzle.
// Al abrirse desactiva su collider (para que Link pueda pasar) y desliza el visual.
// No necesita saber quién la activa: escucha un signalId del PuzzleSignalBus.
public class Door : MonoBehaviour
{
    [Header("Señal")]
    [Tooltip("Canal del puzle que escucha esta puerta.")]
    [PuzzleSignalId]
    [SerializeField] private string signalId = "puerta";
    [Tooltip("Si se abre una vez, ya no se vuelve a cerrar. Necesario para puzles donde el peso se retira después.")]
    [SerializeField] private bool openOnce = true;

    [Header("Piezas")]
    [SerializeField] private Collider2D blockingCollider;
    [Tooltip("Transform que se desliza al abrir. Si se deja vacío se usa el de esta puerta.")]
    [SerializeField] private Transform visual;
    [SerializeField] private Vector2 openOffset = new Vector2(0f, -2f);

    [Header("Animación")]
    [SerializeField] private float openDuration = 0.35f;

    private Vector3 closedPosition;
    private float progress;
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (blockingCollider == null) blockingCollider = GetComponent<Collider2D>();
        if (visual == null) visual = transform;

        closedPosition = visual.localPosition;
    }

    private void OnEnable()
    {
        PuzzleSignalBus.Subscribe(signalId, OnSignalChanged);

        // Si la placa ya estaba pulsada antes de que esta puerta existiera, arranca abierta.
        ApplySignal(PuzzleSignalBus.GetSignal(signalId), true);
    }

    private void OnDisable()
    {
        PuzzleSignalBus.Unsubscribe(signalId, OnSignalChanged);
    }

    private void OnSignalChanged(bool value)
    {
        ApplySignal(value, false);
    }

    private void ApplySignal(bool open, bool instant)
    {
        if (openOnce && isOpen) return;

        isOpen = open;

        if (blockingCollider != null) blockingCollider.enabled = !open;

        if (instant) progress = open ? 1f : 0f;
    }

    private void Update()
    {
        float target = isOpen ? 1f : 0f;
        if (Mathf.Approximately(progress, target)) return;

        progress = Mathf.MoveTowards(progress, target, Time.deltaTime / Mathf.Max(0.01f, openDuration));
        visual.localPosition = closedPosition + (Vector3)(openOffset * progress);
    }
}
