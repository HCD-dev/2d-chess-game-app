using UnityEngine;

public class MovePlate : MonoBehaviour
{
    public GameObject controller;

    GameObject reference = null;
    
    //board positions
    int matrixX;
    int matrixY;

    public bool attack = false;

    public void Start()
    {
        // Ensure collider so OnMouseUp works and size it to sprite
        if (GetComponent<Collider2D>() == null)
        {
            var bc = gameObject.AddComponent<BoxCollider2D>();
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                bc.size = sr.sprite.bounds.size;
            }
            Debug.Log($"[MovePlate] Added BoxCollider2D to MovePlate instance");
        }

        if (attack)
        {
            //change to red
            var sr = gameObject.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
        }
    }

    public void OnMouseUp()
    {
        Debug.Log($"[MovePlate] Clicked target {matrixX},{matrixY} attack={attack}");
        controller = GameObject.FindGameObjectWithTag("GameController");
        if (attack)
        {
            GameObject cp = controller.GetComponent<Game>().GetPosition(matrixX, matrixY);
            if (cp != null)
            {
                string cpName = cp.name ?? "";
                if (cpName == "white_king_0")
                {
                    controller.GetComponent<Game>().Winner("black");
                }
                else if (cpName == "black_king_0")
                {
                    controller.GetComponent<Game>().Winner("white");
                }
                Destroy(cp);
            }
            else
            {
                Debug.LogWarning($"[MovePlate] Attack attempted on empty square {matrixX},{matrixY}");
            }
        }
        controller.GetComponent<Game>().SetPositionEmpty(reference.GetComponent<Chessman>().GetXBoard(),
            reference.GetComponent<Chessman>().GetYBoard());

        reference.GetComponent<Chessman>().SetXBoard(matrixX);
        reference.GetComponent<Chessman>().SetYBoard(matrixY);
        reference.GetComponent<Chessman>().SetCoords();

        controller.GetComponent<Game>().SetPosition(reference);
        controller.GetComponent<Game>().NextTurn();
        reference.GetComponent<Chessman>().DestroyMovePlates();

    }
    
    public void SetCoords(int x, int y)
    {
        matrixX = x;
        matrixY = y;
    }
    public void SetReference(GameObject obj)
    {
        reference = obj;
    }
    public GameObject GetReference()
    { return reference; }

}
