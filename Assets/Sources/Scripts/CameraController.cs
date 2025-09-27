using Unity.Cinemachine;
using UnityEngine;

public class CameraController : Singleton<CameraController>
{
    private CinemachineVirtualCameraBase cinemachineVirtualCamera;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void SetPlayerCameraFollow() {
        cinemachineVirtualCamera = FindAnyObjectByType<CinemachineVirtualCameraBase>();
        cinemachineVirtualCamera.Follow = RPGMovement.Instance.transform;               
    }
}
