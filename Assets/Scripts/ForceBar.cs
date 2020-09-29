/*  <Shows applied force on the force bar.>
    Copyright (C) 2020  Christian Kaltschmidt <c.kaltschmidt@gmx.de>

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

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
