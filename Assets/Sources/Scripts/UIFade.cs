using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIFade : Singleton<UIFade>
{
    [SerializeField] private Image fadeScreen;
    [SerializeField] private float fadeSpeed;

    private IEnumerator fadeRoutine;

    public void FadeToBlack() {
        if (fadeRoutine != null) {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = FadeRoutine(1);
        StartCoroutine(fadeRoutine); 
    }

        public void FadeToClear() {
        if (fadeRoutine != null) {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = FadeRoutine(0);
        StartCoroutine(fadeRoutine); 
    }

    private IEnumerator FadeRoutine(float targetAlpfha) {
        while (!Mathf.Approximately(fadeScreen.color.a, targetAlpfha)) {
            float alpha = Mathf.MoveTowards(fadeScreen.color.a, targetAlpfha, fadeSpeed * Time.deltaTime);
            fadeScreen.color = new Color(fadeScreen.color.r, fadeScreen.color.g, fadeScreen.color.b, alpha);
            yield return null;
        }
    }
}
