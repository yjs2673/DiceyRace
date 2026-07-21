using TMPro;
using UnityEngine;

public class InfoPanelUI : MonoBehaviour
{
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text pirateCoinText;
    [SerializeField] private TMP_Text rerollLeftText;

    private void Start()
    {
        GetCoinData();
    }
    public void GetCoinData()
    {
        if (GameManager.Instance == null)
        {
            Debug.Log("GameManger�� �������� �ʽ��ϴ�.");
            return;
        }
        coinText.text = GameManager.Instance.Coin.ToString();
        pirateCoinText.text = GameManager.Instance.PirateCoin.ToString();
        rerollLeftText.text = GameManager.Instance.Reroll.ToString();
    }

}
