using UnityEngine;

public class SceneManagement : Singleton<SceneManagement>
{
    public string sceneTransitionName {get; private set;}

    public void SetTrancitionName(string sceneTransitionName) {
        this.sceneTransitionName = sceneTransitionName;
    }
}
