using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SyncHelper : MonoBehaviour
{
    [SerializeField] private GameObject[] _syncObjects;

    private Coroutine syncRoutine;

    private void OnEnable()
    {
        if (syncRoutine != null)
        {
            StopCoroutine(syncRoutine);
        }
        syncRoutine = StartCoroutine(SyncRoutine());
    }

    private IEnumerator SyncRoutine()
    {
        SetObjectsActive(false);

        yield return new WaitForEndOfFrame();
        
        SetObjectsActive(true);
    }

    private void SetObjectsActive(bool active)
    {
        for (int i = 0; i < _syncObjects.Length; i++)
        {
            _syncObjects[i].SetActive(active);
        }
    }
}
