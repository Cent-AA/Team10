using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance;

    [Header("UI References")]
    public GameObject dialoguePanel;
    public Image portraitImage;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;

    [Header("Settings")]
    public float displayDuration = 4f;
    public float typewriterSpeed = 0.03f;

    private Coroutine currentRoutine;

    void Awake()
    {
        Instance = this;
        dialoguePanel.SetActive(false);
    }

    public void ShowDialogue(string speakerName, string message, Sprite portrait)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(DisplayRoutine(speakerName, message, portrait));
    }

    IEnumerator DisplayRoutine(string speakerName, string message, Sprite portrait)
    {
        speakerNameText.text = speakerName;
        portraitImage.sprite = portrait;
        dialogueText.text = "";
        dialoguePanel.SetActive(true);

        // Эффект печатающейся машинки
        foreach (char c in message)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }

        yield return new WaitForSeconds(displayDuration);
        dialoguePanel.SetActive(false);
    }
}