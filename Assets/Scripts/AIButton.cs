using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AIButton : MonoBehaviour
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
            Debug.LogWarning("[AIButton] TextMeshProUGUI bulunamadý. Etiket güncellenemeyecek.");

        if (AIManager.Instance != null)
            AIManager.Instance.OnAIChanged += HandleAIChanged;

        UpdateLabel();
    }

    // Butonun OnClick olayýna baðlayýn
    public void ToggleAI()
    {
        EnsureManager();
        AIManager.Instance.ToggleAI();
    }

    private void HandleAIChanged(bool enabled)
    {
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        var enabled = AIManager.Instance != null && AIManager.Instance.AIEnabled;
        string label = enabled ? "AI is On" : "AI is Off";

        if (tmpText != null)
            tmpText.text = label;
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
            AIManager.Instance.OnAIChanged -= HandleAIChanged;
    }
}
