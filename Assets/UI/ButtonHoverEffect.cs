using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalScale;
    private Vector3 targetScale;
    private float scaleAmount = 1.1f;
    private float animationSpeed = 10f;
    private bool isHovering = false;
    private PauseMenuManager pauseMenuManager;

    public void Initialize(Vector3 originalScale, float scaleAmount, float animationSpeed, PauseMenuManager manager)
    {
        this.originalScale = originalScale;
        this.scaleAmount = scaleAmount;
        this.animationSpeed = animationSpeed;
        this.pauseMenuManager = manager;
        this.targetScale = originalScale;
    }

    void Update()
    {
        // Smoothly animate to target scale (using normal delta time since game isn't paused)
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, animationSpeed * Time.deltaTime);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        targetScale = originalScale * scaleAmount;

        // Play hover sound
        if (pauseMenuManager != null)
        {
            pauseMenuManager.PlayButtonHoverSound();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        targetScale = originalScale;
    }
}