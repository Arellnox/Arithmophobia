using UnityEngine;

public class EndRoomTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Détecte si c'est le joueur qui touche le rectangle
        if (other.CompareTag("Player"))
        {
            Debug.Log("Fin de jeu atteinte ! Return au menu...");

            // Récupère le controller du joueur et ouvre le menu/pause
            FirstPersonController player = other.GetComponent<FirstPersonController>();
            if (player != null)
            {
                player.PauseGame(); // Ouvre l'optionPanel et débloque la souris
                Debug.Log("Menu ouvert !");
            }
        }
    }
}
