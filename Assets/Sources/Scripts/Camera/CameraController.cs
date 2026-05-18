using Unity.Cinemachine;
using UnityEngine;

public class CameraController : Singleton<CameraController>
{
    private CinemachineCamera cinemachineCamera;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void SetPlayerCameraFollow() {
        Debug.Log("[CameraController] Intentando asignar seguimiento de cámara...");

        // Buscamos específicamente el componente CinemachineCamera (estándar en CM3)
        cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();

        if (cinemachineCamera == null) {
            Debug.LogError("[CameraController] Error: No se encontró ningún componente 'CinemachineCamera' en la escena.");
            return;
        }

        Debug.Log("[CameraController] Cámara encontrada en el objeto: " + cinemachineCamera.gameObject.name);

        if (BaseCharacter.PlayerInstance != null) {
            cinemachineCamera.Follow = BaseCharacter.PlayerInstance.transform;
            Debug.Log("[CameraController] Seguimiento ('Follow') asignado con éxito a: " + BaseCharacter.PlayerInstance.gameObject.name);
        } else {
            Debug.LogWarning("[CameraController] Advertencia: BaseCharacter.PlayerInstance es nulo. El personaje podría no estar inicializado como 'Player'.");
        }
    }
}
