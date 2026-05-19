using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class charger
    {
        public int x, y;  // position
        public double residual;
        public double speed; // charging speed
        public int status; //0: normal  1:stop charging
        public int q_target;
        public charger(int sx, int sy)
        {
            x = sx; y = sy;
            recharge();
            speed = (common.Origin_RESIDUAL*common.charging_speed/100 * common.TIME_UNIT);
            q_target = 0;
            status = 0;
        }
        public void recharge()
        {
            residual = common.num_charger_per_car * common.Origin_RESIDUAL;
        }
        public void Do_process()
        {
            double target_residual = common.target_ratio * common.Origin_RESIDUAL;
            double energy_to_charge=Math.Min(speed,target_residual-common.nmap.node[q_target].residual);
            
            if (residual < energy_to_charge)
            { //电源用尽，准备回基地台
                common.nmap.node[q_target].residual += residual;
                common.nmap.node[q_target].pre_charged_time = common.current_time;
                common.nmap.node[q_target].pre_residual = common.nmap.node[q_target].residual;
                common.nmap.node[q_target].status = 1;
                common.nmap.refresh_bpr_state(q_target, true);
                residual = 0;
                status = 2;
            }
            else
            {
                if (common.wait_for_charging && q_target > 0 && common.nmap.node[q_target].residual < target_residual)
                {
                    common.nmap.node[q_target].status = 2;
                    common.nmap.node[q_target].charging_visual_until_time =
                        Math.Max(common.nmap.node[q_target].charging_visual_until_time,
                        common.current_time + common.CHARGING_VISUAL_HOLD_TICKS);
                    common.nmap.node[q_target].residual += energy_to_charge;
                    residual -= energy_to_charge;
                }
                if (q_target > 0 && (!common.wait_for_charging || common.nmap.node[q_target].residual >= target_residual))
                {
                    common.nmap.node[q_target].status = 0; //完成充電
                    common.nmap.node[q_target].pre_charged_time = common.current_time;
                    common.nmap.node[q_target].pre_residual = common.nmap.node[q_target].residual;
                    common.nmap.refresh_bpr_state(q_target, true);
                    common.saveNum++;
                    if (residual < (common.target_ratio - common.request_threshold) * common.Origin_RESIDUAL)
                        status = 2; //可能不足完成下一个充电需求，准备回基地台
                    else
                        status = 0;
                }
            }
        }
    }
}
