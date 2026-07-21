using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShopTooltipUI : MonoBehaviour
{
    public static ShopTooltipUI Instance {get; private set;}
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private float mouseOffset = 20f;

    private RectTransform tooltipRect;
    private CanvasGroup canvasGroup;
    private Canvas tooltipCanvas;
    private bool isVisible;

    private void Awake()
    {
        Instance = this;
        tooltipRect = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        tooltipCanvas = GetComponent<Canvas>();

        if (tooltipCanvas == null)
            tooltipCanvas = gameObject.AddComponent<Canvas>();

        tooltipCanvas.overrideSorting = true;
        tooltipCanvas.sortingOrder = 1000;
        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.ignoreParentGroups = true;
        Hide();
    }
    private void Update()
    {
        if(isVisible)
            UpdatePosition();
    }
    public void Show(string itemName, string description)
    {
        itemNameText.text = itemName;
        descriptionText.text = string.IsNullOrWhiteSpace(description) ? "설명이 없습니다." : description;
        transform.SetAsLastSibling();
        canvasGroup.alpha = 1f;
        isVisible = true;
        UpdatePosition();
    }
    public void Hide()
    {
        canvasGroup.alpha = 0f;
        isVisible = false;
    }
    private void UpdatePosition()
    {
        Vector2 mousePosition = Input.mousePosition;
        bool isRightSide = mousePosition.x > Screen.width * 0.5f;
        bool isTopSide = mousePosition.y > Screen.height * 0.5f;
        tooltipRect.pivot = new Vector2(isRightSide ? 1f : 0f, isTopSide? 1f : 0f);
        float offsetX = isRightSide ? -mouseOffset : mouseOffset;
        float offsetY = isTopSide ? -mouseOffset : mouseOffset;
        tooltipRect.position = mousePosition + new Vector2(offsetX, offsetY);
    }
    private void OnDestroy()
    {
        if(Instance == this)
            Instance = null;
    }
}
