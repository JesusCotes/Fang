using UnityEngine;

public class AreaEntrance : MonoBehaviour
{
    [SerializeField]
    private string transitionName;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {
        if (SceneManagement.Instance != null && transitionName == SceneManagement.Instance.sceneTransitionName) {
            if (BaseCharacter.PlayerInstance != null) {
                BaseCharacter.PlayerInstance.transform.position = this.transform.position;
            }
            
            if (CameraController.Instance != null) CameraController.Instance.SetPlayerCameraFollow();
            if (UIFade.Instance != null) UIFade.Instance.FadeToClear();
        }
    }
}
