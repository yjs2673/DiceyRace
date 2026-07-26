using UnityEngine;
using UnityEngine.UI;

public class TutorialInfoUI : MonoBehaviour
{
    [SerializeField] private Button returnButton;
    [SerializeField] private Button prevPageButton;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private GameObject[] pages;

    private int currentPageIndex;

    private void Awake()
    {
        if (returnButton != null)
        {
            returnButton.onClick.AddListener(Hide);
        }

        if (prevPageButton != null)
        {
            prevPageButton.onClick.AddListener(ShowPreviousPage);
        }

        if (nextPageButton != null)
        {
            nextPageButton.onClick.AddListener(ShowNextPage);
        }

        currentPageIndex = 0;
        RefreshPages();
    }

    private void OnDestroy()
    {
        if (returnButton != null)
        {
            returnButton.onClick.RemoveListener(Hide);
        }

        if (prevPageButton != null)
        {
            prevPageButton.onClick.RemoveListener(ShowPreviousPage);
        }

        if (nextPageButton != null)
        {
            nextPageButton.onClick.RemoveListener(ShowNextPage);
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void ShowPreviousPage()
    {
        SetPage(currentPageIndex - 1);
    }

    public void ShowNextPage()
    {
        SetPage(currentPageIndex + 1);
    }

    private void SetPage(int pageIndex)
    {
        if (pages == null || pages.Length == 0)
        {
            return;
        }

        currentPageIndex = Mathf.Clamp(pageIndex, 0, pages.Length - 1);
        RefreshPages();
    }

    private void RefreshPages()
    {
        int pageCount = pages != null ? pages.Length : 0;

        for (int i = 0; i < pageCount; i++)
        {
            if (pages[i] != null)
            {
                pages[i].SetActive(i == currentPageIndex);
            }
        }

        if (prevPageButton != null)
        {
            prevPageButton.gameObject.SetActive(currentPageIndex > 0);
        }

        if (nextPageButton != null)
        {
            nextPageButton.gameObject.SetActive(currentPageIndex < pageCount - 1);
        }
    }
}
