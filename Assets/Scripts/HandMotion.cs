/*  <Gets the current hand movement. Also gets the the applied force and the maximum force an object can take.>
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
using AssemblyCSharp;

public class HandMotion : EventArgs
{
    private HandMovement.Movement Motion;
    private float Force;
    private float Limit;
    public HandMotion(HandMovement.Movement motion,float limit, float force)
    {
        Motion = motion;
        Force = force;
        Limit = limit;
    }
    public HandMovement.Movement GetMotion()
    {
        return Motion;
    }

    public float GetForce()
    {
        return Force;
    }

    public float GetLimit()
    {
        return Limit;
    }
}