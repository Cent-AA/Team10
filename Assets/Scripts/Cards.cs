using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;

public class Cards : MonoBehaviour
{
    //public GameObject lvlup;
    [SerializeField] private GameObject lvlup;
    [SerializeField] private TMP_Text cardText1;
    [SerializeField] private TMP_Text cardText2;
    [SerializeField] private TMP_Text cardText3;
    [SerializeField] private TMP_Text BuffTarget;
    public int who=1 ;
    private HashSet<int> uniqueIndices = new HashSet<int>();
        private string[] cardDescriptions =
    {
        "TEST1",
        "TEST2",
        "TEST3",
        "TEST4",
        "TEST5",
        "TEST6",
        "TEST7",
        "TEST8",
        "TEST9",
        "TEST10",
        "TEST11",
        "TEST12",
        "TEST13",
        "TEST14",
        "TEST15",
        "TEST16",
        "TEST17",
        "TEST18",
        "TEST19",
        "TEST20",
        "TEST21",
    }; // TEMPORARY WE DONT HAVE CARD DESIGNS
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
            int num = Random.Range(0, 21); 
            uniqueIndices.Add(num); 
        }
        
        int[] selected = new int[3];
        uniqueIndices.CopyTo(selected);
        cardText1.text = cardDescriptions[selected[0]];
        cardText2.text = cardDescriptions[selected[1]];
        cardText3.text = cardDescriptions[selected[2]];
        BuffTarget.text =Target[who];
        foreach (int index in uniqueIndices)
        {
            EditorLog("Selected Index: " + index);
        }
            lvlup.SetActive(true);
            Time.timeScale = 0f;
            EditorLog(who);
        }
    /*public void PressCard()
    {
 
         //GameObject clickedButton = EventSystem.current.currentSelectedGameObject;

        //Debug.Log(clickedButton.name);
        who++;
         Time.timeScale = 1f;
        if(who < 4)
      {
            who++;
            TestCard();
        }else{
            who =0;
            lvlup.SetActive(false);
            Time.timeScale = 1f;}
        
    }*/
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
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
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
