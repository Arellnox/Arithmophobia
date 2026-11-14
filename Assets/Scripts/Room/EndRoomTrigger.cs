using UnityEngine;

// À poser sur le rectangle (Collider "Is Trigger") de la salle de fin.
public class EndRoomTrigger : MonoBehaviour
{
    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        // Seul le joueur déclenche la fin, et une seule fois
        if (triggered || !other.CompareTag("Player")) return;
        triggered = true;

        if (EndSequence.Instance != null)
        {
            EndSequence.Instance.Play();
        }
        else
        {
            // Sécurité : sans EndSequence dans la scène, on garde l'ancien comportement
            Debug.LogWarning("Aucun EndSequence dans la scène : ouverture du menu pause à la place.");
            FirstPersonController player = other.GetComponentInParent<FirstPersonController>();
            if (player != null) player.PauseGame();
        }
    }
}