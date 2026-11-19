using UnityEngine;

// Objeto que el jugador puede agarrar para empujar o halar (estilo Minish Cap).
// Es Kinematic: el jugador no puede desplazarlo chocando contra él; solo se mueve mientras está agarrado
// y PlayerMovementController le asigna velocidad sobre el eje en que fue agarrado.
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PushableObject : MonoBehaviour
{
    [Tooltip("Capas que bloquean el desplazamiento del objeto (muros, otros objetos empujables).")]
    [SerializeField] private LayerMask blockingLayers;
    [Tooltip("Velocidad al empujar/halar. El jugador se mueve a la misma velocidad.")]
    [SerializeField] private float moveSpeed = 2f;
    [Tooltip("Margen extra del cast para no quedar incrustado en los muros.")]
    [SerializeField] private float skinWidth = 0.02f;

    private Rigidbody2D rb;
    private ContactFilter2D blockingFilter;
    private readonly RaycastHit2D[] castResults = new RaycastHit2D[4];

    public float MoveSpeed => moveSpeed;

    private void Reset()
    {
        blockingLayers = LayerMask.GetMask("Obstacles", "World");
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.linearVelocity = Vector2.zero;

        blockingFilter = new ContactFilter2D { useTriggers = false };
        blockingFilter.SetLayerMask(blockingLayers);
    }

    // Comprueba con los colliders del propio objeto si puede avanzar 'distance' en 'direction' (se ignora a sí mismo).
    public bool CanMove(Vector2 direction, float distance)
    {
        return rb.Cast(direction, blockingFilter, castResults, distance + skinWidth) == 0;
    }

    public void SetVelocity(Vector2 velocity) => rb.linearVelocity = velocity;
}
