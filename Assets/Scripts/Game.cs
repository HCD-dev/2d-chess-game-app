using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Game : MonoBehaviour
{

    public GameObject chestpiece;
    private GameObject[,] positions = new GameObject[8, 8];
    private GameObject[] playerBlack = new GameObject[16];
    private GameObject[] playerWhite = new GameObject[16];
    private int blackIndex = 0;
    private int whiteIndex = 0;
    private string currentPlayer = "white";
    private bool gameOver = false;

    // Inspector üzerinden atanacak TMP Text ve Restart Button referanslarý
    public TMP_Text winnerText;
    public Button restartButton;

    // Oyun bittiðinde devre dýþý býraktýðýmýz colider'leri saklamak için
    private readonly List<Collider2D> disabledColliders = new List<Collider2D>();

    void Start()
    {
        // Eðer Inspector'dan atanmýþlarsa baþlangýçta gizle (GameObject seviyesinde)
        if (winnerText != null) winnerText.gameObject.SetActive(false);
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(false);
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartGame);
            restartButton.interactable = false;
        }

        // Taþlarý oluþtur
        Create("white_rook_0", 0, 0);
        Create("white_knight_0", 1, 0);
        Create("white_bishop_0", 2, 0);
        Create("white_queen_0", 3, 0);
        Create("white_king_0", 4, 0);
        Create("white_bishop_0", 5, 0);
        Create("white_knight_0", 6, 0);
        Create("white_rook_0", 7, 0);

        for (int x = 0; x < 8; x++)
            Create("white_pawn_0", x, 1);

        Create("black_rook_0", 0, 7);
        Create("black_knight_0", 1, 7);
        Create("black_bishop_0", 2, 7);
        Create("black_queen_0", 3, 7);
        Create("black_king_0", 4, 7);
        Create("black_bishop_0", 5, 7);
        Create("black_knight_0", 6, 7);
        Create("black_rook_0", 7, 7);

        for (int x = 0; x < 8; x++)
            Create("black_pawn_0", x, 6);
    }

    private GameObject Create(string name, int x, int y)
    {
        if (chestpiece == null)
        {
            Debug.LogError("chestpiece prefab inspector'da atanmadý!");
            return null;
        }

        GameObject obj = Instantiate(chestpiece, new Vector3(x, y, 0f), Quaternion.identity);
        obj.name = name;
        obj.transform.SetParent(this.transform);

        // Board pozisyonuna kaydet
        if (x >= 0 && x < 8 && y >= 0 && y < 8)
            positions[x, y] = obj;
        else
            Debug.LogWarning($"Create: koordinatlar sýnýr dýþýnda: {x},{y}");

        // Oyuncu dizilerine kaydet
        if (name.StartsWith("white_"))
        {
            if (whiteIndex < playerWhite.Length) playerWhite[whiteIndex++] = obj;
        }
        else if (name.StartsWith("black_"))
        {
            if (blackIndex < playerBlack.Length) playerBlack[blackIndex++] = obj;
        }

        Chessman cm = obj.GetComponent<Chessman>();
        if (cm != null)
        {
            cm.SetXBoard(x);
            cm.SetYBoard(y);
            cm.Activate();
        }
        else
        {
            Debug.LogWarning("Oluþturulan prefab içinde Chessman bileþeni bulunamadý: " + name);
        }

        return obj;
    }

    public void SetPosition(GameObject obj)
    {
        if (obj == null)
        {
            Debug.LogWarning("SetPosition çaðrýldý fakat obj null.");
            return;
        }

        Chessman cm = obj.GetComponent<Chessman>();
        if (cm == null)
        {
            Debug.LogWarning("SetPosition: obj üzerinde Chessman yok: " + obj.name);
            return;
        }

        int x = cm.GetXBoard();
        int y = cm.GetYBoard();

        if (x < 0 || x >= 8 || y < 0 || y >= 8)
        {
            Debug.LogWarning($"SetPosition: geçersiz koordinat {x},{y} için obje {obj.name}");
            return;
        }

        positions[x, y] = obj;
    }
    public void SetPositionEmpty(int x, int y)
    {
        positions[x, y] = null;
    }
    public GameObject GetPosition(int x, int y)
    { return positions[x, y]; }

    public bool PositionOnBoard(int x, int y)
    {
        if (x < 0 || y < 0 || x >= positions.GetLength(0) || y >= positions.GetLength(1)) return false;
        return true;
    }

    public string GetCurrentPlayer()
    {
        return currentPlayer;
    }

    public bool IsGameOver()
    {
        return gameOver;
    }

    public void NextTurn()
    {
        if (currentPlayer == "white")
            currentPlayer = "black";
        else
            currentPlayer = "white";
    }

    // Update artýk fare ile restart kontrolü yapmýyor
    public void Update()
    {
    }

    // Oyun alanýndaki 2D collider'leri devre dýþý býrakýr (UI'nýn týklanmasýný garanti eder)
    private void DisableBoardInteraction()
    {
        disabledColliders.Clear();

        // positions matrisindeki taþlarýn collider'lerini kapat
        for (int x = 0; x < positions.GetLength(0); x++)
        {
            for (int y = 0; y < positions.GetLength(1); y++)
            {
                var obj = positions[x, y];
                if (obj == null) continue;
                var col = obj.GetComponent<Collider2D>();
                if (col != null && col.enabled)
                {
                    col.enabled = false;
                    disabledColliders.Add(col);
                }
            }
        }

        // Halihazýrda sahnede olan MovePlate'lerin collider'lerini kapat
        var movePlates = GameObject.FindGameObjectsWithTag("MovePlate");
        foreach (var mp in movePlates)
        {
            var col = mp.GetComponent<Collider2D>();
            if (col != null && col.enabled)
            {
                col.enabled = false;
                disabledColliders.Add(col);
            }
        }
    }

    // (isteðe baðlý) Restart öncesi tekrar aktif etmek isterseniz kullanabilirsiniz
    private void RestoreBoardInteraction()
    {
        foreach (var col in disabledColliders)
        {
            if (col != null) col.enabled = true;
        }
        disabledColliders.Clear();
    }

    public void Winner(string playerWinner)
    {
        gameOver = true;
        Debug.Log(playerWinner + " wins!");

        // Önce board týklamalarýný devre dýþý býrak
        DisableBoardInteraction();

        // Inspector'dan atanmýþ TMP_Text'leri aktif et ve metni ayarla
        if (winnerText != null)
        {
            winnerText.gameObject.SetActive(true);
            winnerText.text = playerWinner + " is the winner!";
            // UI'nýn üstte olduðundan emin olun
            var canvas = winnerText.GetComponentInParent<Canvas>();
            if (canvas != null) canvas.sortingOrder = 100;
        }
        else
        {
            Debug.LogWarning("WinnerText TMP_Text Inspector'da atanmadý.");
        }

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
            restartButton.interactable = true;

            // Butonu üstte göster
            var canvasBtn = restartButton.GetComponentInParent<Canvas>();
            if (canvasBtn != null) canvasBtn.sortingOrder = 101;
            restartButton.transform.SetAsLastSibling();

            // Seçili yap (klavye/kontrast için)
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
            }
        }
        else
        {
            Debug.LogWarning("RestartButton Inspector'da atanmadý.");
        }
    }

    // Inspector veya Button tarafýndan çaðrýlacak yeniden baþlatma metodu
    public void RestartGame()
    {
        // (isteðe baðlý) RestoreBoardInteraction(); // gerek yok, sahne reload edecek
        SceneManager.LoadScene("Game");
    }
}
