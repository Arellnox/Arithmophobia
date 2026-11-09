using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject optionPanel;
    public GameObject hubPanel;

    private bool comingFromHub = false;

    public void OpenOptionsFromMenu()
    {
        comingFromHub = false;
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (optionPanel != null) optionPanel.SetActive(true);
    }

    public void OpenOptionsFromHub()
    {
        comingFromHub = true;
        if (hubPanel != null) hubPanel.SetActive(false);
        if (optionPanel != null) optionPanel.SetActive(true);
    }

    public void CloseOptions()
    {
        // Ferme les options
        if (optionPanel != null) optionPanel.SetActive(false);

        // Remet le bon panneau selon d'où on venait
        if (comingFromHub)
        {
            if (hubPanel != null) hubPanel.SetActive(true);
        }
        else
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        }
    }

    public void PlayGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainScene");
        //if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        //if (hubPanel != null) hubPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Le jeu se ferme...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}