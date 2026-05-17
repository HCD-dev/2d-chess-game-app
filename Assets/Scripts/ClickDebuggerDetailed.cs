using UnityEngine;
using UnityEngine.EventSystems;

public class ClickDebuggerDetailed : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // UI engeli kontrolü
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log("[ClickDebugger] Click blocked by UI (EventSystem).");
                return;
            }

            Vector3 wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 wp2 = new Vector2(wp.x, wp.y);

            // 2D raycast (öncelikli)
            RaycastHit2D hit2d = Physics2D.Raycast(wp2, Vector2.zero);
            if (hit2d.collider != null)
            {
                Debug.Log($"[ClickDebugger] 2D hit: {hit2d.collider.gameObject.name} (tag={hit2d.collider.gameObject.tag}, layer={LayerMask.LayerToName(hit2d.collider.gameObject.layer)})");
                return;
            }

            // 3D raycast (eðer 3D collider kullanýyorsanýz)
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit3d))
            {
                Debug.Log($"[ClickDebugger] 3D hit: {hit3d.collider.gameObject.name} (tag={hit3d.collider.gameObject.tag})");
                return;
            }

            Debug.Log("[ClickDebugger] No hit: týklama hiçbir fizik koliderine ulaþmadý.");
        }
    }
}
