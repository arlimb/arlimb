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