using TMPro;
using UnityEngine;

public class TutorialPanelManager : MonoBehaviour
{
    public TextMeshProUGUI infoText;

    public GameObject tutorialPanel;
    public GameObject tutorialButton;

    private void Start()
    {
    }

    public void ShowControls()
    {
    }

    public void ShowCombat()
    {
    }

    public void ShowTips()
    {
    }

    public void OpenTutorial()
    {
        tutorialPanel.SetActive(true);
        tutorialButton.SetActive(false);
    }

    public void CloseTutorial()
    {
        tutorialPanel.SetActive(false);
        tutorialButton.SetActive(true);
    }
}
