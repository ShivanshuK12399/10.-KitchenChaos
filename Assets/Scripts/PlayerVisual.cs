using UnityEngine;

public class PlayerVisual : MonoBehaviour
{

    public MeshRenderer head, body;

    Material material;

    void Awake()
    {
        material = new Material(head.material);
        head.material = material;
        body.material = material;
    }

    public void SetPlayerColor(Color color)
    {
        material.color = color;
    }
}
