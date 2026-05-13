using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class RPGMovement : BaseCharacter
{
    float speed;
    float horizontal;
    float vertical;

    // Instancia estática para acceso global
    public static RPGMovement Instance { get; private set; }

    protected override void Awake()
    {
        // Configuración del Singleton
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        base.Awake(); // Inicializa componentes y agent vía BaseCharacter
        agent.enabled = false; 
    }

    void Update()
    {
        // La lectura de Input siempre es mejor en Update para no perder pulsaciones
        HandleInput();
    }

    void FixedUpdate()
    {
        // Si el GameManager no está en modo exploración, controlamos el bloqueo
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameState.Exploration)
        {
            // Permitimos el control externo solo durante la fase de posicionamiento de batalla
            bool isPositioning = GameManager.Instance.currentState == GameState.Battle && BattleManager.Instance.state == BattleState.Start;

            if (!isPositioning)
            {
                if (agent.enabled) agent.isStopped = true;
                rb.linearVelocity = Vector2.zero;
                UpdateAnimations(Vector2.zero);
            }
            return;
        }

        // Sincronizar animaciones si el agente está moviendo al jugador (ej. cinemáticas o inicio batalla)
        if (agent != null && agent.enabled && agent.velocity.sqrMagnitude > 0.01f)
        {
            UpdateAnimations(agent.velocity);
            return;
        }

        Move();
    }

    private void HandleInput()
    {
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");

        if(Input.GetKey("z") || Input.GetButton("Fire1")) {
            speed = characterData.runSpeed;
        } else {
            // Al caminar reducimos la magnitud del input para la animación y velocidad
            horizontal *= 0.5f;
            vertical *= 0.5f;
            speed = characterData.walkSpeed;
        }
    }

    private void Move()
    {
        Vector2 moveDir = new Vector2(horizontal, vertical);
        
        // Si intentamos movernos hacia un obstáculo, forzamos el input a cero para detener la animación
        if (moveDir.sqrMagnitude > 0.001f && HasObstacleAhead(moveDir.normalized, characterData.detectionDistance, characterData.characterRadius, characterData.obstacleLayer, characterData.raycastOffset)) {
            moveDir = Vector2.zero;
        }

        UpdateAnimations(moveDir);
        rb.linearVelocity = moveDir * speed;
    }
}
