using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class request_event
    {
        public static int max_id = 0;
        public int event_id;
        public int node_id;
        public double residual;
        public int request_time;
        public double timeleft;
        public double consuming_speed;
        public double request_reference_residual;
        public double request_deadline;
        public double request_deadline_low;
        public double request_deadline_high;
        public double depletion_deadline;
        public double deadline;
        public double predict_speed_low;
        public double predict_speed_high;
        public double predict_timeleft_low;
        public double predict_timeleft_high;
        public double predict_charge_need_low;
        public double predict_charge_need_high;
        public bool predict_out_of_window;
        public double deadline_low;
        public double deadline_high;
        public bool is_proactive;
        //for REDF
        public double angle, redundant_time;
        //double estimate_deplete_speed;
        public request_event(int id, double res, int rtime, double tl)
        {
            event_id = -1;
            node_id = id;
            residual = res;
            request_time = rtime;
            timeleft = Math.Max(0, tl);
            consuming_speed = (timeleft > 0) ? residual / timeleft : 0;
            request_reference_residual = residual;
            request_deadline = request_time;
            request_deadline_low = request_deadline;
            request_deadline_high = request_deadline;
            depletion_deadline = request_time + timeleft;
            predict_speed_low = consuming_speed;
            predict_speed_high = consuming_speed;
            predict_timeleft_low = timeleft;
            predict_timeleft_high = timeleft;
            predict_charge_need_low = 0;
            predict_charge_need_high = 0;
            predict_out_of_window = false;
            deadline = depletion_deadline;
            deadline_low = deadline;
            deadline_high = deadline;
            is_proactive = false;
            angle = 0;
            redundant_time = 0;
        }
        public request_event(request_event origin)
        {
            event_id = origin.event_id;
            node_id = origin.node_id;
            residual = origin.residual;
            request_time = origin.request_time;
            timeleft = origin.timeleft;
            consuming_speed = origin.consuming_speed;
            request_reference_residual = origin.request_reference_residual;
            request_deadline = origin.request_deadline;
            request_deadline_low = origin.request_deadline_low;
            request_deadline_high = origin.request_deadline_high;
            depletion_deadline = origin.depletion_deadline;
            deadline = origin.deadline;
            predict_speed_low = origin.predict_speed_low;
            predict_speed_high = origin.predict_speed_high;
            predict_timeleft_low = origin.predict_timeleft_low;
            predict_timeleft_high = origin.predict_timeleft_high;
            predict_charge_need_low = origin.predict_charge_need_low;
            predict_charge_need_high = origin.predict_charge_need_high;
            predict_out_of_window = origin.predict_out_of_window;
            deadline_low = origin.deadline_low;
            deadline_high = origin.deadline_high;
            is_proactive = origin.is_proactive;
            angle = origin.angle;
            redundant_time = origin.redundant_time;
        }
    }
}
