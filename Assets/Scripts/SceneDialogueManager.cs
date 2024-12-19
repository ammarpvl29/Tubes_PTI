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
    public AudioClip textSound;     // Sound effect for typing
    public AudioClip voiceClip;     // Voice line for the dialogue
}

[System.Serializable]
public class SceneDialogue
{
    public List<DialogueLine> lines;
    public string nextSceneName;
}

[RequireComponent(typeof(AudioSource))]
public class SceneDialogueManager : MonoBehaviour
{
    [Header("Dialogue UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI dialogueText;
    public Image speakerImage;
    public Image backgroundImage;
    public AudioClip defaultTextSound;

    [Header("Audio Sources")]
    public AudioSource typingSoundSource;  // For typing sounds
    public AudioSource voiceSource;        // For voice clips

    [Header("Dialogue Configuration")]
    public List<SceneDialogue> dialogues;
    public float textSpeed = 0.05f;
    public float dialogueTransitionDelay = 1f;

    private Queue<DialogueLine> dialogueQueue;
    private bool isDialogueActive = false;
    private Coroutine typingCoroutine;
    private int currentDialogueIndex = 0;
    private string nextSceneToLoad;

    void Start()
    {
        // Setup audio sources if not assigned
        if (typingSoundSource == null)
        {
            typingSoundSource = gameObject.AddComponent<AudioSource>();
        }
        if (voiceSource == null)
        {
            voiceSource = gameObject.AddComponent<AudioSource>();
        }

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
        if (currentDialogueIndex < 0 || currentDialogueIndex >= dialogues.Count)
        {
            Debug.LogError($"Invalid dialogue index: {currentDialogueIndex}. Resetting to 0.");
            currentDialogueIndex = 0;
        }

        dialogueQueue = new Queue<DialogueLine>();
        isDialogueActive = true;

        SceneDialogue currentDialogue = dialogues[currentDialogueIndex];
        if (currentDialogue.lines == null || currentDialogue.lines.Count == 0)
        {
            Debug.LogError($"No dialogue lines in dialogue at index {currentDialogueIndex}");
            return;
        }

        nextSceneToLoad = currentDialogue.nextSceneName;

        foreach (DialogueLine line in currentDialogue.lines)
        {
            dialogueQueue.Enqueue(line);
        }

        dialoguePanel.SetActive(true);
        DisplayNextLine();
    }

    public void DisplayNextLine()
    {
        // Stop any currently playing voice clip
        if (voiceSource.isPlaying)
        {
            voiceSource.Stop();
        }

        if (dialogueQueue.Count == 0)
        {
            currentDialogueIndex++;
            if (currentDialogueIndex < dialogues.Count)
            {
                nextSceneToLoad = dialogues[currentDialogueIndex].nextSceneName;
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

        DialogueLine currentLine = dialogueQueue.Dequeue();
        speakerNameText.text = currentLine.speakerName;
        speakerImage.sprite = currentLine.speakerImage;

        // Play voice clip if available
        if (currentLine.voiceClip != null)
        {
            voiceSource.clip = currentLine.voiceClip;
            voiceSource.Play();
        }

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

            // Play typing sound effect
            if (currentLine.textSound != null || defaultTextSound != null)
            {
                AudioClip soundToPlay = currentLine.textSound ?? defaultTextSound;
                typingSoundSource.PlayOneShot(soundToPlay, 0.5f);  // Reduced volume for typing sounds
            }

            yield return new WaitForSeconds(textSpeed);
        }
    }

    IEnumerator EndDialogueAndTransition()
    {
        yield return new WaitForSeconds(dialogueTransitionDelay);
        dialoguePanel.SetActive(false);
        LoadNextScene();
    }

    void LoadNextScene()
    {
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
        if (isDialogueActive && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            DisplayNextLine();
        }
    }
}