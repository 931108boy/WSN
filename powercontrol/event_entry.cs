using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class event_entry : IComparable<event_entry>
    {
        public int P_Id;
        public int residual;
        public int timeleft;
        public int T_time;  // 事件應處理時間
        public packet p;

        public int CompareTo(event_entry other)
        {
            if (other == null)
                return 1;

            else
                return this.T_time.CompareTo(other.T_time);

        }
    }
}
