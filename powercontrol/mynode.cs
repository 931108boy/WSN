using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class mynode
    {
        public int status = 0; // 0:normal, 1:sent request  2:charging
        public int pre_send_time; //no use?

        public int pre_charged_time;
        public double pre_residual;
        public bool has_request_reference;
        public double request_reference_residual;
        public int charging_visual_until_time;
        public double angle; //相對基地台的角度
        public int id;    // node id
        public int x, y;
        public int hop; // 距離基地台跳數
        public bool base_station; // 是否為基地台, sink

        public int[] fid = new int[1000];
        public double[] succ = new double[1000];
        public float[] prob = new float[1000];

        public int head_count, hit_count, prange, next_prange;

        public double ET;  //傳送至fid的耗能
        public double ER;  //接收封包的耗能
        public double consume_rate_scale;

        public List<event_entry> event_list; //待處理事件串列
        public List<child_info> children_id;
        public List<neighbor_info> n_list;
        public int f_ind;  // 可選上游節點數
        public double f_loading; // 下游期望負載值

        public double residual;


        public void clear_info()
        {
            int i;
            event_list = new List<event_entry>();
            children_id = new List<child_info>();
            n_list = new List<neighbor_info>();
            for (i = 0; i < 100; i++)
                fid[i] = -1;
            f_ind = 0;
            f_loading = 0;
            hop = common.MYINFINITE;
            //---------no use

            residual = common.Origin_RESIDUAL;
            head_count = 0;
            hit_count = 0;
            pre_charged_time = 0;
            pre_residual = residual;
            has_request_reference = false;
            request_reference_residual = 0;
            charging_visual_until_time = 0;
            consume_rate_scale = 1.0;
            status = 0;
        }

        public mynode()
        {
            event_list = new List<event_entry>();
            children_id = new List<child_info>();
            n_list = new List<neighbor_info>();
            clear_info();
        }

        public void setEvent(int PId, packet pkt, int Ptime, int preid)
        {
            event_entry temp_entry = new event_entry();
            packet newpkt = new packet(pkt);
            temp_entry.P_Id = PId;
            temp_entry.p = newpkt;
            temp_entry.p.pre_id = preid;
            temp_entry.T_time = Ptime;
            event_list.Add(temp_entry);
        }
        public void consuming_power(int type, double pkt_leng)
        {  // pkt is nouse now

            double energy;
            if (type == 0)
                energy = tx_energy(pkt_leng);
            else
                energy = rx_energy(pkt_leng);
            if (base_station) return;
            residual -= energy;

            if (residual < 0)
                residual = 0;

        }

        public double tx_energy(double pkt_leng)
        {
            return ET * consume_rate_scale * pkt_leng;
        }

        public double rx_energy(double pkt_leng)
        {
            return ER * consume_rate_scale * pkt_leng;
        }

        public double tx_energy_unit()
        {
            return ET * consume_rate_scale;
        }

        public double rx_energy_unit()
        {
            return ER * consume_rate_scale;
        }
        public double background_consume_speed()
        {
            double lifetime = Math.Max(common.sensor_background_lifetime_sec, common.TIME_UNIT);
            return common.Origin_RESIDUAL * Math.Max(0.01, consume_rate_scale) / lifetime;
        }
        public double background_consume_energy_per_tick()
        {
            return background_consume_speed() * common.TIME_UNIT;
        }
        public void consume_background_power()
        {
            if (base_station) return;
            residual -= background_consume_energy_per_tick();

            if (residual < 0)
                residual = 0;
        }
        public void update_info(packet pkt)
        {
            int i;
            if (pkt.hop < hop - 1)
            { // 找到較短的路
              //        fid = pkt->pre_id;
                fid[0] = pkt.source_id;
                succ[0] = 1;
                f_ind = 1;
                hop = pkt.hop + 1;
            }
            else if (pkt.hop == hop - 1 && hop != common.MYINFINITE)
            { // 同hop 則增加可用上游節點
                for (i = 0; i < f_ind; i++)
                    if (fid[i] == pkt.source_id) break;
                if (i == f_ind)
                {
                    fid[i] = pkt.source_id;
                    succ[i] = 1;
                    f_ind++;
                }
            }
        }


    }
}
