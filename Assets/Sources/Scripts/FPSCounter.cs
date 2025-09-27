using UnityEngine;
using UnityEngine.UI;

public class FPSCounter : MonoBehaviour
{
    void awake()
    {
        Application.targetFrameRate = 60;
    }

    Text text;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        text = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        float fps = 1.0f / Time.deltaTime;
        if (text != null)
        {
            text.text = "FPS: " + Mathf.RoundToInt(fps).ToString();
        }
    }
}
