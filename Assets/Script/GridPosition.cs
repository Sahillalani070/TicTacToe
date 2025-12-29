using UnityEngine;

public class GridPosition : MonoBehaviour
{

    [SerializeField] private int x;
    [SerializeField] private int y;
    private void OnMouseDown()
    {
        Debug.Log("Click" + x + "," + y);
        GameManager.Instance.ClickedOnGridPositionRpc(x, y, GameManager.Instance.GetLocalPlayerType());
    }
}
