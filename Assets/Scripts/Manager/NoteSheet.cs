using UnityEngine;

// À poser sur un objet qui a un Collider : le joueur le lit avec E.
public class NoteSheet : MonoBehaviour
{
    public string title = "Fiche de note";

    [TextArea(5, 15)]
    public string content;

    [Header("Réaction du joueur (optionnelle)")]
    [Tooltip("Dite une seule fois, quand le joueur referme la fiche")]
    public NarrationLine reaction;

    [HideInInspector] public bool reactionPlayed;
}
