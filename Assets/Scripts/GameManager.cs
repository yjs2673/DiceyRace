using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private int initialCoin = 1000;
    [SerializeField] private int initialPirateCoin = 100;
    [SerializeField] private int initialReroll = 999;
    [SerializeField] private int initialPlayerHP = 100;
    [SerializeField] private int initialPlayerDamage = 10;
    [SerializeField] private string initialStageName = "DebugGround";
    [Header("테스트용 시작 인벤토리")]
    [SerializeField] private List<CardData> initialCards;
    [SerializeField] private List<Dice> initialDice;
    public int Coin { get; private set; }
    public int PirateCoin { get; private set; }

    public int Reroll { get; private set; }
    public int PlayerHP { get; private set; }
    public int PlayerDamage { get; private set; }
    public string StageName { get; private set; }
    private readonly List<Dice> hasDice = new List<Dice>();
    private readonly List<CardData> hasCard = new List<CardData>();

    public IReadOnlyList<Dice> HasDice => hasDice;
    public IReadOnlyList<CardData> HasCard => hasCard;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitDefaultData();
    }

    private void InitDefaultData()
    {
        Coin = initialCoin;
        PirateCoin = initialPirateCoin;
        Reroll = initialReroll;
        PlayerHP = initialPlayerHP;
        PlayerDamage = initialPlayerDamage;
        StageName = initialStageName;
        hasCard.Clear();
        hasDice.Clear();

        if (initialCards != null)
            hasCard.AddRange(initialCards);

        if (initialDice != null)
            hasDice.AddRange(initialDice);
    }
    // coin
    public void AddCoin(int amount)
    {
        Coin += amount;
    }

    public bool SpendCoin(int amount)
    {
        if (Coin < amount)
            return false;

        Coin -= amount;
        return true;
    }
    // pirate coin
    public void AddPirateCoin(int amount)
    {
        PirateCoin += amount;
    }

    public bool SpendPirateCoin(int amount)
    {
        if (PirateCoin < amount)
            return false;

        PirateCoin -= amount;
        return true;
    }
    // dice
    public void AddDice(Dice dice)
    {
        if (dice == null)
            return;

        hasDice.Add(dice);
    }

    public void RemoveDice(Dice dice)
    {
        hasDice.Remove(dice);
    }
    // card
    public void AddCard(CardData card)
    {
        if (card == null)
            return;

        hasCard.Add(card);
    }

    public void RemoveCard(CardData card)
    {
        hasCard.Remove(card);
    }
    // reroll
    public bool UseReroll()
    {
        if (Reroll <= 0)
            return false;

        Reroll--;
        return true;
    }

    public void AddReroll(int amount)
    {
        Reroll += amount;
    }
    // player HP
    public void SetPlayerHP(int hp)
    {
        PlayerHP = hp;
    }

    public void TakeDamage(int damage)
    {
        PlayerHP -= damage;

        if (PlayerHP < 0)
            PlayerHP = 0;
    }

    public void Heal(int amount)
    {
        PlayerHP += amount;
    }
    // player Damage
    public void AddPlayerDamage(int amount)
    {
        PlayerDamage += amount;
    }

    public void ReducePlayerDamage(int amount)
    {
        PlayerDamage -= amount;

        if (PlayerDamage < 0)
            PlayerDamage = 0;
    }
}