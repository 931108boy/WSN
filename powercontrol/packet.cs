using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class packet
    {
        // for packet types
        public const int SEND_HELLO = 0;
        public const int RECV_HELLO = 1;
        public const int SEND_UPDATE_GR = 2;
        public const int RECV_UPDATE_GR = 3;
        public const int SEND_DATA = 4;
        public const int RECV_DATA = 5;
        public const int FORWARD_DATA = 15;
        public const int BUILD_COST = 6;
        public const int GERR = 7;
        public const int CBR = 8;
        public const int LOST_EVENT = 9;
        public const int SEND_REQ = 10;
        public const int CHECK_REQ = 11;

        public const int PKT_LOST = 0;
        public const int EVENT_RECV = 1;
        public const int DETECT_FAIL = 2;
        // end of types

        public int source_id, dest_id;  //封包來源及目的ID
        public int pre_id, next_id;     //封包前一跳ID 及下一跳 ID
        public int sent_time;        //發送時間
        public int hop;
        public int fid;          // 父節點ID
        public double residual;      // 發送節點的剩餘電量
        public double consuming_speed;  // 發送節點目前估計耗電速度
        public double remain_work_time; //估計剩餘工作時間
        public double request_deadline;  // 估計送出充電請求的時間
        public double depletion_deadline; // 估計耗盡前需完成服務的時間
        public double predict_speed_low;   // 耗電速度區間下界
        public double predict_speed_high;  // 耗電速度區間上界
        public double predict_timeleft_low;  // 存活時間區間下界
        public double predict_timeleft_high; // 存活時間區間上界
        public double predict_charge_need_low;  // 充電需求區間下界
        public double predict_charge_need_high; // 充電需求區間上界
        public bool predict_out_of_window; // 是否預測落在危險區間外

        public double leng;          // 封包長度
        public int ev_id;

        public int ev_x, ev_y;   // just for Event triggered SEND_DATA

        public packet()
        {
        }
        public packet(packet pkt)
        {
            source_id = pkt.source_id;
            dest_id = pkt.dest_id;
            pre_id = pkt.pre_id;
            next_id = pkt.next_id;
            hop = pkt.hop;
            fid = pkt.fid;
            residual = pkt.residual;
            consuming_speed = pkt.consuming_speed;
            ev_x = pkt.ev_x;
            ev_y = pkt.ev_y;
            sent_time = pkt.sent_time;
            remain_work_time = pkt.remain_work_time;
            request_deadline = pkt.request_deadline;
            depletion_deadline = pkt.depletion_deadline;
            predict_speed_low = pkt.predict_speed_low;
            predict_speed_high = pkt.predict_speed_high;
            predict_timeleft_low = pkt.predict_timeleft_low;
            predict_timeleft_high = pkt.predict_timeleft_high;
            predict_charge_need_low = pkt.predict_charge_need_low;
            predict_charge_need_high = pkt.predict_charge_need_high;
            predict_out_of_window = pkt.predict_out_of_window;
            leng = pkt.leng;
            ev_id = pkt.ev_id;
        }
    }
}
