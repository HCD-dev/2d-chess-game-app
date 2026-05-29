using UnityEngine;
using System.Collections;

public class PlayMoveSound : MonoBehaviour
{
    [Tooltip("Hamle sesi")]
    [SerializeField] private AudioClip moveClip;

    [Tooltip("Eðer boþ býrakýlýrsa bir AudioSource eklenir")]
    [SerializeField] private AudioSource audioSource;

    private AIController aiController;
    private Game gameController;
    private Coroutine initializationCoroutine;

    void Awake()
    {
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void OnEnable()
    {
        initializationCoroutine = StartCoroutine(FindControllersWithDelay());
    }

    void OnDisable()
    {
        if (initializationCoroutine != null)
            StopCoroutine(initializationCoroutine);

        if (aiController != null)
            aiController.OnMoveExecuted -= HandleMoveSound;

        // Game.cs event'i eklendiðinde burasý da temizlenecek
        if (gameController != null)
        {
            // Eðer Game.cs içine bir event yazarsan buraya ekleyebilirsin.
            // Ama þimdilik doðrudan Game.cs içinden tetiklemek daha pratik olacaksa aþaðýya göz at.
        }
    }

    IEnumerator FindControllersWithDelay()
    {
        yield return null;

        aiController = Object.FindAnyObjectByType<AIController>();
        gameController = Object.FindAnyObjectByType<Game>();

        if (aiController != null)
            aiController.OnMoveExecuted += HandleMoveSound;
    }

    private void HandleMoveSound(AIMoveEventArgs args)
    {
        PlaySound();
    }

   
    public void PlaySound()
    {
        if (moveClip == null || audioSource == null) return;
        audioSource.PlayOneShot(moveClip);
    }
}