using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject scanCanvas;
    [SerializeField] private GameObject mainMenuCanvas;
    [SerializeField] private GameObject arModeCanvas;
    [SerializeField] private UnityEngine.UI.Button readyButton;
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private GameObject palPrefab;
    [SerializeField] private GameObject freePalPrefab;
    [SerializeField] private GameObject palSpherePrefab;
    [SerializeField] private Button throwButton;
    [SerializeField] private Button petButton;
    [SerializeField] private float spawnRadius = 3f;
    [SerializeField] private float spawnHeight = 0.3f;
    [SerializeField] private float maxDistance = 10f;

    private GameObject currentPal;
    private GameObject currentSphere;

    private void Start()
    {
        ShowScan();
    }

    private void Update()
    {
        if (currentSphere == null)
            return;

        PalSphere sphere = currentSphere.GetComponent<PalSphere>();

        if (sphere.IsStopped())
        {
            Destroy(currentSphere);
            CreateSphere();
        }
    }

    public void ShowScan()
    {
        scanCanvas.SetActive(true);
        mainMenuCanvas.SetActive(false);
        arModeCanvas.SetActive(false);
        readyButton.interactable = false;
    }

    public void ShowMenu()
    {
        if (currentPal != null)
        {
            Destroy(currentPal);
            currentPal = null;
        }

        if (currentSphere != null)
        {
            Destroy(currentSphere);
            currentSphere = null;
        }

        scanCanvas.SetActive(false);
        mainMenuCanvas.SetActive(true);
        arModeCanvas.SetActive(false);
    }

    public void StartCapture()
    {
        scanCanvas.SetActive(false);
        mainMenuCanvas.SetActive(false);
        arModeCanvas.SetActive(true);
        throwButton.gameObject.SetActive(true);
        petButton.gameObject.SetActive(false);


        if (TryFindSpawnPosition(out Vector3 spawnPosition))
        {
            currentPal = Instantiate(
                palPrefab,
                spawnPosition,
                Quaternion.identity
            );
        }

        CreateSphere();
    }

    public void StartFree()
    {
        mainMenuCanvas.SetActive(false);
        arModeCanvas.SetActive(true);
        throwButton.gameObject.SetActive(false);
        petButton.gameObject.SetActive(true);

        Vector3 cameraPosition = Camera.main.transform.position;

        Vector3 spawnPosition = new Vector3(
            cameraPosition.x,
            spawnHeight,
            cameraPosition.z
        );

        currentPal = Instantiate(
            freePalPrefab,
            spawnPosition,
            Quaternion.identity
        );
    }

    private void OnEnable()
    {
        planeManager.trackablesChanged.AddListener(OnPlanesChanged);
    }

    private void OnDisable()
    {
        planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);
    }

    private void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
    {
        readyButton.interactable = planeManager.trackables.count > 0;
    }

    private bool TryFindSpawnPosition(out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;

        Vector3 cameraPosition = Camera.main.transform.position;

        float startAngle = Random.Range(0f, 360f);

        for (int i = 0; i < 36; i++)
        {
            float angle = startAngle + i * 10f;

            Vector3 candidate = cameraPosition + new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                0f,
                Mathf.Sin(angle * Mathf.Deg2Rad)
            ) * spawnRadius;

            foreach (ARPlane plane in planeManager.trackables)
            {
                if (IsPointInsidePlane(plane, candidate))
                {
                    spawnPosition = candidate;
                    spawnPosition.y = spawnHeight;

                    return true;
                }
            }
        }

        return false;
    }

    private bool IsPointInsidePlane(ARPlane plane, Vector3 worldPoint)
    {
        Vector3 localPoint = plane.transform.InverseTransformPoint(worldPoint);

        Vector2 point = new Vector2(
            localPoint.x,
            localPoint.z
        );

        NativeArray<Vector2> boundary = plane.boundary;

        bool inside = false;

        for (int i = 0, j = boundary.Length - 1;
             i < boundary.Length;
             j = i++)
        {
            if ((boundary[i].y > point.y) != (boundary[j].y > point.y) &&
                point.x < (boundary[j].x - boundary[i].x) *
                (point.y - boundary[i].y) /
                (boundary[j].y - boundary[i].y) +
                boundary[i].x)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private Vector3 GetAimDirection()
    {
        Ray ray = Camera.main.ViewportPointToRay(
            new Vector3(0.5f, 0.5f, 0f)
        );

        Vector3 target;

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            target = hit.point;
        }
        else
        {
            target = ray.GetPoint(maxDistance);
        }

        Vector3 direction = target - Camera.main.transform.position;

        direction.y = 0f;

        return direction.normalized;
    }

    public void LaunchSphere()
    {
        throwButton.interactable = false;

        PalSphere sphere = currentSphere.GetComponent<PalSphere>();

        sphere.Launch();
    }

    public void CreateSphere()
    {
        currentSphere = Instantiate(
            palSpherePrefab,
            Camera.main.transform.position,
            Quaternion.identity
        );

        throwButton.interactable = true;
    }

    public bool TryFindFreePosition(
    Vector3 center,
    float radius,
    out Vector3 position)
    {
        position = Vector3.zero;

        for (int i = 0; i < 36; i++)
        {
            float angle = Random.Range(0f, 360f);

            Vector3 candidate = center + new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                0f,
                Mathf.Sin(angle * Mathf.Deg2Rad)
            ) * radius;

            foreach (ARPlane plane in planeManager.trackables)
            {
                if (IsPointInsidePlane(plane, candidate))
                {
                    position = candidate;
                    position.y = spawnHeight;

                    return true;
                }
            }
        }

        return false;
    }

    public bool IsValidPosition(Vector3 worldPoint)
    {
        foreach (ARPlane plane in planeManager.trackables)
        {
            if (IsPointInsidePlane(plane, worldPoint))
            {
                return true;
            }
        }

        return false;
    }

    public void CallFreePal()
    {
        if (currentPal != null)
        {
            FreePalController pal = currentPal.GetComponent<FreePalController>();

            pal.ComeToPlayer();
        }
    }
}