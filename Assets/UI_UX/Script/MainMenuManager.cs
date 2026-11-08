using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void PlayGame()
    {
        // Load the game scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("HubScene");
    }
    
    public void OpenOptions()
    {
        // Load the options scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("OptionsScene");
    }
    
    public void CloseOptions()
    {
        // Load the main menu scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("MenuScene");
    }
    
    public void QuitGame()
    {
        Debug.Log("Le jeu se ferme..."); // Permet de voir dans la Console si le bouton marche dans l'Editor
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Arrête le mode Play directement dans Unity pour tester
#endif
    }
}
