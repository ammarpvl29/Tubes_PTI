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
    public AudioClip textSound; // Sound effect for this dialogue line
}

[System.Serializable]
public class SceneDialogue
{
    public List<DialogueLine> lines;
    public string nextSceneName; // Scene to load after dialogue
}

[RequireComponent(typeof(AudioSource))]
public class SceneDialogueManager : MonoBehaviour
{
    [Header("Dialogue UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public Image speakerImage;
    public Image backgroundImage; // Optional background for dialogue
    public AudioClip defaultTextSound; // Fallback sound if no individual sound

    [Header("Dialogue Configuration")]
    public List<SceneDialogue> dialogues; // List of all dialogues
    public float textSpeed = 0.05f;
    public float dialogueTransitionDelay = 1f; // Delay before scene transition

    private AudioSource audioSource;
    private Queue<DialogueLine> dialogueQueue;
    private bool isDialogueActive = false;
    private Coroutine typingCoroutine;
    private int currentDialogueIndex = 0;
    private string nextSceneToLoad; // Store the next scene name separately

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        // Ensure dialogue panel is hidden at start
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        // Check if dialogues exist before starting
        if (dialogues != null && dialogues.Count > 0)
        {
            StartDialogues();
        }
        else
        {
            Debug.LogWarning("No dialogues configured in SceneDialogueManager!");
        }
    }

    public void StartDialogues()
    {
        // Validate current dialogue index
        if (currentDialogueIndex < 0 || currentDialogueIndex >= dialogues.Count)
        {
            Debug.LogError($"Invalid dialogue index: {currentDialogueIndex}. Resetting to 0.");
            currentDialogueIndex = 0;
        }

        // Reset dialogue state
        dialogueQueue = new Queue<DialogueLine>();
        isDialogueActive = true;

        // Validate current dialogue's lines
        SceneDialogue currentDialogue = dialogues[currentDialogueIndex];
        if (currentDialogue.lines == null || currentDialogue.lines.Count == 0)
        {
            Debug.LogError($"No dialogue lines in dialogue at index {currentDialogueIndex}");
            return;
        }

        // Store the next scene name for the current dialogue set
        nextSceneToLoad = currentDialogue.nextSceneName;

        // Enqueue all dialogue lines from the first dialogue individually
        foreach (DialogueLine line in currentDialogue.lines)
        {
            dialogueQueue.Enqueue(line);
        }

        // Show dialogue panel
        dialoguePanel.SetActive(true);

        // Start displaying the first dialogue line
        DisplayNextLine();
    }

    public void DisplayNextLine()
    {
        // Check if any lines remain in the current dialogue
        if (dialogueQueue.Count == 0)
        {
            // Move to the next dialogue, if available
            currentDialogueIndex++;
            if (currentDialogueIndex < dialogues.Count)
            {
                // Update next scene name
                nextSceneToLoad = dialogues[currentDialogueIndex].nextSceneName;

                // Enqueue lines for the next dialogue
                foreach (DialogueLine line in dialogues[currentDialogueIndex].lines)
                {
                    dialogueQueue.Enqueue(line);
                }
            }
            else
            {
                StartCoroutine(EndDialogueAndTransition());
                return;
            }
        }

        // Get next dialogue line and display it
        DialogueLine currentLine = dialogueQueue.Dequeue();
        speakerNameText.text = currentLine.speakerName;
        speakerImage.sprite = currentLine.speakerImage;

        // Start typing effect
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(currentLine));
    }

    IEnumerator TypeText(DialogueLine currentLine)
    {
        dialogueText.text = "";
        foreach (char letter in currentLine.dialogueText.ToCharArray())
        {
            dialogueText.text += letter;

            // Play sound effect
            AudioClip soundToPlay = currentLine.textSound ?? defaultTextSound;
            if (soundToPlay != null)
            {
                audioSource.PlayOneShot(soundToPlay, 1f);
            }

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
        // Load the next scene specified in the last dialogue configuration
        if (!string.IsNullOrEmpty(nextSceneToLoad))
        {
            SceneManager.LoadScene(nextSceneToLoad);
        }
        else
        {
            Debug.LogWarning("No next scene specified in the dialogue configuration!");
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