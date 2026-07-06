using UnityEngine;

[CreateAssetMenu(fileName = "New Dice", menuName = "Game/Dice")]
public class Dice : ScriptableObject
{
    public string diceName;
    public Sprite icon;
    public int minValue;
    public int maxValue;
}