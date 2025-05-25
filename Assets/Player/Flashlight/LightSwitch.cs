using UnityEngine;

public class LightSwitch : MonoBehaviour
{
    [SerializeField] private GameObject lightObject;
    [SerializeField] private AudioSource switchSound;
    [SerializeField] private KeyCode toggleKey = KeyCode.F;

    void Update()
    {
        if (Input.GetKeyDown(toggleKey) && lightObject != null)
        {
            lightObject.SetActive(!lightObject.activeSelf);
            if (switchSound != null) switchSound.Play();
        }
    }
}