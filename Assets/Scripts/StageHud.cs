using UnityEngine;
using UnityEngine.UI;

public class StageHud : MonoBehaviour
{
    private Text playerHpText;
    private Text progressText;
    private Text bossHpText;
    private Font defaultFont;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapStageHud()
    {
        if (FindObjectOfType<StageHud>() != null)
        {
            return;
        }

        GameObject hudObject = new GameObject("StageHud");
        hudObject.AddComponent<StageHud>();
    }

    private void Awake()
    {
        defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        BuildHud();
    }

    private void Update()
    {
        RefreshTexts();
    }

    private void BuildHud()
    {
        Canvas canvas = CreateCanvas();

        GameObject leftPanel = CreatePanel("StageHudLeftPanel", canvas.transform, new Vector2(20f, -20f), new Vector2(300f, 120f), TextAnchor.UpperLeft);
        playerHpText = CreateText("PlayerHpText", leftPanel.transform, new Vector2(12f, -12f), new Vector2(276f, 28f), 22, TextAnchor.UpperLeft);
        progressText = CreateText("ProgressText", leftPanel.transform, new Vector2(12f, -44f), new Vector2(276f, 56f), 20, TextAnchor.UpperLeft);

        GameObject rightPanel = CreatePanel("StageHudRightPanel", canvas.transform, new Vector2(-20f, -20f), new Vector2(240f, 72f), TextAnchor.UpperRight);
        bossHpText = CreateText("BossHpText", rightPanel.transform, new Vector2(12f, -12f), new Vector2(216f, 48f), 22, TextAnchor.UpperRight);
    }

    private void RefreshTexts()
    {
        GameManager gameManager = GameManager.Instance;
        StageManager stageManager = StageManager.Instance;

        if (playerHpText != null)
        {
            int playerHp = gameManager != null ? gameManager.PlayerHP : 0;
            playerHpText.text = $"Player HP  {playerHp}";
        }

        if (progressText != null)
        {
            if (stageManager == null)
            {
                progressText.text = "Distance  0 / 0";
            }
            else
            {
                string modeLabel = stageManager.IsBossField ? "Boss Field" : "Normal Field";
                progressText.text = $"{modeLabel}\nDistance  {stageManager.CurrentDistance} / {stageManager.targetDistance}";
            }
        }

        if (bossHpText != null)
        {
            bool showBossHp = stageManager != null && stageManager.IsBossField && stageManager.boss != null;
            bossHpText.transform.parent.gameObject.SetActive(showBossHp);

            if (showBossHp)
            {
                bossHpText.text = $"Boss HP  {stageManager.boss.CurrentHP} / {stageManager.boss.maxHP}";
            }
        }
    }

    private Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("StageHudCanvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private GameObject CreatePanel(string name, Transform parent, Vector2 anchoredPosition, Vector2 sizeDelta, TextAnchor anchor)
    {
        GameObject panelObject = new GameObject(name);
        panelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = panelObject.AddComponent<RectTransform>();
        Image image = panelObject.AddComponent<Image>();

        image.color = new Color(0.08f, 0.12f, 0.18f, 0.82f);

        switch (anchor)
        {
            case TextAnchor.UpperRight:
                rectTransform.anchorMin = new Vector2(1f, 1f);
                rectTransform.anchorMax = new Vector2(1f, 1f);
                rectTransform.pivot = new Vector2(1f, 1f);
                break;
            default:
                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(0f, 1f);
                rectTransform.pivot = new Vector2(0f, 1f);
                break;
        }

        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
        return panelObject;
    }

    private Text CreateText(string name, Transform parent, Vector2 anchoredPosition, Vector2 sizeDelta, int fontSize, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        Text text = textObject.AddComponent<Text>();

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;

        text.font = defaultFont;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        return text;
    }
}
