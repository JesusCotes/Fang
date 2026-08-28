using UnityEngine;

// Traduce un vector de velocidad/input a los parámetros Horizontal/Vertical del Animator
// (elige el eje dominante para animaciones de 4 direcciones tipo top-down).
// Componente universal: todos los characters (Player y NPC) lo usan igual.
[RequireComponent(typeof(Animator))]
public class CharacterAnimator : MonoBehaviour
{
    private Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void UpdateAnimations(Vector2 velocity = default)
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
}
