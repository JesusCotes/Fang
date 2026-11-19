using System.Collections.Generic;
using UnityEngine;

// Escalera vertical estilo Minish Cap. El BoxCollider2D (trigger) define el tramo escalable:
// bounds.min.y = punto de salida inferior, bounds.max.y = punto de salida superior.
// PlayerMovementController consulta FindAt() para engancharse; la escalera no mueve al jugador por sí misma.
[RequireComponent(typeof(BoxCollider2D))]
public class Ladder : MonoBehaviour
{
    private static readonly List<Ladder> activeLadders = new List<Ladder>();

    [Tooltip("Distancia vertical extra fuera del collider desde la que el jugador puede engancharse (arriba y abajo).")]
    [SerializeField] private float entryMargin = 0.35f;
    [Tooltip("Tolerancia horizontal extra para engancharse sin estar perfectamente alineado.")]
    [SerializeField] private float horizontalTolerance = 0.15f;

    private BoxCollider2D area;

    public Bounds Area => area.bounds;
    public float CenterX => area.bounds.center.x;
    public float BottomY => area.bounds.min.y;
    public float TopY => area.bounds.max.y;

    private void Awake()
    {
        area = GetComponent<BoxCollider2D>();
        area.isTrigger = true; // Nunca debe bloquear al jugador
    }

    private void OnEnable() => activeLadders.Add(this);
    private void OnDisable() => activeLadders.Remove(this);

    // Devuelve la escalera cuyo tramo (ampliado por entryMargin) contiene el punto, o null.
    public static Ladder FindAt(Vector2 point)
    {
        for (int i = 0; i < activeLadders.Count; i++)
        {
            if (activeLadders[i].Contains(point)) return activeLadders[i];
        }
        return null;
    }

    private bool Contains(Vector2 point)
    {
        Bounds b = Area;
        return Mathf.Abs(point.x - b.center.x) <= b.extents.x + horizontalTolerance
            && point.y >= b.min.y - entryMargin
            && point.y <= b.max.y + entryMargin;
    }

    private void OnDrawGizmos()
    {
        BoxCollider2D box = area != null ? area : GetComponent<BoxCollider2D>();
        if (box == null) return;
        Bounds b = box.bounds;
        Gizmos.color = new Color(1f, 0.8f, 0.1f, 0.8f);
        Gizmos.DrawWireCube(b.center, b.size);
        Gizmos.color = new Color(1f, 0.8f, 0.1f, 0.3f);
        Gizmos.DrawWireCube(b.center, b.size + new Vector3(horizontalTolerance * 2f, entryMargin * 2f, 0f));
    }
}
