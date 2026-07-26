using UnityEngine;

public class TitleMove : MonoBehaviour
{
    public float amplitude = 50f;
    public float frequency = 2f;

    private RectTransform rectTransform;
    private Vector2 initialAnchoredPosition;
    private Vector3 initialLocalPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            initialAnchoredPosition = rectTransform.anchoredPosition;
            return;
        }

        initialLocalPosition = transform.localPosition;
    }

    private void Update()
    {
        float time = Time.unscaledTime;
        float floatOffset = Mathf.Sin(time * frequency) * amplitude;

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = initialAnchoredPosition + new Vector2(0f, floatOffset);
            return;
        }

        transform.localPosition = initialLocalPosition + new Vector3(0f, floatOffset, 0f);
    }
}
