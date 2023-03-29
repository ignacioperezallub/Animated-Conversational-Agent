using System;
using System.Collections.Generic;

//namespace Interpolation
//{
/*
    public struct Keyframe
    {
        public double time;
        public double value;

        public Keyframe(double t, double v)
        {
            time = t; value = v;
        }
    }

class Interpolation { 

    public static double CosineInterpolate(double t, List<Keyframe> keyframes)
    {
        double t1 = (t - keyframes[0].time) / (keyframes[1].time - keyframes[0].time);
        double y = (1 - Math.Cos(t1 * Math.PI)) / 2;
        return keyframes[0].value * (1 - y) + keyframes[1].value * y;
    }

    public static double LinearInterpolate(double t, List<Keyframe> keyframes)
    {
        double t1 = (t - keyframes[0].time) / (keyframes[1].time - keyframes[0].time);
        //double y = (1 - Math.Cos(t1 * Math.PI)) / 2;
        return keyframes[0].value * (1 - t1) + keyframes[1].value * t1;
    }
}*/