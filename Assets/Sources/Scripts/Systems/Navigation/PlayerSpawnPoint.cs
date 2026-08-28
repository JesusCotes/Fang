using UnityEngine;
using System.Collections;

public class PlayerSpawnPoint : MonoBehaviour
{
    [Header("Configuración de Spawn")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private bool spawnIfMissing = true;

    private void Awake()
    {
        // Usamos una Corrutina para esperar un frame y asegurar que el sistema de 
        // SceneManagement y otros Singletons se hayan inicializado correctamente.
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        // Esperamos al final del frame para que los Start() de otros scripts hayan corrido
        yield return null;

        // Verificamos si ya hay un destino de transición activo (para no interferir con AreaEntrance)
        bool isSceneTransitioning = SceneManagement.Instance != null && !string.IsNullOrEmpty(SceneManagement.Instance.sceneTransitionName);
        if (isSceneTransitioning) yield break;

        // 1. Si no hay instancia de jugador y tenemos un prefab, lo creamos
        if (BaseCharacter.PlayerInstance == null)
        {
            if (spawnIfMissing && playerPrefab != null)
            {
                GameObject newPlayer = Instantiate(playerPrefab, transform.position, Quaternion.identity);
                // Nota: El BaseCharacter del prefab ejecutará su propio Start e InitCharacter()
                // registrándose automáticamente como PlayerInstance.
            }
            else
            {
                Debug.LogWarning("[PlayerSpawnPoint] No se encontró PlayerInstance y no hay prefab asignado.");
            }
        }
        else
        {
            // 2. Si ya existe (puesto manualmente en escena), lo movemos aquí
            BaseCharacter.PlayerInstance.transform.position = transform.position;
            
            // Forzamos a la cámara a actualizar su objetivo inmediatamente
            if (CameraController.Instance != null)
            {
                CameraController.Instance.SetPlayerCameraFollow();
            }
        }
    }
}