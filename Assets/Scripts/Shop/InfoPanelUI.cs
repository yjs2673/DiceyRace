using TMPro;
using UnityEngine;

public class InfoPanelUI : MonoBehaviour
{
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text pirateCoinText;

    private void Start()
    {
        GetCoinData();
    }
    public void GetCoinData()
    {
        if (GameManager.Instance == null)
        {
            return;
        }
        coinText.text = GameManager.Instance.Coin.ToString();
        pirateCoinText.text = GameManager.Instance.PirateCoin.ToString();
    }

}
