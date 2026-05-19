using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class node_angle_entry : IComparable<node_angle_entry>
    {
        public int node_id;
        public double angle;


        public int CompareTo(node_angle_entry other)
        {
            if (other == null)
                return 1;

            else
                return this.angle.CompareTo(other.angle);

        }
    }
}
