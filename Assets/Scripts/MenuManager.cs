using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Menu panels")]
    public GameObject creditsMenuPanel;

    [SerializeField] private Button creditsBackButton;

    private void Awake()
    {
        EnsureReferences();
        WireCreditsBackButton();
    }

    public void OpenCredits()
    {
        EnsureReferences();

        if (creditsMenuPanel != null)
            creditsMenuPanel.SetActive(true);
    }

    public void CloseCredits()
    {
        EnsureReferences();

        if (creditsMenuPanel != null)
            creditsMenuPanel.SetActive(false);
    }

    private void EnsureReferences()
    {
        if (creditsMenuPanel == null)
            creditsMenuPanel = FindInActiveScene("CreditsMenuPanel");

        if (creditsBackButton == null && creditsMenuPanel != null)
            creditsBackButton = FindCreditsBackButton();
    }

    private void WireCreditsBackButton()
    {
        if (creditsBackButton == null)
            return;

        creditsBackButton.interactable = true;
        creditsBackButton.onClick.RemoveListener(CloseCredits);
        creditsBackButton.onClick.AddListener(CloseCredits);

        Graphic targetGraphic = creditsBackButton.targetGraphic;
        if (targetGraphic == null)
        {
            targetGraphic = creditsBackButton.GetComponent<Graphic>();
            creditsBackButton.targetGraphic = targetGraphic;
        }

        if (targetGraphic != null)
            targetGraphic.raycastTarget = true;
    }

    private Button FindCreditsBackButton()
    {
        Transform namedBackButton = FindChildRecursive(creditsMenuPanel.transform, "BackButton");
        if (namedBackButton != null)
        {
            Button button = namedBackButton.GetComponent<Button>();
            if (button == null)
                button = namedBackButton.GetComponentInChildren<Button>(true);

            if (button != null)
                return button;
        }

        Button[] buttons = creditsMenuPanel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].name.IndexOf("back", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return buttons[i];
        }

        return buttons.Length == 1 ? buttons[0] : null;
    }

    private GameObject FindInActiveScene(string objectName)
    {
        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindChildRecursive(roots[i].transform, objectName);
            if (found != null)
                return found.gameObject;
        }

        return null;
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        if (parent.name == childName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            Transform found = FindChildRecursive(child, childName);
            if (found != null)
                return found;
        }

        return null;
    }
}
