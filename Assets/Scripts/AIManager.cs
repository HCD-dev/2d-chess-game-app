using System;
using UnityEngine;

public class AIManager : MonoBehaviour
{
    public static AIManager Instance { get; private set; }

    public enum AISide { White, Black }

    public bool AIEnabled { get; private set; } = false;
    public AISide Side { get; private set; } = AISide.Black; // baþlangýç: black

    // ELO deðeri (ör. 500, 1000, 1500)
    public int Elo { get; private set; } = 500;

    public Action<bool> OnAIChanged;
    public Action<AISide> OnSideChanged;
    public Action<int> OnEloChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetAIEnabled(bool enabled)
    {
        if (AIEnabled == enabled) return;
        AIEnabled = enabled;
        OnAIChanged?.Invoke(AIEnabled);
    }

    public void ToggleAI()
    {
        SetAIEnabled(!AIEnabled);
    }

    public void SetSide(AISide side)
    {
        if (Side == side) return;
        Side = side;
        OnSideChanged?.Invoke(Side);
    }

    public void ToggleSide()
    {
        SetSide(Side == AISide.Black ? AISide.White : AISide.Black);
    }

    public void SetElo(int elo)
    {
        if (Elo == elo) return;
        Elo = elo;
        OnEloChanged?.Invoke(Elo);
    }

    // Eðer Dropdown indeksine göre set etmek isterseniz kullanýn.
    // mapping dropdown etiketinden sayý çekilerek yapýlabilir; burada basit yardýmcý metot var.
    public void SetEloByIndex(int index, string optionLabel)
    {
        // optionLabel örn: "500 ELO BOT" -> 500
        string digits = System.String.Concat(Array.FindAll(optionLabel.ToCharArray(), char.IsDigit));
        if (int.TryParse(digits, out int parsed))
        {
            SetElo(parsed);
        }
        else
        {
            Debug.LogWarning($"[AIManager] ELO parse edilemedi: '{optionLabel}'");
        }
    }
}
