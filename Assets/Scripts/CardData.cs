using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Game/Card")]
public class CardData : ScriptableObject
{
    public string cardName;
    public Sprite icon;
    public string description;
}