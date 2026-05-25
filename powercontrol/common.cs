using System;

namespace WindowsFormsApplication1
{
    public static class common
    {
        public static double mydist(double x1, double y1, double x2, double y2)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
