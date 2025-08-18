using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueNode", menuName = "Dialogue/Dialogue Node")]
public class DialogueNode : ScriptableObject
{
    public string npcName; // The name of the NPC speaking this dialogue

    [TextArea(3, 10)]
    public string npcText; // The text the NPC will say

    public DialogueNode nextNode; // The next dialogue node in the sequence
    public bool endsConversation; // True if this node ends the conversation
}
