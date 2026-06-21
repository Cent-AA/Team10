using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public string characterName = "Инженер";

    private CharacterDialogue data;

    void Start()
    {
        data = DialogueDatabase.Instance.GetCharacter(characterName);
    }

    public void OnDamaged()
    {
        string line = DialogueDatabase.Instance.GetRandomLine(data.onDamageLines);
        DialogueUI.Instance.ShowDialogue(data.characterName, line, data.portrait);
    }

    public void OnDeath()
    {
        string line = DialogueDatabase.Instance.GetRandomLine(data.onDeathLines);
        DialogueUI.Instance.ShowDialogue(data.characterName, line, data.portrait);
    }

    public void OnTurretPlaced()
    {
        string line = DialogueDatabase.Instance.GetRandomLine(data.onTurretPlacedLines);
        DialogueUI.Instance.ShowDialogue(data.characterName, line, data.portrait);
    }
}