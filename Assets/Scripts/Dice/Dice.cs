using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class DiceFaceDefinition
{
    public int value = 1;
    public Vector3 localUpDirection = Vector3.up;
}

[CreateAssetMenu(fileName = "New Dice", menuName = "Game/Dice")]
public class Dice : ScriptableObject
{
    [Header("Basic Info")]
    public string diceName;
    public Sprite icon;
    public int price;
    // public int minValue;  
    // public int maxValue;
    [TextArea]
    public string description;

    [Header("Runtime Roll")]
    public GameObject runtimePrefab;
    [Min(-1)] public int preferredBoardSlot = -1;
    public Vector3 boardSpawnOffset;
    public Vector3 spawnEulerAngles;
    [Min(0.1f)] public float modelScale = 1f;
    [Min(0.1f)] public float rigidbodyMass = 1f;
    [Min(0.1f)] public float rollForceMultiplier = 1f;
    [Min(0.1f)] public float torqueMultiplier = 1f;
    public List<DiceFaceDefinition> faceDefinitions = new List<DiceFaceDefinition>();
}
