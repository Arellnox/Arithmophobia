using System.Collections;
using TMPro;
using UnityEngine;

// Une réplique : un texte (sous-titre) et, si on l'a, une voix enregistrée
[System.Serializable]
public class NarrationLine
{
    [TextArea(1, 3)] public string text;
    public AudioClip clip;
}

public class NarrationManager : MonoBehaviour
{
    public static NarrationManager Instance { get; private set; }

    [Header("Voix et sous-titres")]
    [Tooltip("AudioSource en 2D (Spatial Blend = 0), Play On Awake décoché")]
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [Tooltip("Durée minimale d'affichage d'un sous-titre sans voix")]
    [SerializeField] private float minSubtitleTime = 2f;
    [Tooltip("Durée ajoutée par caractère quand il n'y a pas de voix")]
    [SerializeField] private float secondsPerCharacter = 0.05f;

    [Header("Fiches de note")]
    [SerializeField] private GameObject notePanel;
    [SerializeField] private TextMeshProUGUI noteTitleText;
    [SerializeField] private TextMeshProUGUI noteBodyText;

    [Header("Réplique du début de partie")]
    [SerializeField] private NarrationLine introLine;
    [SerializeField] private float introDelay = 2f;

    private Coroutine subtitleRoutine;
    private NoteSheet currentNote;

    public bool IsNoteOpen => notePanel != null && notePanel.activeSelf;

    void Awake()
    {
        Instance = this;
        if (subtitleText != null) subtitleText.text = "";
        if (notePanel != null) notePanel.SetActive(false);
    }

    void Start()
    {
        if (introLine != null && (!string.IsNullOrEmpty(introLine.text) || introLine.clip != null))
        {
            StartCoroutine(PlayIntro());
        }
    }

    IEnumerator PlayIntro()
    {
        yield return new WaitForSeconds(introDelay);
        Say(introLine);
    }

    // --- Répliques ---------------------------------------------------------

    public void Say(NarrationLine line)
    {
        if (line == null) return;
        if (string.IsNullOrEmpty(line.text) && line.clip == null) return;

        if (voiceSource != null)
        {
            voiceSource.Stop();
            if (line.clip != null)
            {
                voiceSource.clip = line.clip;
                voiceSource.Play();
            }
        }

        if (subtitleRoutine != null) StopCoroutine(subtitleRoutine);
        subtitleRoutine = StartCoroutine(ShowSubtitle(line));
    }

    IEnumerator ShowSubtitle(NarrationLine line)
    {
        string text = line.text ?? "";
        if (subtitleText != null) subtitleText.text = text;

        float duration = line.clip != null
            ? line.clip.length + 0.5f
            : Mathf.Max(minSubtitleTime, text.Length * secondsPerCharacter);

        yield return new WaitForSeconds(duration);

        if (subtitleText != null) subtitleText.text = "";
        subtitleRoutine = null;
    }

    // --- Fiches de note ----------------------------------------------------

    public void ShowNote(NoteSheet note)
    {
        if (notePanel == null || note == null) return;

        currentNote = note;
        if (noteTitleText != null) noteTitleText.text = note.title;
        if (noteBodyText != null) noteBodyText.text = note.content;
        notePanel.SetActive(true);
    }

    public void HideNote()
    {
        if (notePanel != null) notePanel.SetActive(false);

        // Réaction du joueur à la fermeture de la fiche (une seule fois)
        if (currentNote != null && !currentNote.reactionPlayed)
        {
            currentNote.reactionPlayed = true;
            Say(currentNote.reaction);
        }
        currentNote = null;
    }
}
