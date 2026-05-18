using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AIEnemy500 : MonoBehaviour
{
    // Basit hamle tanýmý
    private struct AIMove
    {
        public int fromX, fromY;
        public int toX, toY;
        public GameObject piece;
        public GameObject target;
        public bool capture => target != null;
    }

    private Game controller;
    private bool thinking = false;
    public float baseThinkTime = 0.6f;
    private System.Random rnd = new System.Random();

    private void Start()
    {
        var go = GameObject.FindGameObjectWithTag("GameController");
        if (go == null)
        {
            Debug.LogWarning("[AIEnemy500] GameController bulunamadý.");
            return;
        }
        controller = go.GetComponent<Game>();
    }

    private void Update()
    {
        if (controller == null) return;
        if (thinking) return;
        if (AIManager.Instance == null) return;
        if (!AIManager.Instance.AIEnabled) return;
        if (controller.IsGameOver()) return;

        string current = controller.GetCurrentPlayer();
        string aiSide = (AIManager.Instance.Side == AIManager.AISide.White) ? "white" : "black";
        if (current != aiSide) return;

        StartCoroutine(ThinkAndMove());
    }

    IEnumerator ThinkAndMove()
    {
        thinking = true;

        int elo = AIManager.Instance != null ? AIManager.Instance.Elo : 500;
        float think = baseThinkTime + Mathf.Clamp((1500 - elo) / 2000f, 0f, 1.0f);
        yield return new WaitForSeconds(think);

        var moves = GenerateAllMoves(controller, (AIManager.Instance.Side == AIManager.AISide.White) ? "white" : "black");
        if (moves.Count == 0)
        {
            Debug.Log("[AIEnemy500] Hamle yok.");
            thinking = false;
            yield break;
        }

        var scored = new List<(AIMove move, float score)>();
        foreach (var m in moves)
            scored.Add((m, EvaluateMove(controller, m)));

        float noiseScale = Mathf.Lerp(200f, 20f, Mathf.InverseLerp(300f, 2000f, elo));
        for (int i = 0; i < scored.Count; i++)
        {
            float noise = (float)(rnd.NextDouble() * 2.0 - 1.0) * noiseScale;
            scored[i] = (scored[i].move, scored[i].score + noise);
        }

        scored.Sort((a, b) => b.score.CompareTo(a.score));
        int choicePool = Mathf.Clamp(1 + (int)((1000f - elo) / 300f), 1, Mathf.Min(6, scored.Count));
        var pool = scored.Take(choicePool).ToList();
        var chosen = pool[rnd.Next(pool.Count)].move;

        Debug.Log($"[AIEnemy500] Seçildi: {chosen.fromX},{chosen.fromY} -> {chosen.toX},{chosen.toY} capture={chosen.capture}");

        // Uygula (MovePlate.OnMouseUp mantýðýyla)
        var sc = controller.GetComponent<Game>();
        if (chosen.target != null)
        {
            string cpName = chosen.target.name ?? "";
            if (cpName == "white_king_0")
                controller.GetComponent<Game>().Winner("black");
            else if (cpName == "black_king_0")
                controller.GetComponent<Game>().Winner("white");
            Destroy(chosen.target);
            Debug.Log($"[AIEnemy500] Yakalandý: {chosen.toX},{chosen.toY}");
        }

        GameObject reference = chosen.piece;
        controller.SetPositionEmpty(reference.GetComponent<Chessman>().GetXBoard(), reference.GetComponent<Chessman>().GetYBoard());

        reference.GetComponent<Chessman>().SetXBoard(chosen.toX);
        reference.GetComponent<Chessman>().SetYBoard(chosen.toY);
        reference.GetComponent<Chessman>().SetCoords();

        controller.SetPosition(reference);
        controller.NextTurn();
        reference.GetComponent<Chessman>().DestroyMovePlates();

        thinking = false;
    }

    // Basit deðerleme: capture deðeri + pozisyon
    float EvaluateMove(Game scController, AIMove m)
    {
        float score = 0f;
        if (m.capture)
        {
            score += PieceValue(BaseName(m.target));
        }

        if (m.toX >= 2 && m.toX <= 5 && m.toY >= 2 && m.toY <= 5) score += 20f;

        string pBase = BaseName(m.piece);
        if (pBase.EndsWith("pawn"))
        {
            if (m.piece.GetComponent<Chessman>().player == "white")
                score += m.toY * 2;
            else
                score += (7 - m.toY) * 2;
        }

        // Basit savunma: rakip hamlelerini hesapla
        string opponent = (m.piece.GetComponent<Chessman>().player == "white") ? "black" : "white";
        var oppMoves = GenerateAllMoves(scController, opponent);
        bool willBeCaptured = oppMoves.Any(om => om.capture && om.toX == m.toX && om.toY == m.toY);
        if (willBeCaptured)
        {
            score -= PieceValue(BaseName(m.piece)) * 0.5f;
        }

        return score;
    }

    // Yardýmcý: parçaya göre deðer
    int PieceValue(string baseName)
    {
        switch (baseName)
        {
            case "white_pawn":
            case "black_pawn": return 100;
            case "white_knight":
            case "black_knight": return 320;
            case "white_bishop":
            case "black_bishop": return 330;
            case "white_rook":
            case "black_rook": return 500;
            case "white_queen":
            case "black_queen": return 900;
            case "white_king":
            case "black_king": return 20000;
        }
        return 0;
    }

    string BaseName(GameObject go)
    {
        string name = go.name;
        if (name.EndsWith("_0"))
            name = name.Substring(0, name.Length - 2);
        return name;
    }

    // Hamle üreticisi (Chessman mantýðýna paralel)
    List<AIMove> GenerateAllMoves(Game scObj, string player)
    {
        var moves = new List<AIMove>();
        Game sc = scObj.GetComponent<Game>();

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                GameObject cp = sc.GetPosition(x, y);
                if (cp == null) continue;
                var chess = cp.GetComponent<Chessman>();
                if (chess == null) continue;
                if (chess.player != player) continue;

                string baseName = BaseName(cp);

                switch (baseName)
                {
                    case "white_queen":
                    case "black_queen":
                        AddLineMoves(sc, cp, x, y, moves, 1, 0);
                        AddLineMoves(sc, cp, x, y, moves, 0, 1);
                        AddLineMoves(sc, cp, x, y, moves, 1, 1);
                        AddLineMoves(sc, cp, x, y, moves, -1, 0);
                        AddLineMoves(sc, cp, x, y, moves, 0, -1);
                        AddLineMoves(sc, cp, x, y, moves, -1, -1);
                        AddLineMoves(sc, cp, x, y, moves, -1, 1);
                        AddLineMoves(sc, cp, x, y, moves, 1, -1);
                        break;
                    case "white_rook":
                    case "black_rook":
                        AddLineMoves(sc, cp, x, y, moves, 1, 0);
                        AddLineMoves(sc, cp, x, y, moves, 0, 1);
                        AddLineMoves(sc, cp, x, y, moves, -1, 0);
                        AddLineMoves(sc, cp, x, y, moves, 0, -1);
                        break;
                    case "white_bishop":
                    case "black_bishop":
                        AddLineMoves(sc, cp, x, y, moves, 1, 1);
                        AddLineMoves(sc, cp, x, y, moves, 1, -1);
                        AddLineMoves(sc, cp, x, y, moves, -1, 1);
                        AddLineMoves(sc, cp, x, y, moves, -1, -1);
                        break;
                    case "white_knight":
                    case "black_knight":
                        AddPointMove(sc, cp, x + 1, y + 2, moves);
                        AddPointMove(sc, cp, x - 1, y + 2, moves);
                        AddPointMove(sc, cp, x + 2, y + 1, moves);
                        AddPointMove(sc, cp, x + 2, y - 1, moves);
                        AddPointMove(sc, cp, x + 1, y - 2, moves);
                        AddPointMove(sc, cp, x - 1, y - 2, moves);
                        AddPointMove(sc, cp, x - 2, y + 1, moves);
                        AddPointMove(sc, cp, x - 2, y - 1, moves);
                        break;
                    case "white_king":
                    case "black_king":
                        AddPointMove(sc, cp, x, y + 1, moves);
                        AddPointMove(sc, cp, x, y - 1, moves);
                        AddPointMove(sc, cp, x - 1, y - 1, moves);
                        AddPointMove(sc, cp, x - 1, y, moves);
                        AddPointMove(sc, cp, x - 1, y + 1, moves);
                        AddPointMove(sc, cp, x + 1, y - 1, moves);
                        AddPointMove(sc, cp, x + 1, y, moves);
                        AddPointMove(sc, cp, x + 1, y + 1, moves);
                        break;
                    case "white_pawn":
                        AddPawnMoves(sc, cp, x, y, 1, moves);
                        break;
                    case "black_pawn":
                        AddPawnMoves(sc, cp, x, y, -1, moves);
                        break;
                }
            }
        }

        return moves;
    }

    void AddLineMoves(Game sc, GameObject cp, int startX, int startY, List<AIMove> moves, int incX, int incY)
    {
        int x = startX + incX;
        int y = startY + incY;
        while (sc.PositionOnBoard(x, y) && sc.GetPosition(x, y) == null)
        {
            moves.Add(new AIMove { fromX = startX, fromY = startY, toX = x, toY = y, piece = cp, target = null });
            x += incX;
            y += incY;
        }
        if (sc.PositionOnBoard(x, y))
        {
            GameObject target = sc.GetPosition(x, y);
            if (target != null && target.GetComponent<Chessman>().player != cp.GetComponent<Chessman>().player)
            {
                moves.Add(new AIMove { fromX = startX, fromY = startY, toX = x, toY = y, piece = cp, target = target });
            }
        }
    }

    void AddPointMove(Game sc, GameObject cp, int x, int y, List<AIMove> moves)
    {
        if (!sc.PositionOnBoard(x, y)) return;
        GameObject q = sc.GetPosition(x, y);
        if (q == null)
            moves.Add(new AIMove { fromX = cp.GetComponent<Chessman>().GetXBoard(), fromY = cp.GetComponent<Chessman>().GetYBoard(), toX = x, toY = y, piece = cp, target = null });
        else if (q.GetComponent<Chessman>().player != cp.GetComponent<Chessman>().player)
            moves.Add(new AIMove { fromX = cp.GetComponent<Chessman>().GetXBoard(), fromY = cp.GetComponent<Chessman>().GetYBoard(), toX = x, toY = y, piece = cp, target = q });
    }

    void AddPawnMoves(Game sc, GameObject cp, int x, int y, int dir, List<AIMove> moves)
    {
        int fx = x;
        int fy = y + dir;
        if (sc.PositionOnBoard(fx, fy) && sc.GetPosition(fx, fy) == null)
        {
            moves.Add(new AIMove { fromX = x, fromY = y, toX = fx, toY = fy, piece = cp, target = null });

            int startRank = (dir == 1) ? 1 : 6;
            int fy2 = y + dir * 2;
            if (y == startRank && sc.PositionOnBoard(fx, fy2) && sc.GetPosition(fx, fy2) == null)
            {
                moves.Add(new AIMove { fromX = x, fromY = y, toX = fx, toY = fy2, piece = cp, target = null });
            }
        }

        int ax = x + 1;
        int ay = y + dir;
        if (sc.PositionOnBoard(ax, ay))
        {
            GameObject tgt = sc.GetPosition(ax, ay);
            if (tgt != null && tgt.GetComponent<Chessman>().player != cp.GetComponent<Chessman>().player)
                moves.Add(new AIMove { fromX = x, fromY = y, toX = ax, toY = ay, piece = cp, target = tgt });
        }
        ax = x - 1;
        ay = y + dir;
        if (sc.PositionOnBoard(ax, ay))
        {
            GameObject tgt = sc.GetPosition(ax, ay);
            if (tgt != null && tgt.GetComponent<Chessman>().player != cp.GetComponent<Chessman>().player)
                moves.Add(new AIMove { fromX = x, fromY = y, toX = ax, toY = ay, piece = cp, target = tgt });
        }
    }
}
