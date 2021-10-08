using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableInXSeconds : MonoBehaviour
{

    public float Seconds;

    public GameObject Target;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(DoIt());
    }

    // Update is called once per frame
    private  IEnumerator DoIt()
    {
        yield return new WaitForSeconds(Seconds);

        Target.SetActive(true);

        yield return true;
    }
}
