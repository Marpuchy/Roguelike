using UnityEngine;

public class CellObject : MonoBehaviour
{

    protected Vector2Int MCell;

    public virtual void Init(Vector2Int cell)
    {
        MCell = cell;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public virtual void PlayerEntered()
    {
      
    }

    public virtual bool PlayerWantsToEnter()
    {
        return true;
    }
}
