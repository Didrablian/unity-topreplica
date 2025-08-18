using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class NPCDialogue : MonoBehaviour
{
    public DialogueNode startDialogue; // The first dialogue node for this NPC
    public float interactionRange = 3f; // Range within which player can interact

    [Header("UI Elements")]
    public GameObject dialoguePanel; // Assign your NPC_Dialog_Panel here
    public TextMeshProUGUI npcNameText; // Assign the NPC Name TextMeshPro
    public TextMeshProUGUI dialogueText; // Assign the Dialogue TextMeshPro
    // Removed: public GameObject playerResponseButtonPrefab; // Removed: No more button prefabs
    public TextMeshProUGUI tradeText; // Assign the Trade TextMeshProUGUI
    public TextMeshProUGUI questText; // Assign the Quest TextMeshProUGUI

    // Removed: public GameObject playerResponseTextPrefab; // No longer needed for dynamic responses
    // Removed: public Transform playerResponseButtonParent; // No longer needed for dynamic responses

    private DialogueNode currentDialogueNode;
    private TopDownControls playerControls; // Reference to player's controls
    private GameObject playerObject; // Reference to the player GameObject
    private bool isInDialogue = false;

    void Start()
    {
        // Find the player object with TopDownControls script
        playerObject = GameObject.FindGameObjectWithTag("Player"); 
        if (playerObject != null)
        {
            playerControls = playerObject.GetComponent<TopDownControls>();
            if (playerControls == null)
            {
                Debug.LogError("TopDownControls component not found on player object.");
            }
        }
        else
        {
            Debug.LogError("Player GameObject not found. Ensure player has 'Player' tag.");
        }

        if (dialoguePanel != null) dialoguePanel.SetActive(false); // Ensure dialogue panel is hidden at start
        if (tradeText != null) tradeText.gameObject.SetActive(false); // Hide trade text at start
        if (questText != null) questText.gameObject.SetActive(false); // Hide quest text at start

        // Add EventTriggers for Trade and Quest texts
        if (tradeText != null)
        {
            EventTrigger tradeTrigger = tradeText.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener((eventData) => { OnTradeClicked(); });
            tradeTrigger.triggers.Add(entry);
            tradeText.text = "<u>Trade</u>"; // Underline the text
        }

        if (questText != null)
        {
            EventTrigger questTrigger = questText.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener((eventData) => { OnQuestClicked(); });
            questTrigger.triggers.Add(entry);
            questText.text = "<u>Quest</u>"; // Underline the text
        }
    }

    // Re-introducing Update method for continuous range check
    void Update()
    {
        if (isInDialogue && playerObject != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerObject.transform.position);
            if (distanceToPlayer > interactionRange)
            {
                EndDialogue(); // End dialogue if player moves out of range
            }
        }
    }

    public void StartDialogue()
    {
        isInDialogue = true;
        if (dialoguePanel != null) dialoguePanel.SetActive(true); // Show dialogue panel
        if (playerControls != null) playerControls.SetInputEnabled(false); // Disable player movement/combat input

        currentDialogueNode = startDialogue;
        DisplayCurrentDialogueNode();
    }

    void DisplayCurrentDialogueNode()
    {
        if (currentDialogueNode == null)
        {
            EndDialogue();
            return;
        }

        npcNameText.text = currentDialogueNode.npcName; // Use the name from the DialogueNode asset
        dialogueText.text = currentDialogueNode.npcText;

        // Hide Trade/Quest buttons by default, show only if NPC has them based on a future flag
        // For now, they are always active once dialogue starts.
        if (tradeText != null) tradeText.gameObject.SetActive(true);
        if (questText != null) questText.gameObject.SetActive(true);
    }

    void AdvanceDialogue()
    {
        if (currentDialogueNode != null && currentDialogueNode.nextNode != null && !currentDialogueNode.endsConversation)
        {
            currentDialogueNode = currentDialogueNode.nextNode;
            DisplayCurrentDialogueNode();
        }
        else
        {
            EndDialogue();
        }
    }

    void OnTradeClicked()
    {
        Debug.Log("Opening Trade Window..."); // Placeholder
        // Call your Trade UI logic here
        EndDialogue(); // End dialogue when opening trade
    }

    void OnQuestClicked()
    {
        Debug.Log("Opening Quest Window/Giving Quest..."); // Placeholder
        // Call your Quest UI logic here
        EndDialogue(); // End dialogue when dealing with quest
    }

    void EndDialogue()
    {
        isInDialogue = false;
        if (dialoguePanel != null) dialoguePanel.SetActive(false); // Hide dialogue panel
        if (tradeText != null) tradeText.gameObject.SetActive(false); // Hide trade text
        if (questText != null) questText.gameObject.SetActive(false); // Hide quest text
        if (playerControls != null) playerControls.SetInputEnabled(true); // Re-enable player input
    }
}
