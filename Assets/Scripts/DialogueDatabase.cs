using UnityEngine;

[System.Serializable]
public class CharacterDialogue
{
    public string characterName;
    public Sprite portrait;
    public string[] onDamageLines;
    public string[] onDeathLines;
    public string[] onTurretPlacedLines;
    public string[] onKillLines;
}

public class DialogueDatabase : MonoBehaviour
{
    public static DialogueDatabase Instance;
    public CharacterDialogue[] characters;

    void Awake() => Instance = this;

    public CharacterDialogue GetCharacter(string name)
    {
        foreach (var c in characters)
            if (c.characterName == name) return c;
        return null;
    }

    public string GetRandomLine(string[] lines)
    {
        if (lines == null || lines.Length == 0) return "";
        return lines[Random.Range(0, lines.Length)];
    }
}