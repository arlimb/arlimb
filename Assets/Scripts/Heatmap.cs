/*  <Controls the heatmap.>
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
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System;

public class Heatmap {
    private int arrayX = 8;
    private int arrayY = 24;
	private float angle = 15.0f;
    private float radius = 3.4f;
	private float cubesize = 0.6f;
	private float heightGap = 0.61f;
	private float cubeaddlength = 0.25f;

    private GameObject HeatMap;

    private GameObject[] arrCubes;

	private Color[] colors = new Color[64];

    public bool isVisible { get; private set; }

    public Heatmap(string colormap, float alpha, float cubeheight, bool map3D, GameObject mainArm)
	{
		// create cubes around cylinder surface
		HeatMap = new GameObject("HeatMap");
		arrCubes = new GameObject[24];

		// Read colormap RGB values from file
		string fileData;
		string[] lines;
		fileData = System.IO.File.ReadAllText(string.Format(@".\Assets\Scripts\colormap\{0}.txt", colormap));
		
		lines = fileData.Split("\n"[0]);
		string[] lineData;

		for (int i = 0; i < 64; i++)
		{
			lineData = (lines[i].Trim()).Split(","[0]);
			colors[i] = new Color(float.Parse(lineData[0]), float.Parse(lineData[1]), float.Parse(lineData[2]), alpha);
		} // end for
		
		for (int j = 0; j < arrayX; j++)
		{ // one circle per height
			for (int i = 0; i < arrayY; i++)
			{ 
				GameObject HeatMapGameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
				HeatMapGameObject.name = string.Format("Cube{0}", j * 24 + i);
				HeatMapGameObject.GetComponent<Renderer>().material = new Material(Shader.Find("Transparent/Diffuse"));
				HeatMapGameObject.GetComponent<Renderer>().material.color = new Color(0, 0, 0, 0);

				arrCubes[i] = HeatMapGameObject;
				arrCubes[i].transform.parent = HeatMap.transform;

				arrCubes[i].transform.position = new Vector3(Mathf.Sin(Mathf.Deg2Rad * angle * i) * radius, Mathf.Cos(Mathf.Deg2Rad * angle * i) * radius, j * heightGap);

				arrCubes[i].transform.Rotate(new Vector3(0, 90f, 0));
				arrCubes[i].transform.Rotate(new Vector3(90f + 15f * i, 0, 0));
			} // end for
		} // end for
		
		// place cylinder of cubes around arm model
		HeatMap.transform.Rotate(new Vector3(90f, 150f, 0)); // CHANGE ANGLE HERE!!! y
		HeatMap.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
		HeatMap.transform.position = GameObject.Find("lower_arm").transform.position + new Vector3(-0.05f, 10f, 0);

        // Set Arm Model as child of arm model object
        GameObject armModel = mainArm;// GameObject.Find("male_arms_blendtree");
		GameObject lower_arm = GameObject.Find("lower_arm");
		HeatMap.transform.parent = armModel.transform;
		armModel.transform.parent = lower_arm.transform;
	}

    // Deactivate
    public void Deactivate()
    {
        if (HeatMap!=null && isVisible)
        {
            Renderer[] rendererComponents = HeatMap.GetComponentsInChildren<Renderer>(true);
            // Disable rendering:
            foreach (Renderer component in rendererComponents)
            {
                component.enabled = false;
            }
            isVisible = false;
        }
    }

    // Activate
    public void Activate()
    {
        if (HeatMap != null && !isVisible)
        {
            Renderer[] rendererComponents = HeatMap.GetComponentsInChildren<Renderer>(true);
            // Disable rendering:
            foreach (Renderer component in rendererComponents)
            {
                component.enabled = true;
            }
            isVisible = true;
        }
    }

    
    public void UpdateHeatMap( byte[] map, float cubeheight, bool map3D, bool heat, bool innerGrowing)
    {
        int[] values = new int[192];
		for (int i = 0; i < 192; i++) {
			values [i] = Convert.ToInt32 (map [i]);
		}
		int index = 0;
		int lightIndex = 0;
		int j = 0;
		int l = 0;
		int m = 0;

        if (!isVisible)
        {
            Activate();

        }
        // Select correct entry in Matrix (lineIndex,k) for each cube in Heatmap (lightIndex), set appropriate color, size and position transform
        for (int k = 0; k < 192; k++) {
			lightIndex = k % 8 * 24 + j;
			l = lightIndex % 24;
			m = lightIndex / 24;
			GameObject currentObject = GameObject.Find (string.Format ("Cube{0}", lightIndex));
			index = values [k];
			if (index < 1)
				index = 1;
			if (heat) {
                if (index < colors.Length)
                {
                    currentObject.GetComponent<Renderer>().material.color = colors[index];
                }
                else
                {
                    currentObject.GetComponent<Renderer>().material.color = colors[colors.Length-1];
                }
				
			} else {
				currentObject.GetComponent<Renderer> ().material.color = new Color(0, 0, 0, 0);
			}
			if (map3D) {
				if (innerGrowing){
					currentObject.transform.localScale = new Vector3 (cubesize, cubesize + cubeaddlength, index * cubeheight * 0.1f);
					currentObject.transform.localPosition = new Vector3(Mathf.Sin(Mathf.Deg2Rad * angle * l) * radius, Mathf.Cos(Mathf.Deg2Rad * angle * l) * radius, m * heightGap);
				}else{
					currentObject.transform.localScale = new Vector3 (cubesize, cubesize + cubeaddlength, index * cubeheight * 0.05f);
					currentObject.transform.localPosition = new Vector3(Mathf.Sin(Mathf.Deg2Rad * angle * l) * (radius + index * cubeheight * 0.025f), Mathf.Cos(Mathf.Deg2Rad * angle * l) * (radius + index * cubeheight * 0.025f), m * heightGap);
				}
			} else {
				currentObject.transform.localScale = new Vector3 (cubesize, cubesize + cubeaddlength, 0.1f);
				currentObject.transform.localPosition = new Vector3(Mathf.Sin(Mathf.Deg2Rad * angle * l) * radius, Mathf.Cos(Mathf.Deg2Rad * angle * l) * radius, m * heightGap);
			}
			if (k % 8 == 7) {
				j++;
			} // end if
		} // end for
    } // end UpdateHeatMap
} // end class
