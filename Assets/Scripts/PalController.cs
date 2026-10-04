using UnityEngine;

public class PalController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float wanderRadius = 2f;
    [SerializeField] private float waitTime = 3f;

    [SerializeField] private float threatRotationSpeed = 30f;

    [SerializeField] private AudioSource audioSource;

    private bool isThreat;
    private float waitTimer;

    private Rigidbody rb;

    private Vector3 targetPosition;

    private bool isWaiting;
    private bool isLookingAtPlayer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ChooseRandomTarget();
    }

    public void MoveTo(Vector3 target)
    {
        targetPosition = target;
    }

    private void FixedUpdate()
    {
        if (isThreat)
        {
            LookAtPlayer();
            return;
        }

        float distance = Vector3.Distance(
            rb.position,
            targetPosition
        );

        if (distance < 0.1f)
        {
            if (!isWaiting)
            {
                isWaiting = true;
                audioSource.Play();
            }

            waitTimer += Time.fixedDeltaTime;

            if (waitTimer >= waitTime)
            {
                waitTimer = 0f;
                isWaiting = false;
                ChooseRandomTarget();
            }

            return;
        }

        Vector3 direction = targetPosition - rb.position;
        direction.y = 0f;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            Quaternion rotation = Quaternion.RotateTowards(
                rb.rotation,
                targetRotation,
                180f * Time.fixedDeltaTime
            );

            rb.MoveRotation(rotation);
        }

        Vector3 nextPosition = Vector3.MoveTowards(
            rb.position,
            targetPosition,
            moveSpeed * Time.fixedDeltaTime
        );

        nextPosition.y = rb.position.y;

        rb.MovePosition(nextPosition);
    }

    private void ChooseRandomTarget()
    {
        Vector2 randomPoint = Random.insideUnitCircle * wanderRadius;

        Vector3 target = new Vector3(
            rb.position.x + randomPoint.x,
            rb.position.y,
            rb.position.z + randomPoint.y
        );

        MoveTo(target);
    }

    public void EnterThreat()
    {
        if (isThreat) return;

        isThreat = true;
        waitTimer = 0f;
        AudioSource.PlayClipAtPoint(
            audioSource.clip,
            transform.position,
            audioSource.volume
        );
    }

    private void LookAtPlayer()
    {
        Vector3 direction = Camera.main.transform.position - rb.position;
        direction.y = 0f;

        if (direction == Vector3.zero)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        Quaternion rotation = Quaternion.RotateTowards(
            rb.rotation,
            targetRotation,
            threatRotationSpeed * Time.fixedDeltaTime
        );

        rb.MoveRotation(rotation);

        float angle = Quaternion.Angle(rb.rotation, targetRotation);

        if (angle < 1f)
        {
            if (!isLookingAtPlayer)
            {
                isLookingAtPlayer = true;

                AudioSource.PlayClipAtPoint(
                    audioSource.clip,
                    transform.position,
                    audioSource.volume
                );
            }
        }
        else
        {
            isLookingAtPlayer = false;
        }
    }
}