using UnityEngine;
using System.Collections;

public class PalSphere : MonoBehaviour
{
    [SerializeField] private float forwardOffset = 0.4f;
    [SerializeField] private float verticalOffset = -0.18f;

    [SerializeField] private float launchDistance = 2f;
    [SerializeField] private float launchSpeed = 3f;
    [SerializeField] private float maxHeight = 0.5f;

    [SerializeField] private UnityEngine.UI.Image barFill;
    [SerializeField] private TMPro.TMP_Text probabilityText;

    [SerializeField] private GameObject captureCanvas;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource checkingAudioSource;
    [SerializeField] private AudioSource progressAudioSource;
    [SerializeField] private AudioSource captureAudioSource;

    private bool launched;
    private bool capturing;

    private GameObject capturedPal;

    private GameManager gameManager;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;

        captureCanvas.SetActive(false);

        gameManager = FindFirstObjectByType<GameManager>();
    }

    private void Update()
    {
        if (!launched)
        {
            transform.position = Camera.main.transform.position
                + Camera.main.transform.forward * forwardOffset
                + Vector3.up * verticalOffset;
        }

        if (captureCanvas.activeSelf)
        {
            captureCanvas.transform.LookAt(captureCanvas.transform.position * 2 - Camera.main.transform.position);
        }
    }

    public void Launch()
    {

        launched = true;
        rb.isKinematic = false;

        Vector3 target = Camera.main.transform.position
            + Camera.main.transform.forward * launchDistance;

        Vector3 direction = target - transform.position;
        direction.Normalize();

        float verticalSpeed = Mathf.Sqrt(
            2f * Mathf.Abs(Physics.gravity.y) * maxHeight
        );

        Vector3 velocity = direction * launchSpeed;
        velocity.y = verticalSpeed;

        rb.linearVelocity = velocity;
    }

    public bool IsStopped()
    {
        return launched && !capturing && rb.linearVelocity.magnitude < 0.1f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Pal"))
        {
            audioSource.Play();
            
            PalController pal = collision.gameObject.GetComponent<PalController>();

            pal.EnterThreat();
            StartCapture(collision.gameObject);
        }
    }

    private void StartCapture(GameObject pal)
    {
        launched = true;
        capturing = true;

        capturedPal = pal;

        captureCanvas.SetActive(true);

        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        pal.SetActive(false);

        StartCoroutine(CaptureAnimation());
    }

    private IEnumerator CaptureAnimation()
    {
        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + Vector3.up * 0.6f;

        float duration = 0.4f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                timer / duration
            );

            yield return null;
        }

        transform.position = targetPosition;

        float[] chances = { 50f, 60f, 100f };

        for (int i = 0; i < chances.Length; i++)
        {
            if (i > 0)
            {
                progressAudioSource.Play();
            }

            yield return StartCoroutine(IncreaseBar(chances[i] / 100f));

            yield return new WaitForSeconds(1f);

            checkingAudioSource.Play();

            yield return new WaitForSeconds(2f);

            float roll = Random.Range(0f, 100f);

            if (roll >= chances[i])
            {

                capturedPal.SetActive(true);
                Destroy(gameObject);

                gameManager.CreateSphere();

                yield break;
            }

        }

        probabilityText.text = "¡CAPTURADO!";
        captureAudioSource.Play();

        yield return new WaitForSeconds(3f);

        gameManager.ShowMenu();
    }

    private IEnumerator IncreaseBar(float target)
    {
        float start = barFill.fillAmount;
        float duration = 2f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;

            float value = Mathf.Lerp(start, target, timer / duration);

            barFill.fillAmount = value;
            probabilityText.text = Mathf.RoundToInt(value * 100f) + "%";

            yield return null;
        }

        barFill.fillAmount = target;
        probabilityText.text = Mathf.RoundToInt(target * 100f) + "%";
    }
}