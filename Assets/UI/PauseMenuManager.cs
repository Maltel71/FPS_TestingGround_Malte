using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public Canvas pauseMenuCanvas;
    public GameObject pauseMenuPanel;
    public Button resumeButton;
    public Button exitButton;
    public Slider volumeSlider;

    [Header("Audio")]
    public AudioSource uiAudioSource;
    public AudioClip buttonHoverSound;
    public AudioClip buttonClickSound;

    [Header("Animation Settings")]
    public float buttonScaleAmount = 1.1f;
    public float scaleAnimationSpeed = 10f;

    private bool isPaused = false;
    private bool wasMouseLocked = false;

    void Start()
    {
        // Initialize pause menu
        if (pauseMenuCanvas != null)
            pauseMenuCanvas.gameObject.SetActive(false);

        // Set up button listeners
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(CloseOptionsMenu);
            AddButtonHoverEffects(resumeButton);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(ExitGame);
            AddButtonHoverEffects(exitButton);
        }

        // Set up volume slider
        if (volumeSlider != null)
        {
            // Load saved volume or default to 1
            float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            volumeSlider.value = savedVolume;
            AudioListener.volume = savedVolume;

            // Add listener for volume changes
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        // Find UI audio source if not assigned
        if (uiAudioSource == null)
            uiAudioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // Toggle options menu with ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                CloseOptionsMenu();
            else
                OpenOptionsMenu();
        }
    }

    void OpenOptionsMenu()
    {
        isPaused = true;

        // Show options menu
        if (pauseMenuCanvas != null)
            pauseMenuCanvas.gameObject.SetActive(true);

        // Store current cursor state and unlock cursor
        wasMouseLocked = Cursor.lockState == CursorLockMode.Locked;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Don't pause game time - let the game continue running

        Debug.Log("Options Menu Opened");
    }

    public void CloseOptionsMenu()
    {
        isPaused = false;

        // Hide options menu
        if (pauseMenuCanvas != null)
            pauseMenuCanvas.gameObject.SetActive(false);

        // Restore cursor state
        if (wasMouseLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // No need to resume time since we never paused it

        // Play button click sound
        PlayButtonClickSound();

        Debug.Log("Options Menu Closed");
    }

    public void ExitGame()
    {
        // Play button click sound
        PlayButtonClickSound();

        Debug.Log("Exiting Game");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    void OnVolumeChanged(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.Save();
    }

    void AddButtonHoverEffects(Button button)
    {
        if (button == null) return;

        // Store original scale
        Vector3 originalScale = button.transform.localScale;

        // Create hover effect component
        ButtonHoverEffect hoverEffect = button.gameObject.GetComponent<ButtonHoverEffect>();
        if (hoverEffect == null)
        {
            hoverEffect = button.gameObject.AddComponent<ButtonHoverEffect>();
        }

        // Set up hover effect parameters
        hoverEffect.Initialize(originalScale, buttonScaleAmount, scaleAnimationSpeed, this);
    }

    public void PlayButtonHoverSound()
    {
        if (uiAudioSource != null && buttonHoverSound != null)
        {
            uiAudioSource.PlayOneShot(buttonHoverSound);
        }
    }

    public void PlayButtonClickSound()
    {
        if (uiAudioSource != null && buttonClickSound != null)
        {
            uiAudioSource.PlayOneShot(buttonClickSound);
        }
    }

    // Public getter for other systems
    public bool IsOptionsMenuOpen() => isPaused;
}