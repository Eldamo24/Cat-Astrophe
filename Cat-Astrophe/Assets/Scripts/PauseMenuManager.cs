using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button pauseButton;

    private bool isPaused = false;

    void Start()
    {
        // Asegura que el menú empiece oculto
        pauseMenuUI.SetActive(false);

        // Conecta los botones
        continueButton.onClick.AddListener(ResumeGame);
        exitButton.onClick.AddListener(ExitGame);
    }

    void Update()
    {
        // En PC / Editor: tecla Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        pauseButton.enabled = false;
        isPaused = true;
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f; // Pausa todo el juego (menos UI)
        AudioListener.pause = true; // Pausa sonidos
    }

    public void ResumeGame()
    {
        pauseButton.enabled = true;
        isPaused = false;
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    public void ExitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}