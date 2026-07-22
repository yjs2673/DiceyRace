using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-900)]
public class SceneTransitionFader : MonoBehaviour
{
    private static SceneTransitionFader instance;

    private Canvas canvas;
    private Image fadeImage;
    private Coroutine transitionRoutine;

    public static SceneTransitionFader Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject faderObject = new GameObject("SceneTransitionFader");
                instance = faderObject.AddComponent<SceneTransitionFader>();
            }

            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlay();
        SetAlpha(0f);
    }

    public bool IsTransitioning => transitionRoutine != null;

    public void FadeToScene(string sceneName, float fadeDuration, float holdDuration)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("전환할 씬 이름이 비어 있습니다.");
            return;
        }

        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(FadeToSceneRoutine(sceneName, fadeDuration, holdDuration));
    }

    private IEnumerator FadeToSceneRoutine(string sceneName, float fadeDuration, float holdDuration)
    {
        float duration = Mathf.Max(0.01f, fadeDuration);
        float hold = Mathf.Max(0f, holdDuration);

        yield return Fade(0f, 1f, duration);

        if (hold > 0f)
        {
            yield return new WaitForSeconds(hold);
        }

        SceneManager.LoadScene(sceneName);
        yield return null;

        yield return Fade(1f, 0f, duration);
        transitionRoutine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetAlpha(to);
    }

    private void BuildOverlay()
    {
        GameObject canvasObject = new GameObject("FadeCanvas");
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject imageObject = new GameObject("FadeImage");
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rectTransform = imageObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        fadeImage = imageObject.AddComponent<Image>();
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = false;
    }

    private void SetAlpha(float alpha)
    {
        if (fadeImage == null)
        {
            return;
        }

        Color color = fadeImage.color;
        color.a = Mathf.Clamp01(alpha);
        fadeImage.color = color;
    }
}
