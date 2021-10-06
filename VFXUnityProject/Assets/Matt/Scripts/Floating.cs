using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Floating : MonoBehaviour
{
    public float DriftSpeed = 1f;
    public Vector3 DriftLimits = Vector3.zero;
    public float DistanceBeforeRecalculate = 0.1f;

    private Vector3 _rootPosition;
    private Vector3 _nextPosition;
    
    private void Awake()
    {
        _rootPosition = transform.localPosition;
        Debug.Log($"{gameObject.name} starting position = {_rootPosition.x}, {_rootPosition.y}, {_rootPosition.z}");
    }

    private void OnDisable()
    {
        transform.localPosition = _rootPosition;
    }

    private void OnEnable()
    {
        CalculateNextPosition();
    }

    private void LateUpdate()
    {
        if (Vector3.Distance(transform.localPosition, _nextPosition) > DistanceBeforeRecalculate)
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, _nextPosition, DriftSpeed);
        }
        else
        {
            CalculateNextPosition();
        }
    }

    private void CalculateNextPosition()
    {
        _nextPosition.x = RandomWithinLimit(_rootPosition.x, DriftLimits.x);
        _nextPosition.y = RandomWithinLimit(_rootPosition.y, DriftLimits.y);
        _nextPosition.z = RandomWithinLimit(_rootPosition.z, DriftLimits.z);

        Debug.Log($"{gameObject.name} calculated a new position = {_nextPosition.x}, {_nextPosition.y}, {_nextPosition.z}");
        
        float RandomWithinLimit(float root, float limit)
        {
            return Random.Range(root-limit, root+limit);
        }
    }
}
