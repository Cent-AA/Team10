using Mono.Cecil.Cil;
using NUnit.Framework.Internal;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerInputManage : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject playerPrefab2;
    [SerializeField] private Transform[] spawnPoints;
    private bool wasdjoined=false;
    private bool arrowsjoined=false;
    public int pAmount=0;
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
            if (Gamepad.current.rightTrigger.wasPressedThisFrame)
            {
                if(pAmount ==0)
                {
                var player =PlayerInput.Instantiate(playerPrefab,controlScheme: "Gamepad",pairWithDevice:gamePad);
                pAmount++;
                usedGamepads.Add(gamePad);
                }else if (pAmount ==1)
                {
                    var player =PlayerInput.Instantiate(playerPrefab2,controlScheme: "Gamepad",pairWithDevice:gamePad);
                    pAmount++;
                    usedGamepads.Add(gamePad);
                }
            }
        }



        if(Keyboard.current ==null) return;
        if(!wasdjoined && Keyboard.current.spaceKey.wasPressedThisFrame && pAmount < 2)
        {
            if(pAmount ==0)
            {
            var player =PlayerInput.Instantiate(playerPrefab,controlScheme: "Keyboard1",
            pairWithDevice: Keyboard.current);
            pAmount++;
            if (spawnPoints.Length > 0)
            {
                player.transform.position =spawnPoints[0].position;
            }
            wasdjoined=true;
            }
            else if(pAmount ==1)
            {
            var player =PlayerInput.Instantiate(playerPrefab2,controlScheme: "Keyboard1",
            pairWithDevice: Keyboard.current);
            pAmount++;
            if (spawnPoints.Length > 0)
            {
                player.transform.position =spawnPoints[0].position;
            }
            wasdjoined=true;
            }
        }

        if(!arrowsjoined && Keyboard.current.backspaceKey.wasPressedThisFrame && pAmount < 2)
        {
            if(pAmount ==0)
            {
            var player =PlayerInput.Instantiate(playerPrefab,controlScheme: "Keyboard2",
            pairWithDevice: Keyboard.current);
            pAmount++;
            if (spawnPoints.Length > 0)
            {
                player.transform.position =spawnPoints[0].position;
            }
            arrowsjoined=true;
            }
            else if(pAmount ==1)
            {
            var player =PlayerInput.Instantiate(playerPrefab2,controlScheme: "Keyboard2",
            pairWithDevice: Keyboard.current);
            pAmount++;
            if (spawnPoints.Length > 0)
            {
                player.transform.position =spawnPoints[0].position;
            }
            arrowsjoined=true;
            }
        }
    }
}

