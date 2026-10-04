using UnityEngine;

public class FreePalController : MonoBehaviour
{
    [SerializeField] private float runSpeed = 4f;
    [SerializeField] private float wanderSpeed = 0.5f;
    [SerializeField] private float wanderRadius = 1f;
    [SerializeField] private float playerRadius = 3f;
    [SerializeField] private float wanderWaitTime = 5f;
    [SerializeField] private float interactionWaitTime = 3f;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float restStartDelay = 1f;

    [SerializeField] private AudioSource idleAudio1;
    [SerializeField] private AudioSource idleAudio2;
    [SerializeField] private AudioSource idleAudio3;
    [SerializeField] private AudioSource happyAudio;

    private float wanderTimer;

    private Rigidbody rb;
    private GameManager gameManager;

    private Animator animator;

    private Vector3 targetPosition;

    private bool isWandering;
    private bool isWaiting;
    private bool isPetting;
    private bool isResting;
    private bool restChecked;
    private float restTimer;

    private enum FreePalState
    {
        InitialRun,
        Wandering,
        Coming,
        Waiting,
        Leaving
    }

    private FreePalState currentState;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        gameManager = FindFirstObjectByType<GameManager>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        // Al aparecer, corre hacia los 3 metros.
        currentState = FreePalState.InitialRun;
        PlayAnimation("Run");
        ChooseTarget();
    }

    private void FixedUpdate()
    {
        // Esperas
        if (isWaiting)
        {
            wanderTimer -= Time.fixedDeltaTime;

            // Espera normal
            if (!isPetting && !restChecked)
            {
                restTimer -= Time.fixedDeltaTime;

                // Después de 1 segundo, decidir si hace un rest
                if (restTimer <= 0f)
                {
                    restChecked = true;

                    // 50% de probabilidad de hacer un rest
                    if (Random.value < 0.5f)
                    {
                        int rest = Random.Range(1, 4);

                        if (rest == 1)
                            PlayAnimation("Rest01");
                        else if (rest == 2)
                            PlayAnimation("Rest02");
                        else
                            PlayAnimation("Rest03");

                        isResting = true;
                    }
                }
            }

            // Si el rest terminó, volver a idle
            if (isResting)
            {
                AnimatorStateInfo stateInfo =
                    animator.GetCurrentAnimatorStateInfo(0);

                if (stateInfo.normalizedTime >= 1f)
                {
                    PlayAnimation("Idle");
                    isResting = false;
                }
            }

            // Terminó la espera completa
            if (wanderTimer <= 0f)
            {
                isWaiting = false;
                isResting = false;

                if (isPetting)
                {
                    StartLeaving();
                }
                else if (currentState == FreePalState.Wandering)
                {
                    PlayAnimation("Walk");
                    ChooseWanderTarget();
                }
            }

            return;
        }

        Vector2 currentPosition = new Vector2(
            rb.position.x,
            rb.position.z
        );

        Vector2 target = new Vector2(
            targetPosition.x,
            targetPosition.z
        );

        float distance = Vector2.Distance(
            currentPosition,
            target
        );

        // Llegamos al objetivo
        if (distance < 0.1f)
        {
            switch (currentState)
            {
                case FreePalState.InitialRun:

                    currentState = FreePalState.Wandering;
                    isWandering = true;
                    PlayAnimation("Walk");
                    // Primer punto de deambulación inmediatamente
                    ChooseWanderTarget();

                    break;

                case FreePalState.Wandering:

                    if (!isWandering)
                    {
                        isWandering = true;
                        ChooseWanderTarget();
                    }
                    else
                    {
                        isWaiting = true;
                        isPetting = false;
                        restChecked = false;
                        PlayAnimation("Idle");

                        int idleSound = Random.Range(1, 4);

                        if (idleSound == 1)
                            idleAudio1.Play();
                        else if (idleSound == 2)
                            idleAudio2.Play();
                        else
                            idleAudio3.Play();

                        wanderTimer = wanderWaitTime;
                        restTimer = restStartDelay;
                    }

                    break;

                case FreePalState.Coming:

                if (LookAtPlayer())
                {
                    PlayAnimation("Petting");
                    happyAudio.Play();
                    currentState = FreePalState.Waiting;
                    isWaiting = true;
                    wanderTimer = interactionWaitTime;
                }

                break;

                case FreePalState.Leaving:

                    currentState = FreePalState.Wandering;
                    PlayAnimation("Walk");
                    isWandering = true;

                    ChooseWanderTarget();

                    break;
            }

            return;
        }

        // InitialRun, Coming y Leaving usan runSpeed.
        // Wandering usa wanderSpeed.
        float speed = currentState == FreePalState.Wandering
            ? wanderSpeed
            : runSpeed;

        Vector3 direction = targetPosition - rb.position;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            rb.MoveRotation(
                Quaternion.RotateTowards(
                    rb.rotation,
                    targetRotation,
                    rotationSpeed * Time.fixedDeltaTime
                )
            );
        }

        Vector3 nextPosition = Vector3.MoveTowards(
            rb.position,
            targetPosition,
            speed * Time.fixedDeltaTime
        );

        nextPosition.y = rb.position.y;

        // No salir de un plano válido.
        if (gameManager.IsValidPosition(nextPosition))
        {
            rb.MovePosition(nextPosition);
        }
        else
        {
            if (currentState == FreePalState.Wandering)
            {
                ChooseWanderTarget();
            }
        }
    }

    private void ChooseTarget()
    {
        Vector3 playerPosition =
            Camera.main.transform.position;

        if (gameManager.TryFindFreePosition(
            playerPosition,
            playerRadius,
            out Vector3 position))
        {
            targetPosition = position;

            Debug.Log(
                "Objetivo encontrado. Distancia: " +
                Vector3.Distance(
                    playerPosition,
                    targetPosition
                ) + " m"
            );
        }
        else
        {
            Debug.Log(
                "No se encontró un punto válido."
            );
        }
    }

    private bool ChooseWanderTarget()
    {
        if (gameManager.TryFindFreePosition(
            rb.position,
            wanderRadius,
            out Vector3 position))
        {
            targetPosition = position;

            return true;
        }


        return false;
    }

    public void ComeToPlayer()
    {
        currentState = FreePalState.Coming;

        PlayAnimation("Run");

        isWaiting = false;
        isPetting = true;

        Vector3 playerPosition =
            Camera.main.transform.position;

        Vector3 playerForward =
            Camera.main.transform.forward;

        playerForward.y = 0f;

        if (playerForward != Vector3.zero)
        {
            playerForward.Normalize();
        }

        // 1 metro frente al jugador.
        targetPosition =
            playerPosition + playerForward * 1f;
    }

    private bool LookAtPlayer()
    {
        Vector3 direction =
            Camera.main.transform.position - rb.position;

        direction.y = 0f;

        if (direction == Vector3.zero)
        {
            return true;
        }

        Quaternion targetRotation =
            Quaternion.LookRotation(direction);

        rb.MoveRotation(
            Quaternion.RotateTowards(
                rb.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            )
        );

        float angle = Quaternion.Angle(
            rb.rotation,
            targetRotation
        );

        return angle < 1f;
    }

    private void StartLeaving()
    {
        currentState = FreePalState.Leaving;

        PlayAnimation("Run");

        isWaiting = false;
        isPetting = false;

        // Busca nuevamente un punto a 3 metros.
        ChooseTarget();
    }

    private void PlayAnimation(string animationName)
    {
        if (animator == null)
            return;

        if (animator.GetCurrentAnimatorStateInfo(0).IsName(animationName))
            return;

        animator.CrossFadeInFixedTime(animationName, 0.3f, 0);
    }
}