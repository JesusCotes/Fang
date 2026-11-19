using TMPro;
using UnityEngine;

// Prompt contextual de interacción. Vive en su propio Canvas (UI_Interaction),
// por encima del Canvas de UIFade, y lo controla InteractionDetector.
public class InteractionPromptUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text label;

    private static InteractionPromptUI instance;

    public static InteractionPromptUI Instance
    {
        get
        {
            if (instance == null) instance = FindAnyObjectByType<InteractionPromptUI>(FindObjectsInactive.Include);
            return instance;
        }
    }

    private void Awake()
    {
        instance = this;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        Hide();
    }

    // action = "Abrir"; key = tecla configurada en el detector, para que el prompt
    // muestre la tecla real y no una hardcodeada en la UI.
    public void Show(string action, KeyCode key = KeyCode.E)
    {
        if (label != null) label.text = $"{key}   {action}";
        SetVisible(true);
    }

    public void Hide() => SetVisible(false);

    private void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            return;
        }

        if (label != null) label.enabled = visible;
    }
}
