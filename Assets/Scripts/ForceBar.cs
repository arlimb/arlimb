using UnityEngine;
using System.Collections;

public class ForceBar : MonoBehaviour {
    private bool activateRenderer = false;
    private float appliedForce = 0.0f;
    private float targetForceMin = 0.0f;
    private float targetForceMax = 0.0f;
    private float zeroPosition = 0f;
    private Transform transformForceMax, transformForceMin = null;

	// Use this for initialization
	void Start () {
        zeroPosition = transform.localPosition.x;
        transformForceMax = GameObject.Find("BorderForceMax").transform;
        transformForceMin = GameObject.Find("BorderForceMin").transform;
    }
	
	// Update is called once per frame
	void Update () {
        transform.localPosition = new Vector3(zeroPosition + appliedForce / 2f, transform.localPosition.y, transform.localPosition.z);
        transform.localScale = new Vector3(appliedForce, transform.localScale.y, transform.localScale.z);
    }

    public void SetAppliedForce(float force)
    {
        appliedForce = force;

        //transform.localPosition = new Vector3(zeroPosition + appliedForce / 2f, transform.localPosition.y, transform.localPosition.z);
        //transform.localScale = new Vector3(appliedForce, transform.localScale.y, transform.localScale.z);
    }

    public void ShowForceBar(bool show)
    {
        activateRenderer = show;
    }

    public void SetTargetForce(float min, float max)
    {
        targetForceMax = max;
        targetForceMin = min;

        transformForceMax.localPosition = new Vector3(zeroPosition + max , transformForceMax.localPosition.y, transformForceMax.localPosition.z); //zeroPosition doesn't consider the zeroPosition of the max/min value bu this shouldn't be a problem, since they move in the same y-/z-plane
        transformForceMin.localPosition = new Vector3(zeroPosition + min, transformForceMin.localPosition.y, transformForceMin.localPosition.z);
    }
}
