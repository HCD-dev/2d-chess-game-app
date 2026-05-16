using UnityEngine;

public class Chessman : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject controller;
    public GameObject movePlate;

    //positions
    private int Xboard = -1;
    private int Yboard = -1;

    //variable to keep track of "black" player or "white" player
    private string player;

    //refence for all sprites that the chesspiece can be
    public Sprite black_queen, black_knight, black_bishop,  black_king, black_pawn,   black_rook;
    public Sprite white_queen, white_knight, white_bishop, white_king, white_pawn, white_rook;

    public void Activate()
    {
        controller = GameObject.FindGameObjectWithTag("GameController");

        SetCoords();
        
        switch(this.name)
        {
            case "black_queen": this.GetComponent<SpriteRenderer>().sprite = black_queen; break;
            case "black_knight": this.GetComponent<SpriteRenderer>().sprite = black_knight; break;
            case "black_bishop": this.GetComponent<SpriteRenderer>().sprite = black_bishop; break;
            case "black_king": this.GetComponent<SpriteRenderer>().sprite = black_king; break;
            case "black_pawn": this.GetComponent<SpriteRenderer>().sprite = black_pawn; break;
            case "black_rook": this.GetComponent<SpriteRenderer>().sprite = black_rook; break;

            case "white_queen": this.GetComponent<SpriteRenderer>().sprite = white_queen; break;
            case "white_knight": this.GetComponent<SpriteRenderer>().sprite = white_knight; break;
            case "white_bishop": this.GetComponent<SpriteRenderer>().sprite = white_bishop; break;
            case "white_king": this.GetComponent<SpriteRenderer>().sprite = white_king; break;
            case "white_pawn": this.GetComponent<SpriteRenderer>().sprite = white_pawn; break;
            case "white_rook": this.GetComponent<SpriteRenderer>().sprite = white_rook; break;

            default:
                Debug.LogWarning("Unknown chess piece name: " + this.name);
                break;
        }

    }

    private void SetCoords()
    {
        // Eðer sahnede "Board" tag'li bir obje varsa onun SpriteRenderer.bounds'ý üzerinden
        // kare boyutunu ve sol-alt (bottom-left) köþeyi hesapla. Bu, board'un scale'ine göre
        // tüm taþlarýn doðru þekilde hizalanmasýný saðlar.
        GameObject board = GameObject.FindGameObjectWithTag("Board");
        if (board != null)
        {
            SpriteRenderer sr = board.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Bounds b = sr.bounds;
                float tileSizeX = b.size.x / 8f;
                float tileSizeY = b.size.y / 8f;

                // Board'un sol-alt köþesi
                Vector3 bottomLeft = new Vector3(b.min.x, b.min.y, 0f);

                // Her taþ, karesinin ortasýna yerleþtirilsin
                float worldX = bottomLeft.x + (Xboard + 0.5f) * tileSizeX;
                float worldY = bottomLeft.y + (Yboard + 0.5f) * tileSizeY;

                this.transform.position = new Vector3(worldX, worldY, -1.0f);
                return;
            }
            else
            {
                Debug.LogWarning("Board objesi bulundu fakat SpriteRenderer yok. Varsayýlan hizalama kullanýlacak.");
            }
        }
        else
        {
            Debug.LogWarning("Board tag'li obje bulunamadý. Varsayýlan hizalama kullanýlacak.");
        }

        // Fallback: önceki sabit dönüþüm (uyumlu kalmak için)
        float x = Xboard;
        float y = Yboard;

        x *= 0.66f;
        y *= 0.66f;

        y += 0.33f;
        x += 0.33f;
        this.transform.position = new Vector3(x, y, -1.0f);
    }
    public int GetXBoard()
    {
        return Xboard;
    }
    public int GetYBoard() 
    {
        return Yboard;
    }
    public void SetXBoard(int x)
    {
        Xboard = x;
    }
    public void SetYBoard(int y)
    {
        Yboard = y;
    }
}
