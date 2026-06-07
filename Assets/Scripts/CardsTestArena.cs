using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Security.Cryptography;

public class CardsTestArena : MonoBehaviour
{
    //public GameObject lvlup;
    [SerializeField] private GameObject lvlup;
    [SerializeField] private Image card1;
    [SerializeField] private Image card2;
    [SerializeField] private Image card3;
    [SerializeField] private TMP_Text BuffTarget;
    public int who=1 ;
    private int card1Index;
    private int card2Index;
    private int card3Index;
    private HashSet<int> uniqueIndices = new HashSet<int>();
    [SerializeField] private Sprite[] cardSprites;
    private string[] Target =
    {
        "YOU SHOUDNT SEE THIS",
        "PLAYER 1 BUFFS",
        "TEAM BUFFS",
        "PLAYER 2 BUFFS",
        };
        
    public void TestCard()
    {
    //TEMPORARY NEEDS TO BE ON LVL UP BUT WE DONT HAVE XP OR ANYTHIN
        uniqueIndices.Clear();

        while (uniqueIndices.Count < 3)
        {
            int num = Random.Range(0, cardSprites.Length); 
            uniqueIndices.Add(num); 
        }
        
        int[] selected = new int[3];
        uniqueIndices.CopyTo(selected);
        card1Index = selected[0];
        card2Index = selected[1];
        card3Index = selected[2];
        card1.sprite = cardSprites[card1Index];
        card2.sprite = cardSprites[card2Index];
        card3.sprite = cardSprites[card3Index];
        //card1.sprite = cardSprites[selected[0]];
        //card2.sprite = cardSprites[selected[1]];
        //card3.sprite = cardSprites[selected[2]];
        BuffTarget.text =Target[who];
        foreach (int index in uniqueIndices)
        {
            //Debug.Log("Selected Index: " + index);
        }
            lvlup.SetActive(true);
            Time.timeScale = 0f;
            EditorLog(who);
        }
    public void PressCard()
    {
    Time.timeScale = 1f;

    who++;

    if (who >= 4)
    {
        who = 1;
        lvlup.SetActive(false);
        return;
    }

    TestCard();
    }
    public void SelectCard1()
    {
    ApplyCard(card1Index);
    }

    public void SelectCard2()
    {
    ApplyCard(card2Index);
    }

    public void SelectCard3()
    {
    ApplyCard(card3Index);
    }
    private void ApplyCard(int cardID)
    {
    switch (cardID)
    {
        case 0:
            EditorLog("Increase damage");
            break;

        case 1:
            EditorLog("Increase health");
            if (who == 1)
            {
                IncreasePlayerHealth(0, 1.75f);
            }
            else if (who == 2)
            {
                IncreasePlayerHealth(0, 1.75f);
                IncreasePlayerHealth(1, 1.75f);
            }
            else if (who == 3)
            {
                IncreasePlayerHealth(1, 1.75f);
            }
            break;

        case 2:
            EditorLog("Increase speed");
            break;
        case 3:
            EditorLog("Increase STUFF");
            break;
        default:
            EditorLog("Something idk");
            break;
    }
    PressCard();
    }

    private void IncreasePlayerHealth(int playerIndex, float multiplier)
    {
        if (Registry.Players == null || playerIndex < 0 || playerIndex >= Registry.Players.Count || Registry.Players[playerIndex] == null)
        {
            return;
        }

        PlayerController controller = Registry.Players[playerIndex].GetComponent<PlayerController>();
        if (controller == null)
        {
            controller = Registry.Players[playerIndex].GetComponentInChildren<PlayerController>();
        }

        if (controller == null)
        {
            return;
        }

        EditorLog(controller.maxHealth);
        EditorLog(controller.currentHealth);
        controller.MultiplyHealth(multiplier);
        EditorLog(controller.maxHealth);
        EditorLog(controller.currentHealth);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Transform pl1 = Registry.Players[0];
        //Transform pl2 = Registry.Players[1];
    }

    // Update is called once per frame
    void Update()
    {
        //NEED TO CHANGE SO ITS ON LVL UP / XP GAINED ON KILLS/ XP REQS INCREASES WITH EACH LVL.
        if (Keyboard.current != null && Keyboard.current.oKey.wasPressedThisFrame)
        {
            TestCard();
        }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private static void EditorLog(object message)
    {
        Debug.Log(message);
    }
}
