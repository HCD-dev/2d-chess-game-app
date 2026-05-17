using JetBrains.Annotations;
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
    public string player;

    //refence for all sprites that the chesspiece can be
    public Sprite black_queen, black_knight, black_bishop, black_king, black_pawn, black_rook;
    public Sprite white_queen, white_knight, white_bishop, white_king, white_pawn, white_rook;

    public void Activate()
    {
        controller = GameObject.FindGameObjectWithTag("GameController");

        SetCoords();

        // Ensure there is a Collider2D so OnMouseUp works, and size it to sprite
        if (GetComponent<Collider2D>() == null)
        {
            var bc = gameObject.AddComponent<BoxCollider2D>();
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                // sprite.bounds is in local sprite units (pixels-per-unit accounted), good for collider.size
                bc.size = sr.sprite.bounds.size;
            }
            Debug.Log($"[Chessman] Added BoxCollider2D to '{name}'");
        }

        // Normalize name: support both "white_queen" and "white_queen_0"
        string baseName = this.name;
        if (baseName.EndsWith("_0"))
            baseName = baseName.Substring(0, baseName.Length - 2);

        // Set player based on prefix
        if (baseName.StartsWith("white_"))
            player = "white";
        else if (baseName.StartsWith("black_"))
            player = "black";

        switch (baseName)
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

    public void SetCoords()
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
    private void OnMouseUp()
    {
        if (!controller.GetComponent<Game>().IsGameOver() && controller.GetComponent<Game>().GetCurrentPlayer() == player)
        {

            Debug.Log($"[Chessman] Clicked '{name}' at {Xboard},{Yboard}");
            DestroyMovePlates();
            InitiateMovePlates();
        }
    }
    public void DestroyMovePlates()
    {
        GameObject[] movePlates = GameObject.FindGameObjectsWithTag("MovePlate");
        for (int i = 0; i < movePlates.Length; i++)
        {
            Destroy(movePlates[i]);
        }
    }
    public void InitiateMovePlates()
    {
        // Normalize name here as well
        string baseName = this.name;
        if (baseName.EndsWith("_0"))
            baseName = baseName.Substring(0, baseName.Length - 2);

        switch (baseName)
        {
            case "black_queen":
            case "white_queen":
                LineMovePlate(1, 0);
                LineMovePlate(0, 1);
                LineMovePlate(1, 1);
                LineMovePlate(-1, 0);
                LineMovePlate(0, -1);
                LineMovePlate(-1, -1);
                LineMovePlate(-1, 1);
                LineMovePlate(1, -1);
                break;
            case "black_knight":
            case "white_knight":
                LMovePlate();
                break;
            case "black_bishop":
            case "white_bishop":
                LineMovePlate(1, 1);
                LineMovePlate(1, -1);
                LineMovePlate(-1, 1);
                LineMovePlate(-1, -1);
                break;
            case "black_king":
            case "white_king":
                SurroundMovePlate();
                break;
            case "black_rook":
            case "white_rook":
                LineMovePlate(1, 0);
                LineMovePlate(0, 1);
                LineMovePlate(-1, 0);
                LineMovePlate(0, -1);
                break;
            case "black_pawn":
                PawnMovePlate(Xboard, Yboard - 1);
                break;
            case "white_pawn":
                PawnMovePlate(Xboard, Yboard + 1);
                break;
        }
    }
    public void LineMovePlate(int xIncrement, int yIncrement)
    {
        Game sc = controller.GetComponent<Game>();
        int x = Xboard + xIncrement;
        int y = Yboard + yIncrement;

        while (sc.PositionOnBoard(x, y) && sc.GetPosition(x, y) == null)
        {
            MovePlateSpawn(x, y);
            x += xIncrement;
            y += yIncrement;
        }

        if (sc.PositionOnBoard(x, y) && sc.GetPosition(x, y).GetComponent<Chessman>().player != player)
        {
            MovePlateAttackSpawn(x, y);


        }
    }
    public void LMovePlate()
    {
        PointMovePlate(Xboard + 1, Yboard + 2);
        PointMovePlate(Xboard - 1, Yboard + 2);
        PointMovePlate(Xboard + 2, Yboard + 1);
        PointMovePlate(Xboard + 2, Yboard - 1);
        PointMovePlate(Xboard + 1, Yboard - 2);
        PointMovePlate(Xboard - 1, Yboard - 2);
        PointMovePlate(Xboard - 2, Yboard + 1);
        PointMovePlate(Xboard - 2, Yboard - 1);

    }
    public void SurroundMovePlate()
    {
        PointMovePlate(Xboard, Yboard + 1);
        PointMovePlate(Xboard, Yboard - 1);
        PointMovePlate(Xboard - 1, Yboard - 1);
        PointMovePlate(Xboard - 1, Yboard - 0);
        PointMovePlate(Xboard - 1, Yboard + 1);
        PointMovePlate(Xboard + 1, Yboard - 1);
        PointMovePlate(Xboard + 1, Yboard - 0);
        PointMovePlate(Xboard + 1, Yboard + 1);
    }
    public void PointMovePlate(int x, int y)
    {
        Game sc = controller.GetComponent<Game>();
        if (sc.PositionOnBoard(x, y))
        {
            GameObject cp = sc.GetPosition(x, y);
            if (cp == null)
            {
                MovePlateSpawn(x, y);
            }
            else if (cp.GetComponent<Chessman>().player != player)
            {
                MovePlateAttackSpawn(x, y);
            }
        }
    }

    public void PawnMovePlate(int x, int y)
    {
        Game sc = controller.GetComponent<Game>();
        if (sc.PositionOnBoard(x,y))
        {
            if (sc.GetPosition(x, y) == null)
            {
                MovePlateSpawn(x, y);
            }
        }

        if(sc.PositionOnBoard(x + 1, y)&& sc.GetPosition(x+1, y) != null &&
            sc.GetPosition(x+1,y).GetComponent<Chessman>().player != player)
        {
            MovePlateAttackSpawn(x + 1, y);
        }

        if (sc.PositionOnBoard(x - 1, y) && sc.GetPosition(x - 1, y) != null &&
                    sc.GetPosition(x - 1, y).GetComponent<Chessman>().player != player)
        {
            MovePlateAttackSpawn(x - 1, y);
        }


    }

    public void MovePlateSpawn(int matrixX, int matrixY)
    {
        // Board üzerinden world pozisyonu hesapla (scale baðýmsýz)
        GameObject board = GameObject.FindGameObjectWithTag("Board");
        Vector3 spawnPos;
        if (board != null && board.GetComponent<SpriteRenderer>() != null)
        {
            SpriteRenderer sr = board.GetComponent<SpriteRenderer>();
            Bounds b = sr.bounds;
            float tileSizeX = b.size.x / 8f;
            float tileSizeY = b.size.y / 8f;
            Vector3 bottomLeft = new Vector3(b.min.x, b.min.y, 0f);
            float worldX = bottomLeft.x + (matrixX + 0.5f) * tileSizeX;
            float worldY = bottomLeft.y + (matrixY + 0.5f) * tileSizeY;
            spawnPos = new Vector3(worldX, worldY, -3.0f);
        }
        else
        {
            // Fallback: önceki sabit dönüþüm (geriye uyumluluk)
            float x = matrixX;
            float y = matrixY;
            x *= 0.66f;
            y *= 0.66f;
            x += -2.3f;
            y += -2.3f;
            spawnPos = new Vector3(x, y, -3.0f);
        }

        GameObject mp = Instantiate(movePlate, spawnPos, Quaternion.identity);
        MovePlate mpScript = mp.GetComponent<MovePlate>();
        mpScript.SetReference(gameObject);
        mpScript.SetCoords(matrixX, matrixY);
    }

    public void MovePlateAttackSpawn(int matrixX, int matrixY)
    {
        // Board üzerinden world pozisyonu hesapla (scale baðýmsýz)
        GameObject board = GameObject.FindGameObjectWithTag("Board");
        Vector3 spawnPos;
        if (board != null && board.GetComponent<SpriteRenderer>() != null)
        {
            SpriteRenderer sr = board.GetComponent<SpriteRenderer>();
            Bounds b = sr.bounds;
            float tileSizeX = b.size.x / 8f;
            float tileSizeY = b.size.y / 8f;
            Vector3 bottomLeft = new Vector3(b.min.x, b.min.y, 0f);
            float worldX = bottomLeft.x + (matrixX + 0.5f) * tileSizeX;
            float worldY = bottomLeft.y + (matrixY + 0.5f) * tileSizeY;
            spawnPos = new Vector3(worldX, worldY, -3.0f);
        }
        else
        {
            // Fallback: önceki sabit dönüþüm
            float x = matrixX;
            float y = matrixY;
            x *= 0.66f;
            y *= 0.66f;
            x += -2.3f;
            y += -2.3f;
            spawnPos = new Vector3(x, y, -3.0f);
        }

        GameObject mp = Instantiate(movePlate, spawnPos, Quaternion.identity);
        MovePlate mpScript = mp.GetComponent<MovePlate>();
        mpScript.attack = true;
        mpScript.SetReference(gameObject);
        mpScript.SetCoords(matrixX, matrixY);
    }


}