using TMPro;
using UnityEngine;

public class TutorialPanelManager : MonoBehaviour
{
    public TextMeshProUGUI infoText;

    public GameObject tutorialPanel;
    public GameObject tutorialButton;

    private void Start()
    {
        ShowControls();
    }

    public void ShowControls()
    {
        infoText.text =
@"<size=40><b>CONTROLS</b></size>

<color=#5FB3FF><size=50><b>Player 1</b></size></color>

W A S D - Move
Left Shift - Run


<color=#FFB347><size=50><b>Player 2</b></size></color>

↑ ↓ ← → - Move
Right Shift - Run";
    }

    public void ShowCombat()
    {
        infoText.text =
@"<size=40><b>COMBAT</b></size>

<color=#5FB3FF><size=45><b>Player 1</b></size></color>

Space - Light Attack
Q - Heavy Attack
R - Dash
F - Roll
C - Block

<color=#FFB347><size=45><b>Player 2</b></size></color>

Keypad 0 - Light Attack
Keypad 1 - Heavy Attack
Keypad 2 - Dash
Keypad 3 - Roll
Keypad 4 - Block";
    }

    public void ShowTips()
    {
        infoText.text =
@"<size=40><b>TIPS</b></size>

• Train alone or with a friend.

• Practice attacks and combos on the dummy.

• Improve movement and combat skills.";
    }

    public void OpenTutorial()
    {
        tutorialPanel.SetActive(true);
        tutorialButton.SetActive(false);
        ShowControls();
    }

    public void CloseTutorial()
    {
        tutorialPanel.SetActive(false);
        tutorialButton.SetActive(true);
    }
}