using UnityEngine.UI;
using UnityEngine;

public class RerollButton : MonoBehaviour
{
   [SerializeField] private ShopManager shopManager;
   [SerializeField] private InfoPanelUI infoPanelUI;

   private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(RerollShop);
    }
    private void Start()
    {
        RefreshButton();
    }
    private void RerollShop()
    {
        if(shopManager == null)
        {
            Debug.LogError("ShopManager가 존재하지 않습니다.");
            return;
        }
        if(GameManager.Instance == null)
        {
            Debug.LogError("GameManager가 존재하지 않습니다.");
            return;
        }
        if (!GameManager.Instance.UseReroll())
        {
            Debug.Log("남은 리롤 횟수가 없습니다.");
            RefreshButton();
            return;
        }
        shopManager.CreateShop();
        if(infoPanelUI != null)
            infoPanelUI.GetCoinData();
        RefreshButton();
    }
    private void RefreshButton()
    {
        if(GameManager.Instance == null)
        {
            Debug.LogError("GameManager가 존재하지 않습니다.");
            return;
        }
        button.interactable = GameManager.Instance.Reroll > 0;
    }
    private void OnDestroy()
    {
        if(button != null)
        {
            button.onClick.RemoveListener(RerollShop);
        }
    }
}
