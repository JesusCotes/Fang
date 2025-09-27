using UnityEngine;

public class AreaEntrance : MonoBehaviour
{
    [SerializeField]
    private string transitionName;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {
        if (transitionName == SceneManagement.Instance.sceneTransitionName) {
            RPGMovement.Instance.transform.position = this.transform.position;
            CameraController.Instance.SetPlayerCameraFollow();
            UIFade.Instance.FadeToClear();
        }
    }
}
