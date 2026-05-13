using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(NavMeshAgent))]
public abstract class BaseCharacter : MonoBehaviour
{
    [Header("Base Components")]
    public Rigidbody2D rb;
    public Animator anim;
    public NavMeshAgent agent;

    [Header("Base Settings")]
    public CharacterData characterData;

    protected bool isMoving; // Usado principalmente por la IA para rastrear estado

    protected virtual void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (anim == null) anim = GetComponent<Animator>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }
    }

    public virtual void UpdateAnimations(Vector2 velocity = default)
    {
        if (anim == null) return;

        if (velocity.sqrMagnitude > 0.01f)
        {
            if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y))
            {
                anim.SetFloat("Horizontal", velocity.x > 0 ? 1 : -1);
                anim.SetFloat("Vertical", 0);
            }
            else
            {
                anim.SetFloat("Horizontal", 0);
                anim.SetFloat("Vertical", velocity.y > 0 ? 1 : -1);
            }
        }
        else
        {
            anim.SetFloat("Horizontal", 0);
            anim.SetFloat("Vertical", 0);
        }
    }

    protected bool HasObstacleAhead(Vector2 direction, float distance, float radius, LayerMask mask, Vector2 offset)
    {
        Vector2 origin = (Vector2)transform.position + offset;
        RaycastHit2D hit = Physics2D.CircleCast(origin, radius, direction, distance, mask);
        return hit.collider != null;
    }
}