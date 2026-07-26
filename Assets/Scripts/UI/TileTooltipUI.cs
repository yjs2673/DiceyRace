using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TileTooltipUI : MonoBehaviour
{
    public static TileTooltipUI Instance { get; private set; }

    [SerializeField] private Text typeText;
    [SerializeField] private Text effectText;
    [SerializeField] private float mouseOffset = 20f;

    private RectTransform tooltipRect;
    private CanvasGroup canvasGroup;
    private Canvas parentCanvas;
    private RectTransform canvasRect;
    private bool isVisible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AttachToSceneTooltipIfNeeded()
    {
        GameObject tooltipObject = GameObject.Find("TileTooltip");
        if (tooltipObject == null || tooltipObject.GetComponent<TileTooltipUI>() != null)
        {
            return;
        }

        tooltipObject.AddComponent<TileTooltipUI>();
    }

    private void Awake()
    {
        if (gameObject.name != "TileTooltip")
        {
            enabled = false;
            return;
        }

        if (Instance != null && Instance != this)
        {
            enabled = false;
            return;
        }

        tooltipRect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        parentCanvas = GetComponentInParent<Canvas>();
        canvasRect = parentCanvas != null ? parentCanvas.GetComponent<RectTransform>() : null;

        if (tooltipRect == null || canvasGroup == null || parentCanvas == null || canvasRect == null)
        {
            enabled = false;
            return;
        }

        typeText = typeText != null ? typeText : FindText("TypeText", "ItemNameText");
        effectText = effectText != null ? effectText : FindText("EffectText", "DescriptionText");
        if (typeText == null || effectText == null)
        {
            enabled = false;
            return;
        }

        Instance = this;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.ignoreParentGroups = true;
        Hide();
    }

    private void Update()
    {
        if (Instance != this)
        {
            return;
        }

        UpdateHoveredTile();
        if (isVisible)
        {
            UpdatePosition();
        }
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            Hide();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Show(Tile tile)
    {
        if (tile == null || !tile.HasInspectableEffect)
        {
            Hide();
            return;
        }

        typeText.text = tile.GetTooltipTypeText();
        effectText.text = tile.GetTooltipEffectText();
        transform.SetAsLastSibling();
        canvasGroup.alpha = 1f;
        isVisible = true;
    }

    public void Hide()
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = 0f;
        isVisible = false;
    }

    private void UpdateHoveredTile()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            Hide();
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Hide();
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            Hide();
            return;
        }

        Tile tile = hit.collider.GetComponentInParent<Tile>();
        if (tile == null || !tile.HasInspectableEffect)
        {
            Hide();
            return;
        }

        Show(tile);
    }

    private void UpdatePosition()
    {
        Vector2 mousePosition = Input.mousePosition;
        bool isRightSide = mousePosition.x > Screen.width * 0.5f;
        bool isTopSide = mousePosition.y > Screen.height * 0.5f;

        tooltipRect.pivot = new Vector2(isRightSide ? 1f : 0f, isTopSide ? 1f : 0f);

        float offsetX = isRightSide ? -mouseOffset : mouseOffset;
        float offsetY = isTopSide ? -mouseOffset : mouseOffset;
        Vector2 adjustedScreenPoint = mousePosition + new Vector2(offsetX, offsetY);
        Camera uiCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : parentCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, adjustedScreenPoint, uiCamera, out Vector2 localPoint))
        {
            tooltipRect.anchoredPosition = localPoint;
        }
    }

    private Text FindText(string primaryName, string fallbackName)
    {
        Transform primary = transform.Find(primaryName);
        if (primary != null && primary.TryGetComponent(out Text primaryText))
        {
            return primaryText;
        }

        Transform fallback = transform.Find(fallbackName);
        if (fallback != null && fallback.TryGetComponent(out Text fallbackText))
        {
            return fallbackText;
        }

        return null;
    }
}
