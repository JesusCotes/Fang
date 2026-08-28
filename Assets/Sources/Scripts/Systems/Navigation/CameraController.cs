using Unity.Cinemachine;
using UnityEngine;
using System.Collections.Generic;

public class CameraController : Singleton<CameraController>
{
    private CinemachineCamera cinemachineCamera;
    private CinemachineTargetGroup targetGroup;

    [Header("Configuración de Batalla")]
    [SerializeField] private float battleCameraRadius = 2f; // Aumenta este valor para alejar más la cámara

    private void EnsureCameraReference()
    {
        if (cinemachineCamera == null)
        {
            cinemachineCamera = FindAnyObjectByType<CinemachineCamera>();
        }
    }

    public void SetPlayerCameraFollow() {
        EnsureCameraReference();
        if (cinemachineCamera == null) {
            Debug.LogError("[CameraController] Error: No se encontró ningún componente 'CinemachineCamera' en la escena.");
            return;
        }

        Debug.Log("[CameraController] Cámara encontrada en el objeto: " + cinemachineCamera.gameObject.name);

        if (BaseCharacter.PlayerInstance != null) {
            cinemachineCamera.Follow = BaseCharacter.PlayerInstance.transform;
            Debug.Log("[CameraController] Seguimiento ('Follow') asignado con éxito a: " + BaseCharacter.PlayerInstance.gameObject.name);
            
            // Al volver al modo jugador, desactivamos el TargetGroup para que no interfiera
            if (targetGroup != null) targetGroup.gameObject.SetActive(false);
        } else {
            Debug.LogWarning("[CameraController] Advertencia: BaseCharacter.PlayerInstance es nulo. El personaje podría no estar inicializado como 'Player'.");
        }
    }

    public void SetBattleCamera(List<BaseCharacter> players, List<BaseCharacter> enemies)
    {
        EnsureCameraReference();
        if (cinemachineCamera == null) return;

        if (targetGroup == null)
        {
            targetGroup = FindAnyObjectByType<CinemachineTargetGroup>();
            if (targetGroup == null)
            {
                GameObject tgObj = new GameObject("BattleTargetGroup");
                targetGroup = tgObj.AddComponent<CinemachineTargetGroup>();
            }
        }

        targetGroup.gameObject.SetActive(true);
        // Limpiamos objetivos anteriores y añadimos los nuevos
        targetGroup.Targets.Clear();

        foreach (var character in players)
        {
            if (character != null) AddTargetToGroup(character.transform);
        }

        foreach (var character in enemies)
        {
            if (character != null) AddTargetToGroup(character.transform);
        }

        // Hacemos que la cámara siga al grupo (que estará en el centro de ambos)
        cinemachineCamera.Follow = targetGroup.transform;
        Debug.Log("[CameraController] Cámara enfocando al grupo de batalla.");
    }

    private void AddTargetToGroup(Transform t)
    {
        targetGroup.Targets.Add(new CinemachineTargetGroup.Target 
        { 
            Object = t, 
            Weight = 1f, 
            Radius = battleCameraRadius 
        });
    }
}
