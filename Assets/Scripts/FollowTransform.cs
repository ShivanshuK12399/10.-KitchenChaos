using UnityEngine;

public class FollowTransform : MonoBehaviour
{
    Transform targetTransform;

    public void SetTargetTransform(Transform targetTransform)
    {
        this.targetTransform = targetTransform;
    }

    void LateUpdate()
    {
        if (targetTransform)
        {
            transform.position = targetTransform.position;
            transform.rotation = targetTransform.rotation;
        }
    }
}
