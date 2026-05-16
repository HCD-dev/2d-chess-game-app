using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // White back rank (y = 0)
        Create("white_rook", 0, 0);
        Create("white_knight", 1, 0);
        Create("white_bishop", 2, 0);
        Create("white_queen", 3, 0);
        Create("white_king", 4, 0);
        Create("white_bishop", 5, 0);
        Create("white_knight", 6, 0);
        Create("white_rook", 7, 0);

        // White pawns (y = 1)
        for (int x = 0; x < 8; x++)
            Create("white_pawn", x, 1);

        // Black back rank (y = 7)
        Create("black_rook", 0, 7);
        Create("black_knight", 1, 7);
        Create("black_bishop", 2, 7);
        Create("black_queen", 3, 7);
        Create("black_king", 4, 7);
        Create("black_bishop", 5, 7);
        Create("black_knight", 6, 7);
        Create("black_rook", 7, 7);

        // Black pawns (y = 6)
        for (int x = 0; x < 8; x++)
            Create("black_pawn", x, 6);

        // Artýk ayrý bir dizide döngüyle SetPosition çaðýrmaya gerek yok
    }

    // Update is called once per frame
    void Update()
    {

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
}
