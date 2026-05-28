using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class AIMoveEventArgs
{
    public int fromX;
    public int fromY;
    public int toX;
    public int toY;
    public GameObject piece;
    public GameObject target;
    public bool capture => target != null;

    public AIMoveEventArgs(int fx, int fy, int tx, int ty, GameObject p, GameObject t)
    {
        fromX = fx;
        fromY = fy;
        toX = tx;
        toY = ty;
        piece = p;
        target = t;
    }
}

[System.Serializable]
public class AIMoveUnityEvent : UnityEvent<AIMoveEventArgs> { }

public class AIController : MonoBehaviour
{
    private Game controller;
    private bool thinking = false;
    public float baseThinkTime = 0.6f;
    private System.Random rnd = new System.Random();

    // C# events (kod abonelikleri için)
    public event System.Action OnThinkingStarted;
    public event System.Action OnThinkingFinished;
    public event System.Action<AIMoveEventArgs> OnMoveChosen;
    public event System.Action<AIMoveEventArgs> OnMoveExecuted;
    public event System.Action<AIMoveEventArgs> OnPieceCaptured;

    // UnityEvents (Inspector'a baðlamak için)
    [Header("AI Events (Inspector)")]
    [SerializeField] public UnityEvent onThinkingStartedUnity;
    [SerializeField] public UnityEvent onThinkingFinishedUnity;
    [SerializeField] public AIMoveUnityEvent onMoveChosenUnity;
    [SerializeField] public AIMoveUnityEvent onMoveExecutedUnity;
    [SerializeField] public AIMoveUnityEvent onPieceCapturedUnity;

    private void Start()
    {
        // GameObject.FindGameObjectWithTag yerine de bunu kullanabilirsin eðer sahnede tek bir Game scripti varsa:
        controller = Object.FindAnyObjectByType<Game>();

        if (controller == null)
        {
            Debug.LogWarning("[AIController] Game bileþeni bulunamadý.");
        }
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
        OnThinkingStarted?.Invoke();
        onThinkingStartedUnity?.Invoke();

        int elo = AIManager.Instance != null ? AIManager.Instance.Elo : 500;
        float think = baseThinkTime + Mathf.Clamp((1500 - elo) / 2000f, 0f, 1.0f);
        yield return new WaitForSeconds(think);

        var moves = GenerateAllMoves(controller, (AIManager.Instance.Side == AIManager.AISide.White) ? "white" : "black");
        if (moves.Count == 0)
        {
            Debug.Log("[AIController] Hamle yok.");
            thinking = false;
            OnThinkingFinished?.Invoke();
            onThinkingFinishedUnity?.Invoke();
            yield break;
        }

        // Score each move
        var scored = new List<(Move m, float score)>();
        foreach (var m in moves)
            scored.Add((m, EvaluateMove(controller, m, elo)));

        // Noise scale: yüksek elo -> daha az gürültü
        float noiseScale = Mathf.Lerp(220f, 5f, Mathf.InverseLerp(300f, 2000f, elo));
        for (int i = 0; i < scored.Count; i++)
        {
            float noise = (float)(rnd.NextDouble() * 2.0 - 1.0) * noiseScale;
            scored[i] = (scored[i].m, scored[i].score + noise);
        }

        scored.Sort((a, b) => b.score.CompareTo(a.score));

        // Pool boyutu: düþük elo daha geniþ havuz
        int choicePool = Mathf.Clamp(1 + (int)((1100f - elo) / 250f), 1, Mathf.Min(8, scored.Count));
        var pool = scored.Take(choicePool).ToList();

        // 1500+ için tercih: daha deterministik (küçük pool, favor captures)
        if (elo >= 1400)
        {
            // Eðer pool içinde capture varsa onlardan seç
            var captures = pool.Where(p => p.m.target != null).ToList();
            if (captures.Count > 0)
            {
                pool = captures;
            }
        }

        var chosen = pool[rnd.Next(pool.Count)].m;

        Debug.Log($"[AIController] Elo={elo} moves={moves.Count} pool={choicePool} chosen: {chosen.fromX},{chosen.fromY}->{chosen.toX},{chosen.toY} capture={chosen.capture}");

        // Execute move (MovePlate.OnMouseUp mantýðý)
        var sc = controller;
        if (chosen.target != null)
        {
            string cpName = chosen.target.name ?? "";
            if (cpName == "white_king_0")
                sc.GetComponent<Game>().Winner("black");
            else if (cpName == "black_king_0")
                sc.GetComponent<Game>().Winner("white");
            Destroy(chosen.target);
            Debug.Log($"[AIController] Captured at {chosen.toX},{chosen.toY}");
        }

        GameObject reference = chosen.piece;
        sc.SetPositionEmpty(reference.GetComponent<Chessman>().GetXBoard(), reference.GetComponent<Chessman>().GetYBoard());

        reference.GetComponent<Chessman>().SetXBoard(chosen.toX);
        reference.GetComponent<Chessman>().SetYBoard(chosen.toY);
        reference.GetComponent<Chessman>().SetCoords();

        sc.SetPosition(reference);
        sc.NextTurn();
        reference.GetComponent<Chessman>().DestroyMovePlates();

        // --- YENÝ EKLENECEK KISIM BAÞLANGICI ---
        // Sesi ve diðer sistemleri tetiklemek için Event Argümanlarýný hazýrlýyoruz
        AIMoveEventArgs args = new AIMoveEventArgs(
            chosen.fromX, chosen.fromY,
            chosen.toX, chosen.toY,
            chosen.piece,
            chosen.target // eðer taþ yendiyse null deðil, normal hamleyse null gider
        );

        // C# Event'lerini tetikle
        OnMoveExecuted?.Invoke(args);
        if (chosen.target != null) OnPieceCaptured?.Invoke(args);

        // Unity Event'lerini (Inspector) tetikle
        onMoveExecutedUnity?.Invoke(args);
        if (chosen.target != null) onPieceCapturedUnity?.Invoke(args);

        OnThinkingFinished?.Invoke();
        onThinkingFinishedUnity?.Invoke();
        // --- YENÝ EKLENECEK KISIM BÝTÝÞÝ ---

        thinking = false;
    }

    // Basit hamle tanýmý
    private struct Move
    {
        public int fromX, fromY;
        public int toX, toY;
        public GameObject piece;
        public GameObject target;
        public bool capture => target != null;
    }

    // Deðerlendirme: elo'ya göre aðýrlýk deðiþtirir
    float EvaluateMove(Game scObj, Move m, int elo)
    {
        float score = 0f;

        if (m.capture)
        {
            string tgtBase = BaseName(m.target);
            score += PieceValue(tgtBase) * 1.0f;
        }

        // Merkeze tercih (daha yüksek elo'da daha güçlü)
        float centerBonus = (m.toX >= 2 && m.toX <= 5 && m.toY >= 2 && m.toY <= 5) ? 25f : 0f;
        score += centerBonus * Mathf.Lerp(0.6f, 1.2f, Mathf.InverseLerp(300f, 2000f, elo));

        // Piyon ilerlemesi
        string pBase = BaseName(m.piece);
        if (pBase.EndsWith("pawn"))
        {
            if (m.piece.GetComponent<Chessman>().player == "white")
                score += m.toY * 2f;
            else
                score += (7 - m.toY) * 2f;
        }

        // Rakibin cevap ihtimali: eðer taþ yakalanma riski varsa ceza (daha yüksek elo daha az tolerans)
        string opponent = (m.piece.GetComponent<Chessman>().player == "white") ? "black" : "white";
        var oppMoves = GenerateAllMoves(scObj, opponent);
        bool willBeCaptured = oppMoves.Any(om => om.capture && om.toX == m.toX && om.toY == m.toY);
        if (willBeCaptured)
        {
            score -= PieceValue(BaseName(m.piece)) * 0.4f * Mathf.Lerp(1.0f, 1.5f, Mathf.InverseLerp(300f, 2000f, elo));
        }

        // Küçük hareketlik bonusu (mobility) - daha yüksek elo bunu daha çok önemser
        int mobility = GenerateAllMovesForPiece(scObj, m.piece).Count;
        score += mobility * Mathf.Lerp(1f, 3f, Mathf.InverseLerp(300f, 2000f, elo));

        return score;
    }

    // Parça deðeri
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

    // Tüm hamleleri üret (Chessman mantýðýna paralel)
    List<Move> GenerateAllMoves(Game scObj, string player)
    {
        var moves = new List<Move>();
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

    List<Move> GenerateAllMovesForPiece(Game scObj, GameObject cp)
    {
        int x = cp.GetComponent<Chessman>().GetXBoard();
        int y = cp.GetComponent<Chessman>().GetYBoard();
        var list = new List<Move>();
        Game sc = scObj.GetComponent<Game>();
        string baseName = BaseName(cp);

        switch (baseName)
        {
            case "white_queen":
            case "black_queen":
                AddLineMoves(sc, cp, x, y, list, 1, 0);
                AddLineMoves(sc, cp, x, y, list, 0, 1);
                AddLineMoves(sc, cp, x, y, list, 1, 1);
                AddLineMoves(sc, cp, x, y, list, -1, 0);
                AddLineMoves(sc, cp, x, y, list, 0, -1);
                AddLineMoves(sc, cp, x, y, list, -1, -1);
                AddLineMoves(sc, cp, x, y, list, -1, 1);
                AddLineMoves(sc, cp, x, y, list, 1, -1);
                break;
            case "white_rook":
            case "black_rook":
                AddLineMoves(sc, cp, x, y, list, 1, 0);
                AddLineMoves(sc, cp, x, y, list, 0, 1);
                AddLineMoves(sc, cp, x, y, list, -1, 0);
                AddLineMoves(sc, cp, x, y, list, 0, -1);
                break;
            case "white_bishop":
            case "black_bishop":
                AddLineMoves(sc, cp, x, y, list, 1, 1);
                AddLineMoves(sc, cp, x, y, list, 1, -1);
                AddLineMoves(sc, cp, x, y, list, -1, 1);
                AddLineMoves(sc, cp, x, y, list, -1, -1);
                break;
            case "white_knight":
            case "black_knight":
                AddPointMove(sc, cp, x + 1, y + 2, list);
                AddPointMove(sc, cp, x - 1, y + 2, list);
                AddPointMove(sc, cp, x + 2, y + 1, list);
                AddPointMove(sc, cp, x + 2, y - 1, list);
                AddPointMove(sc, cp, x + 1, y - 2, list);
                AddPointMove(sc, cp, x - 1, y - 2, list);
                AddPointMove(sc, cp, x - 2, y + 1, list);
                AddPointMove(sc, cp, x - 2, y - 1, list);
                break;
            case "white_king":
            case "black_king":
                AddPointMove(sc, cp, x, y + 1, list);
                AddPointMove(sc, cp, x, y - 1, list);
                AddPointMove(sc, cp, x - 1, y - 1, list);
                AddPointMove(sc, cp, x - 1, y, list);
                AddPointMove(sc, cp, x - 1, y + 1, list);
                AddPointMove(sc, cp, x + 1, y - 1, list);
                AddPointMove(sc, cp, x + 1, y, list);
                AddPointMove(sc, cp, x + 1, y + 1, list);
                break;
            case "white_pawn":
                AddPawnMoves(sc, cp, x, y, 1, list);
                break;
            case "black_pawn":
                AddPawnMoves(sc, cp, x, y, -1, list);
                break;
        }

        return list;
    }

    void AddLineMoves(Game sc, GameObject cp, int startX, int startY, List<Move> moves, int incX, int incY)
    {
        int x = startX + incX;
        int y = startY + incY;
        while (sc.PositionOnBoard(x, y) && sc.GetPosition(x, y) == null)
        {
            moves.Add(new Move { fromX = startX, fromY = startY, toX = x, toY = y, piece = cp, target = null });
            x += incX;
            y += incY;
        }
        if (sc.PositionOnBoard(x, y))
        {
            GameObject target = sc.GetPosition(x, y);
            if (target != null && target.GetComponent<Chessman>().player != cp.GetComponent<Chessman>().player)
            {
                moves.Add(new Move { fromX = startX, fromY = startY, toX = x, toY = y, piece = cp, target = target });
            }
        }
    }

    void AddPointMove(Game sc, GameObject cp, int x, int y, List<Move> moves)
    {
        if (!sc.PositionOnBoard(x, y)) return;
        GameObject q = sc.GetPosition(x, y);
        if (q == null)
            moves.Add(new Move { fromX = cp.GetComponent<Chessman>().GetXBoard(), fromY = cp.GetComponent<Chessman>().GetYBoard(), toX = x, toY = y, piece = cp, target = null });
        else if (q.GetComponent<Chessman>().player != cp.GetComponent<Chessman>().player)
            moves.Add(new Move { fromX = cp.GetComponent<Chessman>().GetXBoard(), fromY = cp.GetComponent<Chessman>().GetYBoard(), toX = x, toY = y, piece = cp, target = q });
    }

    void AddPawnMoves(Game sc, GameObject cp, int x, int y, int dir, List<Move> moves)
    {
        int fx = x;
        int fy = y + dir;
        if (sc.PositionOnBoard(fx, fy) && sc.GetPosition(fx, fy) == null)
        {
            moves.Add(new Move { fromX = x, fromY = y, toX = fx, toY = fy, piece = cp, target = null });

            int startRank = (dir == 1) ? 1 : 6;
            int fy2 = y + dir * 2;
            if (y == startRank && sc.PositionOnBoard(fx, fy2) && sc.GetPosition(fx, fy2) == null)
            {
                moves.Add(new Move { fromX = x, fromY = y, toX = fx, toY = fy2, piece = cp, target = null });
            }
        }

        int ax = x + 1;
        int ay = y + dir;
        if (sc.PositionOnBoard(ax, ay))
        {
            GameObject tgt = sc.GetPosition(ax, ay);
            if (tgt != null && tgt.GetComponent<Chessman>().player != cp.GetComponent<Chessman>().player)
                moves.Add(new Move { fromX = x, fromY = y, toX = ax, toY = ay, piece = cp, target = tgt });
        }
        ax = x - 1;
        ay = y + dir;
        if (sc.PositionOnBoard(ax, ay))
        {
            GameObject tgt = sc.GetPosition(ax, ay);
            if (tgt != null && tgt.GetComponent<Chessman>().player != cp.GetComponent<Chessman>().player)
                moves.Add(new Move { fromX = x, fromY = y, toX = ax, toY = ay, piece = cp, target = tgt });
        }
    }
}
