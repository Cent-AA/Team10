using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Players : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject playerPrefab2;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private BG3PortraitHealthBar[] playerPortraitBars = new BG3PortraitHealthBar[2];

    private bool wasdjoined = false;
    private bool arrowsjoined = false;
    public int pAmount = 0;
    private List<Gamepad> usedGamepads = new List<Gamepad>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var gamePad in Gamepad.all)
        {
            if (usedGamepads.Contains(gamePad)) continue;
            if (gamePad.rightTrigger.wasPressedThisFrame)
            {
                if(pAmount == 0)
                {
                    var player = PlayerInput.Instantiate(playerPrefab, controlScheme: "Gamepad", pairWithDevice: gamePad);
                    AssignPortraitBar(player, 0);
                    pAmount++;
                    usedGamepads.Add(gamePad);
                    Registry.Register(player.transform);
                }
                else if (pAmount == 1)
                {
                    var player = PlayerInput.Instantiate(playerPrefab2, controlScheme: "Gamepad", pairWithDevice: gamePad);
                    AssignPortraitBar(player, 1);
                    pAmount++;
                    usedGamepads.Add(gamePad);
                    Registry.Register(player.transform);
                }
            }
        }

        if(Keyboard.current == null) return;
        if(!wasdjoined && Keyboard.current.spaceKey.wasPressedThisFrame && pAmount < 2)
        {
            EditorLog("pressed");
            if(pAmount == 0)
            {
                var player = PlayerInput.Instantiate(playerPrefab, controlScheme: "Keyboard1", pairWithDevice: Keyboard.current);
                AssignPortraitBar(player, 0);
                pAmount++;
                if (spawnPoints.Length > 0)
                {
                    player.transform.position = spawnPoints[0].position;
                }
                wasdjoined = true;
                EditorLog("Spawn start");

                Registry.Register(player.transform);
                EditorLog("Spawn finished");
            }
            else if(pAmount == 1)
            {
                var player = PlayerInput.Instantiate(playerPrefab2, controlScheme: "Keyboard1", pairWithDevice: Keyboard.current);
                AssignPortraitBar(player, 1);
                pAmount++;
                if (spawnPoints.Length > 0)
                {
                    player.transform.position = spawnPoints[0].position;
                }
                wasdjoined = true;
                Registry.Register(player.transform);
            }
        }

        if(!arrowsjoined && Keyboard.current.backspaceKey.wasPressedThisFrame && pAmount < 2)
        {
            if(pAmount == 0)
            {
                var player = PlayerInput.Instantiate(playerPrefab, controlScheme: "Keyboard2", pairWithDevice: Keyboard.current);
                AssignPortraitBar(player, 0);
                pAmount++;
                if (spawnPoints.Length > 0)
                {
                    player.transform.position = spawnPoints[0].position;
                }
                arrowsjoined = true;
                Registry.Register(player.transform);
            }
            else if(pAmount == 1)
            {
                var player = PlayerInput.Instantiate(playerPrefab2, controlScheme: "Keyboard2", pairWithDevice: Keyboard.current);
                AssignPortraitBar(player, 1);
                pAmount++;
                if (spawnPoints.Length > 0)
                {
                    player.transform.position = spawnPoints[0].position;
                }
                arrowsjoined = true;
                Registry.Register(player.transform);
            }
        }
    }

    void AssignPortraitBar(PlayerInput player, int playerIndex)
    {
        if (player == null || playerIndex < 0)
        {
            return;
        }

        PlayerController controller = player.GetComponent<PlayerController>();
        if (controller == null)
        {
            controller = player.GetComponentInChildren<PlayerController>();
        }

        if (controller == null)
        {
            return;
        }

        controller.playerNumber = playerIndex + 1;

        BG3PortraitHealthBar portraitBar = GetPortraitBar(playerIndex);
        if (portraitBar != null)
        {
            portraitBar.Bind(controller);
        }
    }

    BG3PortraitHealthBar GetPortraitBar(int playerIndex)
    {
        if (playerPortraitBars != null && playerIndex >= 0 && playerIndex < playerPortraitBars.Length && playerPortraitBars[playerIndex] != null)
        {
            return playerPortraitBars[playerIndex];
        }

        int playerNumber = playerIndex + 1;
        BG3PortraitHealthBar[] portraitBars = FindObjectsByType<BG3PortraitHealthBar>(FindObjectsInactive.Exclude);
        for (int i = 0; i < portraitBars.Length; i++)
        {
            if (portraitBars[i] != null && portraitBars[i].PlayerNumber == playerNumber)
            {
                return portraitBars[i];
            }
        }

        return null;
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private static void EditorLog(object message)
    {
        Debug.Log(message);
    }
}
