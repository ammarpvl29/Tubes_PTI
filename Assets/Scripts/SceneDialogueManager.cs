using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class DialogueLine
{
    public string speakerName;
    [TextArea(3, 10)]
    public string dialogueText;
    public Sprite speakerImage;
}

[System.Serializable]
public class SceneDialogue
{
    public List<DialogueLine> lines;
    public string nextSceneName; // Scene to load after dialogue
}

public class SceneDialogueManager : MonoBehaviour
{
    [Header("Dialogue UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public Image speakerImage;
    public Image backgroundImage; // Optional background for dialogue

    [Header("Dialogue Configuration")]
    public SceneDialogue introDialogue; // Dialogue before Game1 scene
    public float textSpeed = 0.05f;
    public float dialogueTransitionDelay = 1f; // Delay before scene transition

    private Queue<DialogueLine> dialogueQueue;
    private bool isDialogueActive = false;
    private Coroutine typingCoroutine;

    void Start()
    {
        // Ensure dialogue panel is hidden at start
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        // Automatically start dialogue if configured
        StartIntroDialogue();
    }

    public void StartIntroDialogue()
    {
        // Check if we have a configured dialogue
        if (introDialogue == null || introDialogue.lines.Count == 0)
        {
            LoadNextScene();
            return;
        }

        // Reset and prepare dialogue queue
        dialogueQueue = new Queue<DialogueLine>(introDialogue.lines);
        isDialogueActive = true;

        // Show dialogue panel
        dialoguePanel.SetActive(true);

        // Start displaying first dialogue line
        DisplayNextLine();
    }

    public void DisplayNextLine()
    {
        // Check if any lines remain
        if (dialogueQueue.Count == 0)
        {
            StartCoroutine(EndDialogueAndTransition());
            return;
        }

        // Get next dialogue line
        DialogueLine currentLine = dialogueQueue.Dequeue();

        // Stop any existing typing coroutine
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        // Update UI
        speakerNameText.text = currentLine.speakerName;
        speakerImage.sprite = currentLine.speakerImage;

        // Start typing effect
        typingCoroutine = StartCoroutine(TypeText(currentLine.dialogueText));
    }

    IEnumerator TypeText(string fullText)
    {
        dialogueText.text = "";
        foreach (char letter in fullText.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(textSpeed);
        }
    }

    IEnumerator EndDialogueAndTransition()
    {
        // Wait a moment after last dialogue line
        yield return new WaitForSeconds(dialogueTransitionDelay);

        // Hide dialogue panel
        dialoguePanel.SetActive(false);

        // Load next scene
        LoadNextScene();
    }

    void LoadNextScene()
    {
        // Load the next scene specified in the dialogue configuration
        if (!string.IsNullOrEmpty(introDialogue.nextSceneName))
        {
            SceneManager.LoadScene(introDialogue.nextSceneName);
        }
        else
        {
            Debug.LogWarning("No next scene specified in dialogue configuration!");
        }
    }

    void Update()
    {
        // Allow player to progress dialogue with spacebar or mouse click
        if (isDialogueActive && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            DisplayNextLine();
        }
    }
}