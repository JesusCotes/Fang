using System;
using System.Collections.Generic;
using UnityEngine;

// Catálogo de canales de puzle del juego.
//
// No se consulta en runtime: los componentes siguen guardando su signalId como string.
// Existe para que el campo signalId se dibuje como desplegable (ver PuzzleSignalIdDrawer)
// y para que haya un único sitio donde ver todos los canales con su descripción.
//
// Convención de id: zona-funcion, en minúsculas y con guiones. El prefijo de zona evita
// colisiones entre escenas y permite encontrarlos con grep.
[CreateAssetMenu(fileName = "PuzzleSignals", menuName = "Fang/Puzzle Signal Catalog")]
public class PuzzleSignalCatalog : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("Identificador del canal. Convención: zona-funcion.")]
        public string id;

        [Tooltip("Qué lo dispara y qué abre o cierra.")]
        public string description;
    }

    [SerializeField] private List<Entry> signals = new List<Entry>();

    public IReadOnlyList<Entry> Signals => signals;

    public bool Contains(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;

        for (int i = 0; i < signals.Count; i++)
        {
            if (signals[i] != null && signals[i].id == id) return true;
        }

        return false;
    }
}
