/*  <Holds information about animation progress.>
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

using System;
using UnityEngine;
using System.Collections;
using System.Threading;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System.Diagnostics;
using System.IO;


namespace AssemblyCSharp
{
	public class DOF
	{
		public string NamePos {get; set;}
		public string NameNeg {get; set;}
		public byte ID {get; set;}
		public float MinValue {get; set;}
		public float MaxValue {get; set;}
		public float CurrentValue {get; private set;} //returns the current animation value
		public int Range {get; set;}

		public DOF(string namePos, string nameNeg, byte id, float min, float max,int range=40)
		{
			this.NamePos = namePos;
			this.NameNeg = nameNeg;
			this.ID = id;
			this.MinValue = min;
			this.MaxValue = max;
			this.CurrentValue = 0;
			this.Range = range;
		}

		public float GetNegativeLimit()
		{
			if (CurrentValue < 0)
				return Math.Abs(CurrentValue);
			else
				return 0;
		}

		public float GetPostiveLimit()
		{
			if (CurrentValue > 0)
				return CurrentValue;
			else
				return 0;
		}

		public void Move(float step, byte direction)
		{
           // UnityEngine.Debug.Log(step.ToString());
			if(direction==0x01)
			{
				if (CurrentValue+(step/Range)>MaxValue)
					CurrentValue=MaxValue;
				else
					CurrentValue += (step/Range);
			}
			else
			{
				if (CurrentValue-(step/Range)<MinValue)
					CurrentValue=MinValue;
				else
					CurrentValue -= (step/Range);
			}
		}
		public void RestValue()
		{
			CurrentValue = 0;
		}
	}
}