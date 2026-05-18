using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AISideButton : MonoBehaviour
{
    // TextMeshPro kullanýmý; Inspector'da baðlamak istemezseniz otomatik arama yapýlýr
    public TextMeshProUGUI tmpText;

    private Button button;

    private void Start()
    {
        EnsureManager();
        button = GetComponent<Button>();

        // Eðer TMP inspector'da yoksa, çocuk nesnelerde ara
        if (tmpText == null)
            tmpText = GetComponentInChildren<TextMeshProUGUI>();

        if (tmpText == null)
            Debug.LogWarning("[AISideButton] TextMeshProUGUI bulunamadý. Etiket güncellenemeyecek.");

        if (AIManager.Instance != null)
        {
            AIManager.Instance.OnAIChanged += HandleAIChanged;
            AIManager.Instance.OnSideChanged += HandleSideChanged;
        }

        UpdateInteractable();
        UpdateLabel();
    }

    // Butonun OnClick olayýna baðlayýn
    public void ToggleSide()
    {
        EnsureManager();
        AIManager.Instance.ToggleSide();
    }

    private void HandleAIChanged(bool enabled)
    {
        UpdateInteractable();
        UpdateLabel();
    }

    private void HandleSideChanged(AIManager.AISide side)
    {
        UpdateLabel();
    }

    private void UpdateInteractable()
    {
        if (button != null)
            button.interactable = AIManager.Instance != null && AIManager.Instance.AIEnabled;
    }

    private void UpdateLabel()
    {
        if (AIManager.Instance == null) return;
        string side = AIManager.Instance.Side == AIManager.AISide.Black ? "Black" : "White";
        string label = "AI Side: " + side;
        if (tmpText != null) tmpText.text = label;
    }

    private void EnsureManager()
    {
        if (AIManager.Instance == null)
        {
            var go = new GameObject("AIManager");
            go.AddComponent<AIManager>();
        }
    }

    private void OnDestroy()
    {
        if (AIManager.Instance != null)
        {
            AIManager.Instance.OnAIChanged -= HandleAIChanged;
            AIManager.Instance.OnSideChanged -= HandleSideChanged;
        }
    }
}