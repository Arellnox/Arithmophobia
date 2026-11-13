using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Séquence de fin : le jeu se fige, le son s'éteint, l'écran passe au noir,
// la pierre tombale apparaît, puis retour au menu.
// À placer sur un GameObject vide ACTIF de la scène principale.
public class EndSequence : MonoBehaviour
{
    public static EndSequence Instance { get; private set; }

    [Header("Écrans (un CanvasGroup chacun)")]
    [Tooltip("Image noire plein écran")]
    [SerializeField] private CanvasGroup blackScreen;
    [Tooltip("Parent de l'image de la pierre tombale (doit être sous BlackScreen dans la Hierarchy)")]
    [SerializeField] private CanvasGroup tombstoneScreen;

    [Header("Timings (en secondes)")]
    [Tooltip("Fondu vers le noir, son compris. Mettre 0 pour un cut sec")]
    [SerializeField] private float fadeToBlackTime = 2f;
    [Tooltip("Silence dans le noir avant l'apparition de l'image")]
    [SerializeField] private float blackHoldTime = 1.5f;
    [SerializeField] private float tombstoneFadeInTime = 1.5f;
    [Tooltip("Temps pendant lequel l'écran de fin reste affiché")]
    [SerializeField] private float tombstoneHoldTime = 5f;
    [Tooltip("Fondu vers le noir avant le retour au menu")]
    [SerializeField] private float fadeOutTime = 1f;

    [Header("Retour au menu")]
    [Tooltip("Nom de la scène du menu (elle doit être dans les Build Profiles). Vide = recharge la scène actuelle")]
    [SerializeField] private string menuSceneName = "";

    private bool isPlaying;

    void Awake()
    {
        Instance = this;
        Init(blackScreen);
        Init(tombstoneScreen);
    }

    // Les écrans restent invisibles et ne bloquent jamais les clics (boutons du menu)
    private static void Init(CanvasGroup group)
    {
        if (group == null) return;
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    public void Play()
    {
        if (isPlaying) return;
        isPlaying = true;
        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        // 1. Le joueur ne peut plus rien faire
        FirstPersonController player = FindFirstObjectByType<FirstPersonController>();
        if (player != null) player.FreezeForEnding();

        // 2. Cut : l'image passe au noir et le son s'éteint en même temps
        yield return Fade(blackScreen, 0f, 1f, fadeToBlackTime, true);
        AudioListener.volume = 0f;

        // 3. Silence complet dans le noir
        yield return new WaitForSeconds(blackHoldTime);

        // 4. La pierre tombale apparaît (toujours sans son)
        yield return Fade(tombstoneScreen, 0f, 1f, tombstoneFadeInTime, false);
        yield return new WaitForSeconds(tombstoneHoldTime);

        // 5. Fondu vers le noir puis retour au menu
        yield return Fade(tombstoneScreen, 1f, 0f, fadeOutTime, false);
        ReturnToMenu();
    }

    // Fait varier l'alpha d'un CanvasGroup. Si fadeAudio : le volume global suit (1 -> 0)
    private IEnumerator Fade(CanvasGroup group, float from, float to, float duration, bool fadeAudio)
    {
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;
            if (group != null) group.alpha = Mathf.Lerp(from, to, k);
            if (fadeAudio) AudioListener.volume = 1f - k;
            yield return null;
        }

        if (group != null) group.alpha = to;
        if (fadeAudio) AudioListener.volume = 0f;
    }

    private void ReturnToMenu()
    {
        // Le volume est global : il faut le rétablir pour que le menu ait du son
        AudioListener.volume = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        string scene = string.IsNullOrEmpty(menuSceneName)
            ? SceneManager.GetActiveScene().name
            : menuSceneName;

        SceneManager.LoadScene(scene);
    }
}
