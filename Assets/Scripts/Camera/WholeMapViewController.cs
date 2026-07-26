using UnityEngine;

public class WholeMapViewController : MonoBehaviour
{
    private static readonly Vector3 WholeMapCameraPosition = new Vector3(40f, 2f, -42f);
    private static readonly Vector3 WholeMapCameraRotation = Vector3.zero;

    public static WholeMapViewController Instance { get; private set; }

    public bool IsWholeMapViewActive => isWholeMapViewActive;

    private CameraFollow cameraFollow;
    private DiceManager diceManager;
    private bool isWholeMapViewActive;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapWholeMapViewController()
    {
        if (FindFirstObjectByType<WholeMapViewController>() != null)
        {
            return;
        }

        if (FindFirstObjectByType<PlayerController>() == null)
        {
            return;
        }

        GameObject controllerObject = new GameObject("WholeMapViewController");
        controllerObject.AddComponent<WholeMapViewController>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        RefreshReferences();

        if (isWholeMapViewActive && !CanKeepWholeMapViewOpen())
        {
            CloseWholeMapView();
        }
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        CloseWholeMapView();
        Instance = null;
    }

    public void ToggleWholeMapView()
    {
        RefreshReferences();

        if (isWholeMapViewActive)
        {
            CloseWholeMapView();
            return;
        }

        if (!CanOpenWholeMapView())
        {
            return;
        }

        isWholeMapViewActive = true;
        diceManager.SetAwaitingMoveSuspended(true);
        diceManager.SetPrimaryActionLocked(true);
        cameraFollow.SetTemporaryPose(WholeMapCameraPosition, WholeMapCameraRotation);
    }

    private bool CanOpenWholeMapView()
    {
        return cameraFollow != null
            && diceManager != null
            && diceManager.CanToggleWholeMapView;
    }

    private bool CanKeepWholeMapViewOpen()
    {
        return cameraFollow != null
            && diceManager != null
            && diceManager.CanToggleWholeMapView;
    }

    private void CloseWholeMapView()
    {
        if (!isWholeMapViewActive)
        {
            return;
        }

        isWholeMapViewActive = false;

        if (cameraFollow != null)
        {
            cameraFollow.ClearTemporaryPose();
            cameraFollow.SnapToFollowTarget();
        }

        if (diceManager != null)
        {
            diceManager.SetAwaitingMoveSuspended(false);
            diceManager.SetPrimaryActionLocked(false);
        }
    }

    private void RefreshReferences()
    {
        if (cameraFollow == null && Camera.main != null)
        {
            cameraFollow = Camera.main.GetComponent<CameraFollow>();
        }

        if (diceManager == null)
        {
            diceManager = FindFirstObjectByType<DiceManager>();
        }
    }
}
