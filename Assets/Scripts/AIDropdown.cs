using System.Linq;
using UnityEngine;
using TMPro;

public class AIDropdown : MonoBehaviour
{
    public TMP_Dropdown dropdown; // Inspector'a sürükleyin ya da çocukta otomatik bulunur.

    private void Start()
    {
        EnsureManager();

        if (dropdown == null)
            dropdown = GetComponentInChildren<TMP_Dropdown>();

        if (dropdown == null)
        {
            Debug.LogWarning("[AIDropdown] TMP_Dropdown bulunamadý. Lütfen Inspector'a atayýn.");
            return;
        }

        // Önceki dinleyicileri temizle ki çift tetiklenme olmasýn
        dropdown.onValueChanged.RemoveAllListeners();
        // Dinamik olarak runtime'da kodu baðlýyoruz (Inspector uyarýsý vermez)
        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);

        if (AIManager.Instance != null)
        {
            AIManager.Instance.OnAIChanged += HandleAIChanged;
            AIManager.Instance.OnEloChanged += HandleEloChanged;
        }

        SyncDropdownToManager();
    }

    private void OnDropdownValueChanged(int index)
    {
        if (dropdown == null || AIManager.Instance == null) return;

        string label = dropdown.options[index].text;
        Debug.Log($"[AIDropdown] Dropdown seçildi index={index} label='{label}'");
        AIManager.Instance.SetEloByIndex(index, label);
        Debug.Log($"[AIDropdown] AIManager.Elo now = {AIManager.Instance.Elo}");
    }

    private void HandleAIChanged(bool enabled)
    {
        Debug.Log($"[AIDropdown] HandleAIChanged: AIEnabled={enabled}");
        UpdateInteractable();
    }

    private void HandleEloChanged(int elo)
    {
        Debug.Log($"[AIDropdown] HandleEloChanged: new Elo={elo}");
        // Manager'daki Elo deðeri deðiþtiyse dropdown'da eþleþen indeksi seç
        if (dropdown == null) return;
        for (int i = 0; i < dropdown.options.Count; i++)
        {
            string digits = string.Concat(dropdown.options[i].text.Where(char.IsDigit));
            if (int.TryParse(digits, out int parsed) && parsed == elo)
            {
                dropdown.SetValueWithoutNotify(i);
                return;
            }
        }
    }

    private void UpdateInteractable()
    {
        if (dropdown != null)
            dropdown.interactable = AIManager.Instance != null && AIManager.Instance.AIEnabled;
    }

    private void SyncDropdownToManager()
    {
        if (dropdown == null || AIManager.Instance == null) return;
        // Manager.Elo ile eþleþen indeksi seç
        HandleEloChanged(AIManager.Instance.Elo);
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
            AIManager.Instance.OnEloChanged -= HandleEloChanged;
        }
    }
}
