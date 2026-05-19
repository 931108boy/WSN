using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class car
    {

        public double x, y; //car's position
        double speed;
        int num_charger;
        //public int charger_touse;
        public List<charger> mycharger;
        public List<moving_data> moving_seq;

        public List<request_event> charging_list;

        public int starttime;
        public int status;  // 0: stop, 1:moving forward, 2: wait, 3: moving back to station
        int target_node;
        public car(int sx, int sy, int ispeed, int ncharger)
        {
            x = sx; y = sy; speed = ispeed * common.TIME_UNIT;
            num_charger = ncharger;
            mycharger = new List<charger>();
            for (int i=0; i<common.num_charger_per_car; i++)
            {
                charger tmp = new charger((int)x, (int)y);
                tmp.q_target = 0;
                mycharger.Add(tmp);
            }
            //charger_touse = 0;
            moving_seq = new List<moving_data>();
            charging_list = new List<request_event>();

        }
        public void Do_process()
        {  //1: 前進 4:等待充電 3:回程 2: standby outside station
            double prex, prey;
            if ((status == 1 || status == 3))
            {
                //if ( moving_seq.Count() <=0 || charger_touse > num_charger-1) status = 2; //沒有charger，或已尋訪完目標，等待回程                    

                target_node = moving_seq.First().target_id;
                double target_dist = common.mydist(x, y, common.nmap.node[target_node].x, common.nmap.node[target_node].y);
                if (target_dist < speed)
                { //到達充電目的節點
                    prex = x; prey = y;
                    x = common.nmap.node[target_node].x;
                    y = common.nmap.node[target_node].y;
                    common.moving_distance += common.mydist(prex, prey, x, y);

                    if (common.debug_output)
                        common.ofs.WriteLine("node:{0} arrive: {1} status:{2}", target_node, common.current_time, status);
                    //移除本车中的request记录
                    remove_from_car_charging_list(moving_seq.First().event_id);
                    if (target_node > 0)
                        if (common.nmap.node[target_node].residual < common.nmap.node[target_node].tx_energy(common.default_pkt_leng))
                        {
                            common.missed_task++;
                        }
                        else
                        {
                            common.done_task++;
                            //common.ofs.WriteLine("Target:{0}-{1}", target_node, common.nmap.node[target_node].residual);
                        }    


                    //if (common.wait_for_charging && target_node != 0)
                    if (target_node != 0)
                    { //等待充電完成
                        status = 4;
                        common.nmap.node[target_node].status = 2;
                        common.nmap.node[target_node].charging_visual_until_time =
                            Math.Max(common.nmap.node[target_node].charging_visual_until_time,
                            common.current_time + common.CHARGING_VISUAL_HOLD_TICKS);
                        mycharger[0].q_target = target_node;
                        mycharger[0].status = 1;
                    }
                    else if (target_node == 0)
                    {
                        //回到基地台
                        status = 0;
                        moving_seq.RemoveAt(0);
                        
                        //假设立即完成充电
                        mycharger[0].recharge();
                        common.available_num_car = common.nmap.get_available_car();
                        common.min_next_charging_time = common.nmap.calcMinRedundentTime(common.available_num_car);
                    }
                }
                else
                { //keep on moving
                    prex = x; prey = y;
                    x += (speed / target_dist) * (common.nmap.node[target_node].x - x);
                    y += (speed / target_dist) * (common.nmap.node[target_node].y - y);
                    common.moving_distance += common.mydist(prex, prey, x, y);
                    common.redraw = true;
                    //Todo: check nearby target

                }
            }
            else if (status == 4) //wait for charging
            {
                if (mycharger[0].status == 0)
                { // 充電完成
                    //if (moving_seq.Count>0)
        
                    moving_seq.RemoveAt(0);
                    if (moving_seq.Count == 0)
                    {
                        if (common.car_no_return)
                            status = 2;
                        else
                            back_to_base();
                    }
                    else
                        status = 1;
                    if (common.dynamic_schedule || common.car_no_return)
                    {
                        if (common.dynamic_schedule)
                        {
                            if (common.nmap.calcMinRedundentTime(1) > 0)
                            {
                                move_requests_backto_list();
                                moving_seq.Clear();
                                status = 2;
                            }                               
                        }
                        else if (moving_seq.Count <= 0)
                        {
                            if (common.car_no_return)
                                status = 2;
                            else
                                back_to_base();
                        }

                        if ((status == 0 || status == 2)) //应该不会有status = 0 
                        {
                            common.available_num_car = common.nmap.get_available_car();
                            common.min_next_charging_time = common.nmap.calcMinRedundentTime(common.available_num_car);
                        }
                    }
                    else
                    {
                        if (moving_seq.Count() <= 0)
                        {
                             back_to_base();
                        }
                    }
                }
                else
                {
                    if (mycharger[0].status == 2)
                    { //电量不足，回基地台
                        mycharger[0].status = 0;
                        back_to_base();
                    }
                    else //继续充电
                        mycharger[0].Do_process();
                }
            }

        }
        public void back_to_base()
        {
            status = 3;
            moving_seq.Clear();
            move_requests_backto_list();
            moving_data md = new moving_data();
            md.event_id = -1;
            md.target_id = 0;
            moving_seq.Add(md); //0: 代表基地台位置
        }
        public request_event remove_from_car_charging_list(int event_id)
        {
            //移除event_list items
            foreach (request_event rq in charging_list)
            {
                if (rq.event_id == event_id)
                {
                    charging_list.Remove(rq);
                    return rq;
                }
            }
            return null;
        }
        public void move_requests_backto_list()
        {
            //从车中串列移回 request_charging_list

            foreach (request_event rq in charging_list)
            {
                common.nmap.requeue_car_request(rq);
            }
            charging_list.Clear();
        }
    }
}
