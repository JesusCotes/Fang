using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Dibuja un campo [PuzzleSignalId] como desplegable con los canales del catálogo.
// Si no existe catálogo, o el valor guardado no está en él, cae al campo de texto
// normal en lugar de romper el Inspector.
[CustomPropertyDrawer(typeof(PuzzleSignalIdAttribute))]
public class PuzzleSignalIdDrawer : PropertyDrawer
{
    private const string EmptyLabel = "(ninguna)";

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        PuzzleSignalCatalog catalog = FindCatalog();
        if (catalog == null || catalog.Signals.Count == 0)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        var options = new List<string> { EmptyLabel };
        foreach (PuzzleSignalCatalog.Entry entry in catalog.Signals)
        {
            if (entry == null) continue;
            options.Add(string.IsNullOrEmpty(entry.id) ? "(sin id)" : entry.id);
        }

        int index = options.IndexOf(property.stringValue);

        // Un id que no está en el catálogo se muestra al final para no perderlo.
        if (index <= 0 && !string.IsNullOrEmpty(property.stringValue))
        {
            options.Add(property.stringValue);
            index = options.Count - 1;
        }
        else if (index < 0)
        {
            index = 0;
        }

        EditorGUI.BeginProperty(position, label, property);

        Rect fieldRect = EditorGUI.PrefixLabel(position, label);
        int chosen = EditorGUI.Popup(fieldRect, index, options.ToArray());

        if (chosen != index)
        {
            property.stringValue = chosen <= 0 ? string.Empty : options[chosen];
        }

        EditorGUI.EndProperty();
    }

    private static PuzzleSignalCatalog FindCatalog()
    {
        string[] guids = AssetDatabase.FindAssets("t:PuzzleSignalCatalog");
        if (guids.Length == 0) return null;

        return AssetDatabase.LoadAssetAtPath<PuzzleSignalCatalog>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }
}
