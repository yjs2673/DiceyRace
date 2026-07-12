using TMPro;
using UnityEngine;

public class LeftPanelInfo : MonoBehaviour
{
    [Header("Stage")]
    [SerializeField] private TMP_Text stageNameText;
    
    [Header("Move")]
    [SerializeField] private TMP_Text playerHPText;

    [Header("Reroll")]
    [SerializeField] private TMP_Text rerollLeftText;

    [Header("Gold")]
    [SerializeField] private TMP_Text goldLeftText;
    [SerializeField] private TMP_Text pirateGoldLeftText;

    private void Start()
    {
        RefreshUI();
    }
    public void RefreshUI()
    {
        if(GameManager.Instance == null)
        {
            Debug.Log("GameManger가 존재하지 않습니다.");
            return;
        }
        stageNameText.text = GameManager.Instance.StageName;
        playerHPText.text = GameManager.Instance.PlayerHP.ToString();
        rerollLeftText.text = GameManager.Instance.Reroll.ToString();
        goldLeftText.text = GameManager.Instance.Coin.ToString();
        pirateGoldLeftText.text =
            GameManager.Instance.PirateCoin.ToString();
        
    }

}
