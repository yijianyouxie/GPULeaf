using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TestList : MonoBehaviour {

    private List<Camera> cList = new List<Camera>(2);
	// Use this for initialization
	void Start () {
        Debug.LogError("=====cList.Legth:" + cList.Count);
        Camera c = new Camera();
        cList.Add(c);
        Debug.LogError("=====cList.Legth2:" + cList.Count);
    }
	
	// Update is called once per frame
	void Update () {
	
	}
}
