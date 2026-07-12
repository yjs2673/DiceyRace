using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Game/Card")]
public class CardData : ScriptableObject
{
    [Header("CardInfo")]
    public string cardName;
    public Sprite icon;
    public int price;

    [TextArea]
    public string description;

}