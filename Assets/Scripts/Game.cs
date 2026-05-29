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
        if (currentPlayer == "white") currentPlayer = "black";
        else currentPlayer = "white";

        // Sýra gelen oyuncunun yasal hamlesi var mý kontrol et
        if (!HasLegalMoves(currentPlayer))
        {
            if (IsInCheck(currentPlayer))
            {
                // Þah altýnda ve hamlesi yoksa = MAT!
                string winner = (currentPlayer == "white") ? "black" : "white";
                Winner(winner);
            }
            else
            {
                // Þah altýnda deðil ama hamlesi de yoksa = BERABERLÝK (PAT)
                if (winnerText != null)
                {
                    winnerText.gameObject.SetActive(true);
                    winnerText.text = "Stalemate! It's a Draw.";
                }
                gameOver = true;
                if (restartButton != null) restartButton.gameObject.SetActive(true);
            }
        }
    }
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

    public void RestartGame()
    {
        SceneManager.LoadScene("Game");
    }
  

    public GameObject GetKing(string playerColor)
    {
        Chessman[] allPieces = Object.FindObjectsByType<Chessman>(FindObjectsSortMode.None);
        foreach (Chessman piece in allPieces)
        {
            string baseName = piece.name.EndsWith("_0") ? piece.name.Substring(0, piece.name.Length - 2) : piece.name;
            if (piece.player == playerColor && (baseName == "white_king" || baseName == "black_king"))
            {
                return piece.gameObject;
            }
        }
        return null;
    }

    public bool IsInCheck(string playerColor)
    {
        GameObject king = GetKing(playerColor);
        if (king == null) return false;

        int kingX = king.GetComponent<Chessman>().GetXBoard();
        int kingY = king.GetComponent<Chessman>().GetYBoard();

        // Sahnedeki tüm taþlarý tarayýp, rakip taþlarýn þahýn karesini tehdit edip etmediðine bakarýz
        Chessman[] allPieces = Object.FindObjectsByType<Chessman>(FindObjectsSortMode.None);
        foreach (Chessman piece in allPieces)
        {
            if (piece.player != playerColor)
            {
                // Þah durumunu kontrol ederken sonsuz döngüye girmemek için 
                // sadece taþlarýn hamle yapabileceði kareleri doðrudan simüle edeceðiz.
                if (CanPieceAttackSquare(piece, kingX, kingY))
                {
                    return true;
                }
            }
        }
        return false;
    }
    private bool CanPieceAttackSquare(Chessman piece, int targetX, int targetY)
    {
        // Taþýn normal þartlarda hareket kurallarýna göre o kareye ulaþýp ulaþamayacaðýný doðrular
        // Yapay zeka scriptindeki mantýk veya Chessman'deki kurallarla paralellik gösterir
        int x = piece.GetXBoard();
        int y = piece.GetYBoard();
        string baseName = piece.name.EndsWith("_0") ? piece.name.Substring(0, piece.name.Length - 2) : piece.name;

        switch (baseName)
        {
            case "white_pawn":
                return (targetY == y + 1 && Mathf.Abs(targetX - x) == 1);
            case "black_pawn":
                return (targetY == y - 1 && Mathf.Abs(targetX - x) == 1);
            case "white_knight":
            case "black_knight":
                int dx = Mathf.Abs(targetX - x);
                int dy = Mathf.Abs(targetY - y);
                return (dx == 1 && dy == 2) || (dx == 2 && dy == 1);
            case "white_king":
            case "black_king":
                return Mathf.Abs(targetX - x) <= 1 && Mathf.Abs(targetY - y) <= 1;
            // Rook, Bishop ve Queen için düz/çapraz hat kontrolü (arada taþ var mý bakarak)
            case "white_rook":
            case "black_rook":
                return CheckLineAttack(x, y, targetX, targetY, true, false);
            case "white_bishop":
            case "black_bishop":
                return CheckLineAttack(x, y, targetX, targetY, false, true);
            case "white_queen":
            case "black_queen":
                return CheckLineAttack(x, y, targetX, targetY, true, true);
        }
        return false;
    }





    private bool CheckLineAttack(int x, int y, int tx, int ty, bool allowStraight, bool allowDiagonal)
    {
        int dx = tx - x;
        int dy = ty - y;

        bool isStraight = (dx == 0 || dy == 0);
        bool isDiagonal = (Mathf.Abs(dx) == Mathf.Abs(dy));

        if (isStraight && !allowStraight) return false;
        if (isDiagonal && !allowDiagonal) return false;
        if (!isStraight && !isDiagonal) return false;

        int stepX = System.Math.Sign(dx);
        int stepY = System.Math.Sign(dy);

        int curX = x + stepX;
        int curY = y + stepY;

        while (curX != tx || curY != ty)
        {
            if (GetPosition(curX, curY) != null) return false; // Arada taþ var, hat kapalý
            curX += stepX;
            curY += stepY;
        }
        return true;
    }
    public bool SimulationLeavesKingInCheck(GameObject piece, int toX, int toY)
    {
        Chessman cm = piece.GetComponent<Chessman>();
        int fromX = cm.GetXBoard();
        int fromY = cm.GetYBoard();
        string playerColor = cm.player;

        // Hedef karedeki orijinal taþý sakla
        GameObject targetPiece = GetPosition(toX, toY);

        // Tahtayý geçici olarak yeni hamleye göre güncelle
        positions[fromX, fromY] = null;
        positions[toX, toY] = piece;
        cm.SetXBoard(toX);
        cm.SetYBoard(toY);
        if (targetPiece != null) targetPiece.SetActive(false); // Simülasyonda yok sayýlmasý için gizle

        // Bu sanal durumda þah altýnda mýyýz?
        bool inCheck = IsInCheck(playerColor);

        // Tahtayý ve taþ pozisyonlarýný eski haline geri yükle
        positions[fromX, fromY] = piece;
        positions[toX, toY] = targetPiece;
        cm.SetXBoard(fromX);
        cm.SetYBoard(fromY);
        if (targetPiece != null) targetPiece.SetActive(true);

        return inCheck;
    }
    public bool HasLegalMoves(string playerColor)
    {
        Chessman[] allPieces = Object.FindObjectsByType<Chessman>(FindObjectsSortMode.None);
        foreach (Chessman piece in allPieces)
        {
            if (piece.player == playerColor)
            {
                // Taþýn üretebildiði hamle plakalarýný toplayýp legal olan var mý bakacaðýz
                // Bu mantýðý tetiklemek için Chessman'e entegre ettiðimiz filtreyi kullanabiliriz.
                if (piece.HasAnyLegalMove())
                {
                    return true;
                }
            }
        }
        return false;
    }
}
