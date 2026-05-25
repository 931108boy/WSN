using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class nodemap
    {

        public mynode[] node;
        public int max_node, max_base;

        public int width, height; // for grouping

        public List<node_angle_entry> node_angle_list;
        public List<car> car_list;
        public List<charger> charger_list;

        public List<request_event> request_charging_list;
        public double[,] distance_matrix;
        public request_event[] bpr_state_table;

        const double BPR_DEADLINE_BAND_RATIO = 0.25;
        const double BPR_MIN_CONSUME_SPEED = 1e-6;

        public double cal_succ(int s, int e, double k, double pd)
        {
            double prange, dist;
            return 1; //不使用成功率计算
            prange = node[s].prange;
            dist = common.mydist(node[s].x, node[s].y, node[e].x, node[e].y);
            if (dist > prange * 0.9) return pd;
            else if ((k * pd * prange * prange) / (dist * dist) > 1)
                return 1;
            else return (k * pd * prange * prange) / (dist * dist);
        }
        public nodemap(int basesize, int nodesize)
        {
            max_base = basesize;
            max_node = nodesize;
            node = new mynode[nodesize];
            for (int i = 0; i < nodesize; i++)
                node[i] = new mynode();
            car_list = new List<car>();

            charger_list = new List<charger>();
            request_charging_list = new List<request_event>();
            node_angle_list = new List<node_angle_entry>();
            //store the distance for each node-pair
            distance_matrix = new double[nodesize, nodesize];
            bpr_state_table = new request_event[nodesize];
            //init();
        }
        void CUCK(List<car> cList)
        { //注意: 應該已改成 位置代表節點，各點內的有充電順序與充電車輛兩個訊息? 
            int carn, currentnode, samecnt, genIdx;
            int[] prenode = new int[common.max_num_car];
            bool[] firststate = new bool[common.max_num_car];
            int[] carmap = new int[common.available_num_car];

            cuckooSearch.event_list = get_EDF_list(common.nmap.request_charging_list, 0);
            if (cuckooSearch.event_list.Count == 0)
            {
                samecnt = common.current_time;
            }

            common.call_gene_count++;

            //mapping usable cars
            int mapi = 0;
            for (int i = 0; i < common.max_num_car; i++)
            {
                if (cList[i].status == 0 || cList[i].status == 2)
                {
                    carmap[mapi++] = i;
                }
            }
            if (cuckooSearch.event_list.Count > 1)
            {
                int start_num_car = (int)Math.Ceiling(cuckooSearch.event_list.Count() / (double)common.num_charger_per_car);
                start_num_car = 1;
                for (int ci = start_num_car; ci <= mapi; ci++)
                {
                    cuckooSearch.cs(common.population_size, ci);
                    if (cuckooSearch.best.fitness < 1000000) break;
                }

                for (int i = 0; i < cuckooSearch.best.num_node; i++)
                {
                    int j = cuckooSearch.best.find_Seq(i);
                    carn = carmap[cuckooSearch.best.charge_car_no[j]];
                    currentnode = cuckooSearch.event_list[j].node_id;
                    if (currentnode != 0)
                    {
                        moving_data md = new moving_data();
                        md.target_id = currentnode;
                        md.event_id = cuckooSearch.event_list[j].event_id;
                        cList[carn].moving_seq.Add(md);

                        //移除event_list items，指派充電車後就移除request
                        move_request_to_car(md.event_id, carn);
                    }
                }
            }
            else
            {
                moving_data md = new moving_data();
                md.target_id = cuckooSearch.event_list[0].node_id;
                md.event_id = cuckooSearch.event_list[0].event_id;
                cList[carmap[0]].moving_seq.Add(md);

                //common.total_gene_iterations += max_same_cnt;
                move_request_to_car(md.event_id, carmap[0]);               
            }
        }

        void PSO(List<car> cList)
        { //注意: 應該已改成 位置代表節點，各點內的有充電順序與充電車輛兩個訊息? 
            int carn, currentnode, samecnt;
            int[] prenode = new int[common.max_num_car];
            bool[] firststate = new bool[common.max_num_car];
            int[] carmap = new int[common.available_num_car];
            pso mypso = new pso();
            pso.event_list = get_EDF_list(common.nmap.request_charging_list, 0);

            if (pso.event_list.Count == 0)
            {
                samecnt = common.current_time;
            }

            common.call_gene_count++;

            //mapping usable cars
            int mapi = 0;
            for (int i = 0; i < common.max_num_car; i++)
            {
                if (cList[i].status == 0 || cList[i].status == 2)
                {
                    carmap[mapi++] = i;
                }
            }
            if (pso.event_list.Count > 1)
            {
                int start_num_car = (int)Math.Ceiling(pso.event_list.Count() / (double)common.num_charger_per_car);
                start_num_car = 1;
                for (int ci = start_num_car; ci <= mapi; ci++)
                {
                    mypso.psoSearch(pso.event_list.Count, ci);
                    if (mypso.global_best.fitness < 1000000) break;
                }

                for (int i = 0; i < mypso.global_best.num_node; i++)
                {
                    int j = mypso.global_best.find_Seq(i);
                    carn = carmap[mypso.global_best.charge_car_no[j]];
                    currentnode = pso.event_list[j].node_id;
                    if (currentnode != 0)
                    {
                        moving_data md = new moving_data();
                        md.target_id = currentnode;
                        md.event_id = pso.event_list[j].event_id;
                        cList[carn].moving_seq.Add(md);

                        //移除event_list items，指派充電車後就移除request
                        move_request_to_car(md.event_id, carn);
                    }
                }
            }
            else
            {
                moving_data md = new moving_data();
                md.target_id = pso.event_list[0].node_id;
                md.event_id = pso.event_list[0].event_id;
                cList[carmap[0]].moving_seq.Add(md);

                //common.total_gene_iterations += max_same_cnt;
                move_request_to_car(md.event_id, carmap[0]);
            }
        }
        int find_max_hop()
        {
            int max_hop = 0;
            for (int i = 1; i < max_node; i++)
                if (node[i].hop != common.MYINFINITE && node[i].hop > max_hop) max_hop = node[i].hop;
            return max_hop;
        }
        public double check_node_load()
        {
            int mh, h, i, j;
            double all_load = 0, avg_load, var_load, max_load = 0;
            mh = find_max_hop();
            for (i = 0; i < max_node; i++)
            {
                if (node[i].f_ind == 0)
                    node[i].f_loading = 100000;
                else
                    node[i].f_loading = 1; // 每個節點的起始負載期望值都相同
            }
            for (h = mh; h > 0; h--)
            {
                for (i = 1; i < max_node; i++)
                {
                    if (node[i].hop == h)
                    {
                        all_load += node[i].f_loading;
                        if (max_load < node[i].f_loading * node[i].prange * node[i].prange * common.Eamp)
                            max_load = node[i].f_loading * node[i].prange * node[i].prange * common.Eamp;
                        for (j = 0; j < node[i].f_ind; j++)
                        {
                            node[node[i].fid[j]].f_loading += node[i].f_loading / node[i].f_ind;
                        }
                    }
                }
            }
            avg_load = all_load / (max_node - 1);
            var_load = 0;
            for (i = 1; i < max_node; i++)
            {
                var_load += Math.Pow(node[i].f_loading - avg_load, 2);
            }
            var_load /= (max_node - 1);
            //        return var_load;
            return max_load;
        }
        void build_neighbor2(int id, bool init_used)
        { // 該與 build_neighbor整合
            int i;
            double dist;
            neighbor_info temp;
            for (i = 0; i < max_node; i++)
            {
                if (i == id) continue;
                dist = common.mydist(node[i].x, node[i].y, node[id].x, node[id].y);
                if (dist <= node[i].prange)
                {
                    temp.id = i;
                    temp.dist = dist;
                    //temp.dist = 1;  // using hop for counting cost
                    temp.used = init_used;
                    node[id].n_list.Add(temp);
                }
            }
        }
        public void setup_succ()
        {
            for (int i = max_base; i < max_node; i++)
            {
                for (int j = 0; j < node[i].f_ind; j++)
                    node[i].succ[j] = cal_succ(i, node[i].fid[j], 1, 0.5);
            }
        }
        public void setup_prob(float fprob, float tprob)
        {
            //設定每個link的傳輸成功機率
            for (int i = max_base; i < max_node; i++)
            {
                for (int j = 0; j < node[i].f_ind; j++)
                    node[i].prob[j] = (float)(common.rand.NextDouble() * (tprob - fprob) + fprob);
            }
        }
        public void rebuild_tree()
        {
            int i, x;
            Queue<int> myQ = new Queue<int>();
            for (i = max_base; i < max_node; i++)
            {
                //           node[i].clear_info();
                node[i].n_list = new List<neighbor_info>();
                for (int j = 0; j < 1000; j++)
                    node[i].fid[j] = -1;
                node[i].f_ind = 0;
                node[i].f_loading = 0;
                node[i].hop = common.MYINFINITE;
            }
            for (i = 0; i < max_node; i++)
                build_neighbor2(i, true);
            myQ.Enqueue(0);
            while (myQ.Count > 0)
            {
                x = myQ.Dequeue();
                foreach (neighbor_info nx in node[x].n_list)
                {
                    if (node[nx.id].hop > node[x].hop + 1)
                    { // 找到較短的路
                        node[nx.id].fid[0] = x;
                        node[nx.id].f_ind = 1;
                        node[nx.id].succ[0] = cal_succ(nx.id, x, 1, 0.5);
                        node[nx.id].hop = node[x].hop + 1;
                        myQ.Enqueue(nx.id);
                    }
                    else if (node[nx.id].hop == node[x].hop + 1)
                    { // 同hop 則增加可用上游節點
                        for (i = 0; i < node[nx.id].f_ind; i++)
                            if (node[nx.id].fid[i] == x) break;
                        if (i == node[nx.id].f_ind)
                        {
                            node[nx.id].fid[i] = x;
                            node[nx.id].succ[i] = cal_succ(nx.id, x, 1, 0.5);
                            node[nx.id].f_ind++;
                        }
                    }

                }
            }
        }

        //for charging scheduling
        public int get_available_car()
        {
            int car_ready = 0;
            foreach (car c in car_list)
                if (c.status == 0 || c.status == 2)
                {
                    car_ready++;
                }
            return car_ready;
        }
        public void init()
        {
            car_list.Clear();

            charger_list.Clear();
            request_charging_list.Clear();
            for (int i = 0; i < max_node; i++)
            {
                node[i].clear_info();
            }

            if (bpr_state_table == null || bpr_state_table.Length != max_node)
                bpr_state_table = new request_event[max_node];
            else
                Array.Clear(bpr_state_table, 0, bpr_state_table.Length);

            cal_distance(max_node);
        }

        public void cal_distance(int nodesize)
        {
            for (int i = 0; i < nodesize; i++)
                for (int j = i + 1; j < nodesize; j++)
                {
                    distance_matrix[i, j] = common.mydist(node[i].x, node[i].y, node[j].x, node[j].y);
                    distance_matrix[j, i] = distance_matrix[i, j];
                }
        }
        int find_dispatch_car(List<car> cList)
        {
            for (int i = 0; i < cList.Count && i < common.max_num_car; i++)
            {
                if (cList[i].status == 0 || cList[i].status == 2)
                    return i;
            }
            return -1;
        }
        int find_dispatch_car_at_base(List<car> cList)
        {
            for (int i = 0; i < cList.Count && i < common.max_num_car; i++)
            {
                if (cList[i].status == 0)
                    return i;
            }
            return -1;
        }
        double get_request_threshold_energy()
        {
            return common.Origin_RESIDUAL * common.request_threshold;
        }
        double get_service_floor_energy(int node_id)
        {
            if (node_id < 0 || node_id >= max_node)
                return 0;
            return 0.0;
        }
        double get_bpr_deadline_threshold()
        {
            return Math.Max(1.0, common.fix_waiting_time);
        }
        double estimate_node_consume_speed(int node_id)
        {
            double elapsed = Math.Max(1.0, common.current_time - node[node_id].pre_charged_time);
            double consumed = Math.Max(0.0, node[node_id].pre_residual - node[node_id].residual);
            double speed = consumed / elapsed;

            if (double.IsNaN(speed) || double.IsInfinity(speed) || speed < BPR_MIN_CONSUME_SPEED)
            {
                speed = Math.Max(BPR_MIN_CONSUME_SPEED, node[node_id].background_consume_speed());
            }

            return speed;
        }
        public double get_estimated_consume_speed(int node_id)
        {
            return estimate_node_consume_speed(node_id);
        }
        public double get_service_floor(int node_id)
        {
            return get_service_floor_energy(node_id);
        }
        void ensure_bpr_state_table()
        {
            if (bpr_state_table == null || bpr_state_table.Length != max_node)
                bpr_state_table = new request_event[max_node];
        }
        void normalize_request_deadline_band(request_event rq)
        {
            if (rq == null)
                return;

            double band = get_bpr_deadline_threshold();
            rq.request_deadline_low = Math.Max(common.current_time, rq.request_deadline - band);
            rq.request_deadline_high = Math.Max(rq.request_deadline_low, rq.request_deadline + band);
        }
        void normalize_deadline_band(request_event rq)
        {
            if (rq == null)
                return;

            if (rq.predict_timeleft_high > 0 && rq.predict_timeleft_high >= rq.predict_timeleft_low)
            {
                rq.deadline_low = Math.Max(common.current_time, rq.request_time + rq.predict_timeleft_low);
                rq.deadline_high = Math.Max(rq.deadline_low, rq.request_time + rq.predict_timeleft_high);
                return;
            }

            double currentTimeLeft = Math.Max(0.0, rq.deadline - common.current_time);
            double band = Math.Max(1.0, currentTimeLeft * BPR_DEADLINE_BAND_RATIO);
            rq.deadline_low = Math.Max(common.current_time, rq.deadline - band);
            rq.deadline_high = Math.Max(rq.deadline_low, rq.deadline + band);
        }
        void finalize_request_event(request_event rq, bool proactiveService)
        {
            if (rq == null)
                return;

            rq.request_deadline = Math.Max(rq.request_time, rq.request_deadline);
            rq.depletion_deadline = Math.Max(rq.request_deadline, rq.depletion_deadline);
            rq.deadline = proactiveService ? rq.request_deadline : rq.depletion_deadline;
            normalize_request_deadline_band(rq);
            normalize_deadline_band(rq);
        }
        request_event build_sensor_state(int node_id)
        {
            if (node_id < max_base || node_id >= max_node)
                return null;

            double speed = estimate_node_consume_speed(node_id);
            double requestResidual = get_request_threshold_energy();
            double serviceFloor = get_service_floor_energy(node_id);
            double residualToDepletion = node[node_id].residual - serviceFloor;
            if (residualToDepletion <= 0)
                return null;

            double timeToDepletion = residualToDepletion / speed;
            if (double.IsNaN(timeToDepletion) || double.IsInfinity(timeToDepletion) || timeToDepletion <= 0)
                return null;

            double residualToRequest = node[node_id].residual - requestResidual;
            double timeToRequest = (residualToRequest <= 0) ? 0 : residualToRequest / speed;

            request_event rq = new request_event(node_id, node[node_id].residual, common.current_time, timeToDepletion);
            rq.consuming_speed = speed;
            rq.predict_speed_low = speed;
            rq.predict_speed_high = speed;
            rq.predict_timeleft_low = timeToDepletion;
            rq.predict_timeleft_high = timeToDepletion;
            rq.request_deadline = common.current_time + timeToRequest;
            rq.depletion_deadline = common.current_time + timeToDepletion;
            rq.is_proactive = false;
            finalize_request_event(rq, false);
            return rq;
        }
        public void refresh_bpr_state(int node_id, bool force)
        {
            ensure_bpr_state_table();
            if (node_id < max_base || node_id >= max_node)
                return;
            if (node[node_id].status != 0)
                return;

            request_event nextState = build_sensor_state(node_id);
            if (nextState == null)
            {
                bpr_state_table[node_id] = null;
                return;
            }

            if (!force && bpr_state_table[node_id] != null)
            {
                double diff = Math.Abs(nextState.request_deadline - bpr_state_table[node_id].request_deadline);
                if (diff < get_bpr_deadline_threshold())
                    return;
            }

            bpr_state_table[node_id] = nextState;
        }
        public void refresh_all_bpr_states()
        {
            ensure_bpr_state_table();
            for (int nid = max_base; nid < max_node; nid++)
            {
                if (node[nid].status == 0)
                    refresh_bpr_state(nid, true);
            }
        }
        public void register_on_demand_request(request_event rq)
        {
            if (rq == null)
                return;

            ensure_bpr_state_table();
            request_event snapshot = new request_event(rq);
            snapshot.is_proactive = false;
            snapshot.request_deadline = snapshot.request_time;
            snapshot.request_deadline_low = snapshot.request_deadline;
            snapshot.request_deadline_high = snapshot.request_deadline;
            snapshot.depletion_deadline = Math.Max(snapshot.request_deadline, snapshot.depletion_deadline);
            snapshot.deadline = snapshot.depletion_deadline;
            normalize_deadline_band(snapshot);
            bpr_state_table[snapshot.node_id] = snapshot;
        }
        request_event build_predicted_request(int node_id, bool proactive)
        {
            if (node_id < max_base || node_id >= max_node)
                return null;
            if (node[node_id].status != 0)
                return null;

            ensure_bpr_state_table();
            request_event sensorState = bpr_state_table[node_id];
            if (sensorState == null || sensorState.depletion_deadline <= common.current_time)
            {
                refresh_bpr_state(node_id, true);
                sensorState = bpr_state_table[node_id];
            }
            if (sensorState == null || sensorState.request_deadline <= common.current_time)
                return null;

            request_event rq = new request_event(sensorState);
            rq.request_time = common.current_time;
            rq.timeleft = Math.Max(0.0, rq.depletion_deadline - common.current_time);
            rq.is_proactive = proactive;
            if (proactive)
            {
                rq.predict_timeleft_low = 0;
                rq.predict_timeleft_high = 0;
            }
            finalize_request_event(rq, proactive);
            return rq;
        }
        HashSet<int> get_reserved_node_ids()
        {
            HashSet<int> reserved = new HashSet<int>();

            foreach (request_event rq in request_charging_list)
                reserved.Add(rq.node_id);

            foreach (car c in car_list)
            {
                foreach (request_event rq in c.charging_list)
                    reserved.Add(rq.node_id);

                foreach (moving_data md in c.moving_seq)
                    if (md.target_id > 0)
                        reserved.Add(md.target_id);
            }

            return reserved;
        }
        double estimate_bpr_window(int maxTask)
        {
            if (maxTask <= 0)
                return 0;

            int minx = node[0].x;
            int maxx = node[0].x;
            int miny = node[0].y;
            int maxy = node[0].y;

            for (int i = 1; i < max_node; i++)
            {
                minx = Math.Min(minx, node[i].x);
                maxx = Math.Max(maxx, node[i].x);
                miny = Math.Min(miny, node[i].y);
                maxy = Math.Max(maxy, node[i].y);
            }

            double side = Math.Max(1.0, Math.Max(maxx - minx, maxy - miny));
            double pathLength;

            if (maxTask <= 1)
            {
                pathLength = 2.0 * Math.Sqrt(2.0) * side;
            }
            else
            {
                double denominator = Math.Sqrt(maxTask) - 1.0;
                if (denominator <= 0)
                    pathLength = (maxTask + 1) * Math.Sqrt(2.0) * side;
                else
                    pathLength = ((maxTask - 1) * side) / denominator + 2.0 * Math.Sqrt(2.0) * side;
            }

            double travelWindow = pathLength / Math.Max(common.car_speed * common.TIME_UNIT, 1e-6);
            double chargeEnergy = Math.Max(0.0, (common.target_ratio - common.request_threshold) * common.Origin_RESIDUAL);
            double chargeWindow = 0.0;

            if (chargeEnergy > 0)
                chargeWindow = (chargeEnergy / Math.Max(common.charging_speed, 1e-6)) / common.TIME_UNIT;

            return Math.Max(1.0, travelWindow + chargeWindow * maxTask);
        }
        double estimate_bpr_network_consume_speed()
        {
            double total = 0;
            int count = 0;

            for (int nid = max_base; nid < max_node; nid++)
            {
                double speed = estimate_node_consume_speed(nid);
                if (double.IsNaN(speed) || double.IsInfinity(speed) || speed <= 0)
                    continue;
                total += speed;
                count++;
            }

            if (count <= 0)
                return BPR_MIN_CONSUME_SPEED;
            return Math.Max(BPR_MIN_CONSUME_SPEED, total / count);
        }
        double estimate_bpr_return_time()
        {
            int minx = node[0].x;
            int maxx = node[0].x;
            int miny = node[0].y;
            int maxy = node[0].y;

            for (int i = 1; i < max_node; i++)
            {
                minx = Math.Min(minx, node[i].x);
                maxx = Math.Max(maxx, node[i].x);
                miny = Math.Min(miny, node[i].y);
                maxy = Math.Max(maxy, node[i].y);
            }

            double side = Math.Max(1.0, Math.Max(maxx - minx, maxy - miny));
            return Math.Max(1.0, Math.Sqrt(2.0) * side / Math.Max(common.car_speed * common.TIME_UNIT, 1e-6));
        }
        double estimate_bpr_request_slack_window()
        {
            double slackSum = 0;
            int count = 0;
            double requestResidual = get_request_threshold_energy();

            for (int nid = max_base; nid < max_node; nid++)
            {
                double slackEnergy = requestResidual - get_service_floor_energy(nid);
                if (slackEnergy <= 0)
                    continue;

                double speed = estimate_node_consume_speed(nid);
                if (double.IsNaN(speed) || double.IsInfinity(speed) || speed <= 0)
                    continue;

                slackSum += slackEnergy / speed;
                count++;
            }

            if (count <= 0)
                return Math.Max(1.0, requestResidual / estimate_bpr_network_consume_speed());
            return Math.Max(1.0, slackSum / count);
        }
        double estimate_bpr_max_wait_window(int maxTask)
        {
            double currentMission = estimate_bpr_window(maxTask);
            double nextMissionLead = Math.Max(0.0, currentMission - estimate_bpr_return_time());
            return currentMission + nextMissionLead;
        }
        int estimate_bpr_max_task_count(car chargeCar)
        {
            if (common.fixed_bpr_max_task > 0)
                return common.fixed_bpr_max_task;

            double availableChargeEnergy = common.num_charger_per_car * common.Origin_RESIDUAL;
            if (chargeCar != null && chargeCar.mycharger.Count > 0)
            {
                availableChargeEnergy = chargeCar.mycharger[0].residual;
                if (chargeCar.status == 0)
                    availableChargeEnergy = Math.Max(availableChargeEnergy, common.num_charger_per_car * common.Origin_RESIDUAL);
            }

            double energyPerTask = Math.Max((common.target_ratio - common.request_threshold) * common.Origin_RESIDUAL, 1e-6);
            int energyBound = Math.Max(1, (int)Math.Floor(availableChargeEnergy / Math.Max(energyPerTask, 1e-6)));
            double requestSlackWindow = estimate_bpr_request_slack_window();
            int maxTask = 1;

            for (int candidate = 1; candidate <= energyBound; candidate++)
            {
                if (estimate_bpr_max_wait_window(candidate) > requestSlackWindow)
                    break;
                maxTask = candidate;
            }

            return Math.Max(1, maxTask);
        }
        request_event clone_as_proactive_request(request_event candidate)
        {
            request_event proactive = new request_event(candidate);
            proactive.event_id = request_event.max_id++;
            proactive.request_time = common.current_time;
            proactive.timeleft = Math.Max(0.0, proactive.request_deadline - common.current_time);
            proactive.predict_timeleft_low = 0;
            proactive.predict_timeleft_high = 0;
            proactive.is_proactive = true;
            finalize_request_event(proactive, true);
            return proactive;
        }
        List<request_event> build_bpr_task_pool(int maxTask)
        {
            List<request_event> onDemandList = get_EDF_list(common.nmap.request_charging_list, 0);
            List<request_event> taskPool = new List<request_event>();

            if (onDemandList.Count > maxTask)
                onDemandList = onDemandList.GetRange(0, maxTask);

            foreach (request_event rq in onDemandList)
            {
                rq.is_proactive = false;
                finalize_request_event(rq, false);
                taskPool.Add(rq);
            }

            int proactiveSlots = maxTask - taskPool.Count;
            if (proactiveSlots <= 0)
                return taskPool;

            HashSet<int> reservedNodes = get_reserved_node_ids();
            List<request_event> futureCandidates = new List<request_event>();

            for (int nid = max_base; nid < max_node; nid++)
            {
                if (reservedNodes.Contains(nid))
                    continue;
                if (node[nid].status != 0)
                    continue;

                request_event predicted = build_predicted_request(nid, true);
                if (predicted == null)
                    continue;

                futureCandidates.Add(predicted);
            }

            futureCandidates.Sort(delegate (request_event req1, request_event req2)
            {
                return req1.request_deadline.CompareTo(req2.request_deadline);
            });

            double missionWindow = estimate_bpr_window(maxTask);
            for (int i = 0; i < futureCandidates.Count && proactiveSlots > 0; i++)
            {
                request_event anchor = futureCandidates[i];
                double windowStart = anchor.request_deadline;
                double windowEnd = windowStart + missionWindow;
                List<request_event> bottleList = futureCandidates.FindAll(delegate (request_event rq)
                {
                    return rq.request_deadline_high >= windowStart && rq.request_deadline_low <= windowEnd;
                });

                if (bottleList.Count <= maxTask)
                    continue;

                int addCount = Math.Min(proactiveSlots, bottleList.Count - maxTask);
                List<request_event> selectable = bottleList.FindAll(delegate (request_event rq)
                {
                    return !reservedNodes.Contains(rq.node_id);
                });

                while (addCount > 0 && selectable.Count > 0)
                {
                    int pickedIndex = common.rand.Next(selectable.Count);
                    request_event candidate = selectable[pickedIndex];
                    selectable.RemoveAt(pickedIndex);

                    taskPool.Add(clone_as_proactive_request(candidate));
                    reservedNodes.Add(candidate.node_id);
                    futureCandidates.RemoveAll(delegate (request_event rq)
                    {
                        return rq.node_id == candidate.node_id;
                    });
                    proactiveSlots--;
                    addCount--;
                }

                if (proactiveSlots <= 0)
                    break;

                i = -1;
            }

            return taskPool;
        }
        void assign_request_to_car(car chargeCar, request_event rq, int carno)
        {
            moving_data md = new moving_data();
            md.event_id = rq.event_id;
            md.target_id = rq.node_id;
            chargeCar.moving_seq.Add(md);

            if (rq.is_proactive)
            {
                node[rq.node_id].status = 1;
                chargeCar.charging_list.Add(rq);
            }
            else
            {
                move_request_to_car(rq.event_id, carno);
            }
        }
        public double calcMinRedundentTime(int num_car_used)
        {
            List<request_event> edf_list, predict_list;
            request_event temp;
            List<charge_info> temp_list = new List<charge_info>();
            double[] accu_time = new double[common.max_num_car];
            int[] charger_left = new int[common.max_num_car];
            bool[] firststate = new bool[common.max_num_car];
            int[] prenode = new int[common.max_num_car];
            double total_dist = 0, min_redundent_time, redundent_time;
            int carn, dest_node, list_cnt;

            charge_info tmp;

            if (num_car_used <= 0) return -2;
            list_cnt = common.nmap.request_charging_list.Count();
            if (list_cnt <= 0) return 1000000; //沒有充電需求

            predict_list = new List<request_event>();
            for (int i = 0; i < list_cnt; i++)
            {
                temp = new request_event(common.nmap.request_charging_list[i]);
                predict_list.Add(temp);
            }
            if (common.method_sel <= 2 || common.method_sel ==5 || common.method_sel==6) //EDF, GENE, PSO, Cuckoo
                edf_list = get_EDF_list(predict_list, 0);
//            else if (common.method_sel <= 7)
//                edf_list = get_NJF_list(predict_list, 0);
            else //Lin'method, no MRT, 立即执行, M_LIN
                return -1;

            for (carn = 0; carn < num_car_used; carn++)
            {
                accu_time[carn] = common.current_time;
                firststate[carn] = true;
                charger_left[carn] = common.num_charger_per_car;
                prenode[carn] = -1;
            }
            temp_list.Clear();
            //平均分配充電工作給充電車
            carn = 0;
            for (int i = 0; i < edf_list.Count(); i++)
            {
                tmp = new charge_info();
                tmp.car_no = carn;
                tmp.destid_in_eventlist = i; //紀錄是在edf_list中的第幾項
                temp_list.Add(tmp);
                carn = (carn + 1) % num_car_used;
            }
            min_redundent_time = 10000000;
            for (int i = 0; i < temp_list.Count(); i++)
            {
                carn = temp_list[i].car_no;

                dest_node = edf_list[temp_list[i].destid_in_eventlist].node_id;
                if (firststate[carn])
                {
                    accu_time[carn] += common.mydist(common.nmap.car_list[carn].x, common.nmap.car_list[carn].y, common.nmap.node[dest_node].x, common.nmap.node[dest_node].y) / common.car_speed;
                    total_dist += common.mydist(common.nmap.car_list[carn].x, common.nmap.car_list[carn].y, common.nmap.node[dest_node].x, common.nmap.node[dest_node].y);
                    firststate[carn] = false;
                }
                else
                {
                    if (charger_left[carn] == 0)
                    {
                        //回基地台
                        accu_time[carn] += common.mydist(common.nmap.car_list[carn].x, common.nmap.car_list[carn].y, common.nmap.node[0].x, common.nmap.node[0].y) / common.car_speed;
                        total_dist += common.mydist(common.nmap.car_list[carn].x, common.nmap.car_list[carn].y, common.nmap.node[0].x, common.nmap.node[0].y);
                        charger_left[carn] = common.num_charger_per_car;
                        //
                        accu_time[carn] += common.mydist(common.nmap.node[0].x, common.nmap.node[0].y, common.nmap.node[dest_node].x, common.nmap.node[dest_node].y) / common.car_speed;
                        total_dist += common.mydist(common.nmap.node[0].x, common.nmap.node[0].y, common.nmap.node[dest_node].x, common.nmap.node[dest_node].y);
                    }
                    else
                    {
                        accu_time[carn] += (common.nmap.distance_matrix[prenode[carn], dest_node] / common.car_speed);
                        total_dist += common.nmap.distance_matrix[prenode[carn], dest_node];
                    }
                }
                prenode[carn] = dest_node;
                charger_left[carn]--;

                redundent_time = (edf_list[temp_list[i].destid_in_eventlist].deadline) - accu_time[carn];
                if (redundent_time < 0)
                    return -1; //檢查失敗，無法於時限內完成
                else
                {
                    if (min_redundent_time > redundent_time)
                        min_redundent_time = redundent_time;
                }

                if (common.charging_time_include)
                {
                    int edf_order = temp_list[i].destid_in_eventlist;
                    accu_time[carn] += (common.Origin_RESIDUAL * common.target_ratio - edf_list[edf_order].residual + (common.current_time-edf_list[edf_order].request_time)*edf_list[edf_order].consuming_speed) / common.charging_speed;

                }

            }

            if (list_cnt >= common.num_charger_per_car * num_car_used)
                return common.current_time;
            else
                return common.current_time + 1 * min_redundent_time;
            //以下是否要修改权重?
            /*
            if (predict_level<=1) //宽松预测
                return Math.Truncate(common.current_time + 1 * min_redundent_time);
            else
                return Math.Truncate(common.current_time + 1 * min_redundent_time);
             */
        }
        public int find_bottle(List<request_event> charging_list, int num_car_used)
        {
            List<charge_info> temp_list = new List<charge_info>();
            double[] accu_time = new double[common.max_num_car];
            int[] charger_left = new int[common.max_num_car];
            bool[] firststate = new bool[common.max_num_car];
            int[] prenode = new int[common.max_num_car];
            double total_dist = 0, min_redundent_time, redundent_time;
            int carn, dest_node, list_cnt;

            charge_info tmp;

            if (num_car_used <= 0) return -2;
            list_cnt = charging_list.Count();
            if (list_cnt <= 0) return 1000000; //沒有充電需求

            for (carn = 0; carn < num_car_used; carn++)
            {
                accu_time[carn] = common.current_time;
                firststate[carn] = true;
                charger_left[carn] = common.num_charger_per_car;
                prenode[carn] = -1;
            }
            temp_list.Clear();
            //平均分配充電工作給充電車
            carn = 0;
            for (int i = 0; i < list_cnt; i++)
            {
                tmp = new charge_info();
                tmp.car_no = carn;
                tmp.destid_in_eventlist = i; //紀錄是在edf_list中的第幾項
                temp_list.Add(tmp);
                carn = (carn + 1) % num_car_used;
            }
            min_redundent_time = 10000000;
            for (int i = 0; i < temp_list.Count(); i++)
            {
                carn = temp_list[i].car_no;

                dest_node = charging_list[temp_list[i].destid_in_eventlist].node_id;
                if (firststate[carn])
                {
                    accu_time[carn] += common.mydist(common.nmap.car_list[carn].x, common.nmap.car_list[carn].y, common.nmap.node[dest_node].x, common.nmap.node[dest_node].y) / common.car_speed;
                    total_dist += common.mydist(common.nmap.car_list[carn].x, common.nmap.car_list[carn].y, common.nmap.node[dest_node].x, common.nmap.node[dest_node].y);
                    firststate[carn] = false;
                }
                else
                {
                    if (charger_left[carn] == 0)
                    {
                        //回基地台
                        accu_time[carn] += common.mydist(common.nmap.car_list[carn].x, common.nmap.car_list[carn].y, common.nmap.node[0].x, common.nmap.node[0].y) / common.car_speed;
                        total_dist += common.mydist(common.nmap.car_list[carn].x, common.nmap.car_list[carn].y, common.nmap.node[0].x, common.nmap.node[0].y);
                        charger_left[carn] = common.num_charger_per_car;
                        //
                        accu_time[carn] += common.mydist(common.nmap.node[0].x, common.nmap.node[0].y, common.nmap.node[dest_node].x, common.nmap.node[dest_node].y) / common.car_speed;
                        total_dist += common.mydist(common.nmap.node[0].x, common.nmap.node[0].y, common.nmap.node[dest_node].x, common.nmap.node[dest_node].y);
                    }
                    else
                    {
                        accu_time[carn] += (common.nmap.distance_matrix[prenode[carn], dest_node] / common.car_speed);
                        total_dist += common.nmap.distance_matrix[prenode[carn], dest_node];
                    }
                }
                prenode[carn] = dest_node;
                charger_left[carn]--;
                //redundent_time = (charging_list[temp_list[i].destid_in_eventlist].request_time + charging_list[temp_list[i].destid_in_eventlist].timeleft) - accu_time[carn];
                redundent_time = (charging_list[temp_list[i].destid_in_eventlist].deadline) - accu_time[carn];
                if (redundent_time < 0)
                    return i; //檢查失敗，無法於時限內完成，傳回失敗點
                else
                {
                    if (min_redundent_time > redundent_time)
                        min_redundent_time = redundent_time;
                }

                if (common.charging_time_include)
                    accu_time[carn] += (100 - charging_list[temp_list[i].destid_in_eventlist].residual) / common.charging_speed;

            }

            return -1; //檢查成功           
        }
        public void start_charging()
        {
            DateTime exec_start, exec_end;
            exec_start = DateTime.Now;
            if (common.nmap.request_charging_list.Count() <= 0)
                return;
            common.charging_time_include = true;
            switch (common.method_sel)
            {
                case 1:
                    EDF(car_list);
                    break;
                case 2:
                    GENE(car_list);
                    break;
                case 3:
                    Lin(car_list, 0.5);
                    break;
                case 4:
                    RCSS(car_list);
                    break;
                case 5:
                    PSO(car_list);
                    break;
                case 6:
                    CUCK(car_list);
                    break;
                case 7:
                    M_Lin(car_list);
                    break;
                case 8:
                    dynamic_Lin(car_list);
                    break;
                case 9: //NEDF
                    Lin(car_list, 1);
                    break;
                case 10: //BPR_NJF
                    BPR_NJF(car_list);
                    break;
                case 11: //Prevention
                    Prevention(car_list);
                    break;
            }
            exec_end = DateTime.Now;
            common.total_time_exec += exec_end.Subtract(exec_start).TotalMilliseconds;
            //common.call_gene_count++;
            foreach (car c in car_list)
                if (c.moving_seq.Count > 0 && (c.status == 0 || c.status == 2))
                {
                    if (c.status == 0) c.mycharger[0].recharge(); //reload chargers, assume single charger per car
                    c.status = 1; //go
                    common.total_car_used++;
                }
            common.available_num_car = common.nmap.get_available_car();
        }

        List<request_event> get_NJF_list(List<request_event> request_charging_list, int type = 0)
        {
            int i, j;
            int num_charging_task;

            num_charging_task = request_charging_list.Count();
            List<request_event> temp_list = new List<request_event>();
            request_event tmp;
            int current_pos = 0;
            double min_dist, temp_dist;
            int min_job = 0;
            if (common.car_no_return) current_pos = common.current_node;
            bool[] used = new bool[num_charging_task];
            for (i = 0; i < num_charging_task; i++) used[i] = false;
            for (i = 0; i < num_charging_task; i++)
            {
                min_dist = common.MYINFINITE;
                for (j = 0; j < num_charging_task; j++)
                {
                    if (used[j]) continue;
                    temp_dist = common.nmap.distance_matrix[current_pos, request_charging_list[j].node_id];
                    if (temp_dist < min_dist)
                    {
                        min_dist = temp_dist;
                        min_job = j;
                    }
                }
                used[min_job] = true;
                current_pos = request_charging_list[min_job].node_id;
                tmp = new request_event(request_charging_list[min_job]);
                tmp.timeleft = tmp.timeleft - (common.current_time - tmp.request_time);
                temp_list.Add(tmp);

            }

            return temp_list;
        }

        List<request_event> get_EDF_list(List<request_event> request_charging_list, int type = 0)
        {

            int i;
            int num_charging_task;
            /*
            if (common.car_no_return)
                num_charging_task = request_charging_list.Count();
            else
                num_charging_task = Math.Min(common.num_charger_per_car * common.available_num_car, request_charging_list.Count());
             */
            num_charging_task = request_charging_list.Count();
            List<request_event> temp_list = new List<request_event>();
            request_event tmp;
            for (i = 0; i < num_charging_task; i++)
            {
                tmp = new request_event(request_charging_list[i]);
                tmp.timeleft = tmp.timeleft - (common.current_time - tmp.request_time);
                temp_list.Add(tmp);
            }
            //request_charging_list.RemoveRange(0, num_charging_task);
            if (type == 0)
                temp_list.Sort(delegate (request_event req1, request_event req2)
                {
                    return req1.timeleft.CompareTo(req2.timeleft);
                });
            return temp_list;
        }

        struct lin
        {
            public int original_seq_id;
            public double distance;
            public double time_left;
            public double td_sum;
        }
        struct lin_result
        {
            public int original_seq_id;
            public int torder, dorder;
            public double time_left;
        }
        List<lin> get_RCSS_list(List<request_event> request_charging_list, int type = 0)
        {
            int i, j;
            int num_charging_task;

            num_charging_task = request_charging_list.Count();
            List<request_event> temp_list = new List<request_event>();
            List<lin> lin_list = new List<lin>();
            List<lin_result> result_list = new List<lin_result>();
            lin temp_lin;
            lin_result temp_result;
            request_event tmp;
            int current_pos = common.current_node;
            double temp_dist;

            for (i = 0; i < num_charging_task; i++)
            {
                temp_dist = common.nmap.distance_matrix[current_pos, common.nmap.request_charging_list[i].node_id];
                temp_lin = new lin();
                temp_lin.original_seq_id = i;
                temp_lin.distance = temp_dist;
                tmp = new request_event(request_charging_list[i]);
                //tmp.timeleft = tmp.timeleft - (common.current_time - tmp.request_time);
                
                temp_lin.time_left = -tmp.consuming_speed; //consuming_speed 越大放越前面
                lin_list.Add(temp_lin);
                temp_result = new lin_result();
                temp_result.original_seq_id = i;
                temp_result.torder = 0;
                temp_result.dorder = 0;
                result_list.Add(temp_result);

            }
            lin_list.Sort(delegate (lin req1, lin req2)
            {
                return req1.time_left.CompareTo(req2.time_left);
            });
            for (i = 0; i < lin_list.Count(); i++)
            {
                temp_result = result_list[lin_list[i].original_seq_id];
                //temp_result.original_seq_id = lin_list[i].original_seq_id;
                temp_result.torder = i + 1;
                result_list[lin_list[i].original_seq_id] = temp_result;
            }
            lin_list.Sort(delegate (lin req1, lin req2)
            {
                return req1.distance.CompareTo(req2.distance);
            });
            for (i = 0; i < lin_list.Count(); i++)
            {
                temp_result = result_list[lin_list[i].original_seq_id];
                //temp_result.original_seq_id = lin_list[i].original_seq_id;
                temp_result.dorder = i + 1;
                result_list[lin_list[i].original_seq_id] = temp_result;
            }
            lin_list.Sort(delegate (lin req1, lin req2)
            {
                return req1.original_seq_id.CompareTo(req2.original_seq_id);
            });
            for (i = 0; i < lin_list.Count(); i++)
            {
                temp_lin = lin_list[i];
                //temp_result.original_seq_id = lin_list[i].original_seq_id;
                temp_lin.td_sum = result_list[i].torder + 0.8*result_list[i].dorder;
                lin_list[i] = temp_lin;
            }
            lin_list.Sort(delegate (lin req1, lin req2)
            {
                return (req1.td_sum * 10000000.0 + req1.time_left * req1.distance*0.000001).CompareTo(req2.td_sum * 10000000.0 + req2.time_left * req2.distance * 0.000001);
            });
            return lin_list;
        }
        List<lin> get_Lin_list(List<request_event> request_charging_list, double alpha=0.5)
        {
            int i, j;
            int num_charging_task;

            num_charging_task = request_charging_list.Count();
            List<request_event> temp_list = new List<request_event>();
            List<lin> lin_list = new List<lin>();
            List<lin_result> result_list = new List<lin_result>();
            lin temp_lin;
            lin_result temp_result;
            request_event tmp;
            int current_pos = common.current_node;
            double temp_dist;

            for (i = 0; i < num_charging_task; i++)
            {
                temp_dist = common.nmap.distance_matrix[current_pos, common.nmap.request_charging_list[i].node_id];
                temp_lin = new lin();
                temp_lin.original_seq_id = i;
                temp_lin.distance = temp_dist;
                tmp = new request_event(request_charging_list[i]);
                tmp.timeleft = tmp.timeleft - (common.current_time - tmp.request_time);
                temp_lin.time_left = tmp.timeleft;
                lin_list.Add(temp_lin);
                temp_result = new lin_result();
                temp_result.original_seq_id = i;
                temp_result.torder = 0;
                temp_result.dorder = 0;
                result_list.Add(temp_result);

            }
            lin_list.Sort(delegate (lin req1, lin req2)
            {
                return req1.time_left.CompareTo(req2.time_left);
            });
            for (i = 0; i < lin_list.Count(); i++)
            {
                temp_result = result_list[lin_list[i].original_seq_id];
                //temp_result.original_seq_id = lin_list[i].original_seq_id;
                temp_result.torder = i + 1;
                result_list[lin_list[i].original_seq_id] = temp_result;
            }
            lin_list.Sort(delegate (lin req1, lin req2)
            {
                return req1.distance.CompareTo(req2.distance);
            });
            for (i = 0; i < lin_list.Count(); i++)
            {
                temp_result = result_list[lin_list[i].original_seq_id];
                //temp_result.original_seq_id = lin_list[i].original_seq_id;
                temp_result.dorder = i + 1;
                result_list[lin_list[i].original_seq_id] = temp_result;
            }
            lin_list.Sort(delegate (lin req1, lin req2)
            {
                return req1.original_seq_id.CompareTo(req2.original_seq_id);
            });
            for (i = 0; i < lin_list.Count(); i++)
            {
                temp_lin = lin_list[i];
                //temp_result.original_seq_id = lin_list[i].original_seq_id;
                temp_lin.td_sum = alpha*result_list[i].torder + (1-alpha)*result_list[i].dorder;
                lin_list[i] = temp_lin;
            }
            lin_list.Sort(delegate (lin req1, lin req2)
            { 
                if (req1.td_sum > req2.td_sum) return 1;
                else if (req1.td_sum < req2.td_sum) return -1;
                else
                   return ( req1.time_left * req1.distance).CompareTo(req2.time_left * req2.distance);
            });
            return lin_list;
        }
        List<lin> get_dynamic_Lin_list(List<request_event> request_charging_list)
        {
            int i, j;
            int num_charging_task;

            num_charging_task = request_charging_list.Count();
            List<request_event> temp_list = new List<request_event>();
            List<lin> lin_list = new List<lin>();
            List<lin_result> result_list = new List<lin_result>();
            lin temp_lin;
            lin_result temp_result;
            request_event tmp;
            int current_pos = common.current_node;
            double temp_dist;

            for (i = 0; i < num_charging_task; i++)
            {
                temp_dist = common.nmap.distance_matrix[current_pos, common.nmap.request_charging_list[i].node_id];
                temp_lin = new lin();
                temp_lin.original_seq_id = i;
                temp_lin.distance = temp_dist;
                tmp = new request_event(request_charging_list[i]);
                tmp.timeleft = tmp.timeleft - (common.current_time - tmp.request_time);
                temp_lin.time_left = tmp.timeleft;
                lin_list.Add(temp_lin);
                temp_result = new lin_result();
                temp_result.original_seq_id = i;
                temp_result.torder = 0;
                temp_result.dorder = 0;
                temp_result.time_left = tmp.timeleft;
                result_list.Add(temp_result);

            }
            lin_list.Sort(delegate (lin req1, lin req2)
            {
                return req1.time_left.CompareTo(req2.time_left);
            });
            for (i = 0; i < lin_list.Count(); i++)
            {
                temp_result = result_list[lin_list[i].original_seq_id];
                //temp_result.original_seq_id = lin_list[i].original_seq_id;
                temp_result.torder = i + 1;
                result_list[lin_list[i].original_seq_id] = temp_result;
            }
            lin_list.Sort(delegate (lin req1, lin req2)
            {
                return req1.distance.CompareTo(req2.distance);
            });
            for (i = 0; i < lin_list.Count(); i++)
            {
                temp_result = result_list[lin_list[i].original_seq_id];
                //temp_result.original_seq_id = lin_list[i].original_seq_id;
                temp_result.dorder = i + 1;
                result_list[lin_list[i].original_seq_id] = temp_result;
            }
            lin_list.Sort(delegate (lin req1, lin req2)
            {
                return req1.original_seq_id.CompareTo(req2.original_seq_id);
            });
            for (i = 0; i < lin_list.Count(); i++)
            {
                temp_lin = lin_list[i];
                //temp_result.original_seq_id = lin_list[i].original_seq_id;
                double alpha;
                if (temp_lin.time_left < 1000)
                    alpha = 0.9;
                else if (temp_lin.time_left < 2000)
                    alpha = 0.5;
                else
                    alpha = 0.2;
                alpha = 0;
                temp_lin.td_sum = alpha * result_list[i].torder + (1 - alpha) * result_list[i].dorder;
                lin_list[i] = temp_lin;
            }
            lin_list.Sort(delegate (lin req1, lin req2)
            { 
                if (req1.td_sum > req2.td_sum) return 1;
                else if (req1.td_sum < req2.td_sum) return -1;
                else
                    return (req1.time_left * req1.distance).CompareTo(req2.time_left * req2.distance);
            });
            return lin_list;
        }
        void Lin(List<car> cList, double alpha=0.5)
        {
            int[] carmap = new int[common.max_num_car];
            List<request_event> angle_sorted_event_list = new List<request_event>();

            List<lin> temp_list = get_Lin_list(common.nmap.request_charging_list, alpha);

            request_event rq = common.nmap.request_charging_list[temp_list[0].original_seq_id];
            //目前只考慮一部充電車，應改做循序分配工作給充電車
            moving_data md = new moving_data();
            md.event_id = rq.event_id;
            md.target_id = rq.node_id;
            cList[0].moving_seq.Add(md);
            move_request_to_car(md.event_id, 0);
            //移除event_list items，指派充電車後就移除request, 在car的do_process中已經移除
            //remove_from_request_list(md.event_id);
        }
        void RCSS(List<car> cList)
        {
            int[] carmap = new int[common.max_num_car];
            List<request_event> angle_sorted_event_list = new List<request_event>();

            List<lin> temp_list = get_RCSS_list(common.nmap.request_charging_list);

            request_event rq = common.nmap.request_charging_list[temp_list[0].original_seq_id];
            //目前只考慮一部充電車，應改做循序分配工作給充電車
            moving_data md = new moving_data();
            md.event_id = rq.event_id;
            md.target_id = rq.node_id;
            cList[0].moving_seq.Add(md);
            move_request_to_car(md.event_id, 0);
            //移除event_list items，指派充電車後就移除request, 在car的do_process中已經移除
            //remove_from_request_list(md.event_id);
        }

        void dynamic_Lin(List<car> cList)
        {
            int[] carmap = new int[common.max_num_car];
            List<request_event> angle_sorted_event_list = new List<request_event>();

            List<lin> temp_list = get_dynamic_Lin_list(common.nmap.request_charging_list);

            request_event rq = common.nmap.request_charging_list[temp_list[0].original_seq_id];
            //目前只考慮一部充電車，應改做循序分配工作給充電車
            moving_data md = new moving_data();
            md.event_id = rq.event_id;
            md.target_id = rq.node_id;
            cList[0].moving_seq.Add(md);
            move_request_to_car(md.event_id, 0);
        }
        void M_Lin(List<car> cList)
        {
            int[] carmap = new int[common.max_num_car];
            // List<request_event> angle_sorted_event_list = new List<request_event>();
            List<lin> temp_list2;
            request_event rq;
            List<request_event> temp_list1 = get_NJF_list(common.nmap.request_charging_list);
            int i = find_bottle(temp_list1, 1);
            if ( i >= 0)
            {  // Lin
                //temp_list2 = get_Lin_list(common.nmap.request_charging_list);
                //rq = common.nmap.request_charging_list[temp_list2[0].original_seq_id];
                rq = temp_list1[i];
            }
            else //NJF
                rq = temp_list1[0];
            //目前只考慮一部充電車，應改做循序分配工作給充電車
            moving_data md = new moving_data();
            md.event_id = rq.event_id;
            md.target_id = rq.node_id;
            cList[0].moving_seq.Add(md);
            move_request_to_car(md.event_id, 0);
            //移除event_list items，指派充電車後就移除request, 在car的do_process中已經移除
            //remove_from_request_list(md.event_id);
        }
        void BPR_NJF(List<car> cList)
        {
            int carno = find_dispatch_car_at_base(cList);
            if (carno < 0)
                return;

            int maxTask = estimate_bpr_max_task_count(cList[carno]);
            List<request_event> taskPool = build_bpr_task_pool(maxTask);
            if (taskPool.Count <= 0)
                return;

            List<request_event> njfList = get_NJF_list(taskPool);
            foreach (request_event rq in njfList)
            {
                assign_request_to_car(cList[carno], rq, carno);

                if (cList[carno].moving_seq.Count() >= maxTask)
                    break;
            }
        }

        void Prevention(List<car> cList)
        {
            int carno = find_dispatch_car_at_base(cList);
            if (carno < 0)
                return;

            int maxTask = estimate_bpr_max_task_count(cList[carno]);
            List<request_event> taskPool = build_bpr_task_pool(maxTask);
            if (taskPool.Count <= 0)
                return;

            List<request_event> njfList = get_NJF_list(taskPool);
            foreach (request_event rq in njfList)
            {
                assign_request_to_car(cList[carno], rq, carno);

                if (cList[carno].moving_seq.Count() >= maxTask)
                    break;
            }
        }

        void NJF(List<car> cList)
        {
            int i;
            List<request_event> temp_list = get_NJF_list(common.nmap.request_charging_list);
            List<request_event> original_NJF = temp_list.ToList();
            request_event temp_req;
            int carno = 0, EDF_stop = 0, k;
            int largest_ind = 0, NJF_i;

            i = find_bottle(temp_list, 1);
            if (i >= 0)
            {
                NJF_i = i;

                while (i > largest_ind)
                {
                    largest_ind = i;
                    temp_req = temp_list[i];
                    temp_list.RemoveAt(i);
                    for (k = 0; k < EDF_stop; k++)
                    {
                        if (temp_list[k].deadline > temp_req.deadline) { k = k - 1; break; }
                    }

                    temp_list.Insert(k, temp_req);
                    i = find_bottle(temp_list, 1);
                }
                if (i >= 0 && NJF_i >= i) temp_list = original_NJF.ToList();

            }

            /*
            if (common.dynamic_schedule)
            {
                if ((i=find_bottle(temp_list, 1)) >= 0)
                {
                    request_event rq = temp_list[i];
                    moving_data md = new moving_data();
                    md.event_id = rq.event_id;
                    md.target_id = rq.node_id;
                    cList[carno].moving_seq.Add(md);
                    remove_from_request_list(md.event_id);
                    return;
                }
            }
            */
            foreach (request_event rq in temp_list)
            {
                moving_data md = new moving_data();
                md.event_id = rq.event_id;
                md.target_id = rq.node_id;
                cList[carno].moving_seq.Add(md);
                //移除event_list items，指派充電車後就移除request
                // remove_from_request_list(md.event_id);

                //if (common.dynamic_schedule) break; //一次只处理一个

                //一次最多只處理 common.num_charger_per_car 個
                if (cList[carno].moving_seq.Count() * common.request_threshold * common.Origin_RESIDUAL >= cList[carno].mycharger[0].residual) break;
            }
        }
        void EDF(List<car> cList)
        {
            int[] carmap = new int[common.max_num_car];
            List<request_event> angle_sorted_event_list = new List<request_event>();
            List<request_event> temp_list = get_EDF_list(common.nmap.request_charging_list);
            int i, angle_sorted_seq;
            int[] num_job_car = new int[common.max_num_car];
            int num_car = 0;
            for (i = 0; i < common.max_num_car; i++)
            {
                num_job_car[i] = 0;
                if (cList[i].status == 0 || cList[i].status == 2)
                {
                    carmap[num_car++] = i;
                }
            }
            angle_sorted_event_list.Clear();
            for (i = 0; i < temp_list.Count; i++)
            {
                angle_sorted_event_list.Add(temp_list[i]);
            }
            angle_sorted_event_list.Sort(delegate (request_event req1, request_event req2)
            {
                return common.nmap.node[req1.node_id].angle.CompareTo(common.nmap.node[req2.node_id].angle);
            });
            int carno = 0;
            int avg_charge = (int)Math.Ceiling((double)(temp_list.Count()) / num_car);
            bool found=false;
            foreach (request_event rq in temp_list)
            {
                moving_data md = new moving_data();
                md.event_id = rq.event_id;
                md.target_id = rq.node_id;
                if (common.assign_car_method == 0)
                {
                    for(i=0; i<common.max_num_car; i++)
                    {
                        if (cList[i].status != 0 && cList[i].status != 2) continue;
                        if ((num_job_car[i] + 1) * common.request_threshold * common.Origin_RESIDUAL < cList[i].mycharger[0].residual) break;
                    }
                    if (i < common.max_num_car)
                    {
                        cList[i].moving_seq.Add(md);
                        move_request_to_car(md.event_id, i);
                        num_job_car[i]++;
                    }
                    else break;
                }
                else
                {
                    for (i = 0; i < angle_sorted_event_list.Count; i++)
                        if (angle_sorted_event_list[i].node_id == rq.node_id) break;
                    angle_sorted_seq = i;
                    int qv = (int)Math.Floor(angle_sorted_seq / (double)avg_charge);
                    found = true;
                    if ((num_job_car[carno] + 1) * common.request_threshold * common.Origin_RESIDUAL > cList[carno].mycharger[0].residual)
                    {
                        int si = (qv + 1) % num_car;
                        found = false;
                        while (si != qv)
                        {
                            if ((num_job_car[carno] + 1) * common.request_threshold * common.Origin_RESIDUAL < cList[carno].mycharger[0].residual)
                            {
                                found = true;
                                break;
                            }
                            si = (si + 1) % num_car;                           
                        }
                        qv = si;
                    }
                    if (found)
                    {
                        num_job_car[qv]++;
                        cList[carmap[qv]].moving_seq.Add(md);
                        move_request_to_car(md.event_id, carno);
                    }
                    else break;
                }

            }
        }

        public static void remove_from_request_list(int event_id)
        {
            //移除event_list items
            foreach (request_event rq in common.nmap.request_charging_list)
            {
                if (rq.event_id == event_id)
                {
                    common.nmap.request_charging_list.Remove(rq);
                    break;
                }
            }
        }
        public static void remove_from_request_list(List<request_event> request_list, int event_id)
        {
            //移除event_list items
            foreach (request_event rq in request_list)
            {
                if (rq.event_id == event_id)
                {
                    request_list.Remove(rq);
                    break;
                }
            }
        }
        public static void move_request_to_car(int event_id, int carno)
        {
            //移除event_list items 至车中串列
            foreach (request_event rq in common.nmap.request_charging_list)
            {
                if (rq.event_id == event_id)
                {
                    common.nmap.car_list[carno].charging_list.Add(rq);
                    common.nmap.request_charging_list.Remove(rq);
                    break;
                }
            }
        }
        public void requeue_car_request(request_event rq)
        {
            if (rq == null)
                return;

            if (rq.is_proactive)
            {
                request_event currentState = build_sensor_state(rq.node_id);
                if (currentState != null && currentState.request_deadline > common.current_time)
                {
                    node[rq.node_id].status = 0;
                    refresh_bpr_state(rq.node_id, true);
                    return;
                }

                if (currentState != null)
                {
                    currentState.event_id = (rq.event_id >= 0) ? rq.event_id : request_event.max_id++;
                    currentState.request_time = common.current_time;
                    currentState.request_deadline = common.current_time;
                    currentState.request_deadline_low = currentState.request_deadline;
                    currentState.request_deadline_high = currentState.request_deadline;
                    currentState.timeleft = Math.Max(0.0, currentState.depletion_deadline - common.current_time);
                    currentState.is_proactive = false;
                    finalize_request_event(currentState, false);
                    request_charging_list.Insert(0, currentState);
                    node[rq.node_id].status = 1;
                    register_on_demand_request(currentState);
                    return;
                }

                node[rq.node_id].status = 0;
                return;
            }

            request_charging_list.Insert(0, rq);
            register_on_demand_request(rq);
        }
        /*
        public static void move_request_backto_list(int event_id, int carno)
        {
            //从车中串列移回 request_charging_list
            foreach (request_event rq in common.nmap.car_list[carno].charging_list)
            {
                if (rq.event_id == event_id)
                {
                    common.nmap.request_charging_list.Insert(0,rq);
                    common.nmap.car_list[carno].charging_list.Remove(rq);                    
                    break;
                }
            }
        }
        */
        /*
                void CUCK(List<car> cList)
                {
                    int carn, currentnode, samecnt, genIdx;
                    int[] prenode = new int[common.max_num_car];
                    bool[] firststate = new bool[common.max_num_car];
                    int[] carmap = new int[common.available_num_car];
                    if (common.current_time == 611)
                    {
                        samecnt = 0;
                    }
                    cuckooSearch.event_list = get_EDF_list(common.nmap.request_charging_list, 0);
                    if (cuckooSearch.event_list.Count == 0)
                    {
                        samecnt = common.current_time;
                    }
                    gene ss;
                    gene bestone = null, allbest = null;
                    int popu_size = 20;
                    int max_same_cnt = 20;
                    common.call_gene_count++;

                    //mapping usable cars
                    int mapi = 0;
                    for (int i = 0; i < common.max_num_car; i++)
                    {
                        if (cList[i].status == 0 || cList[i].status == 2)
                        {
                            carmap[mapi++] = i;
                        }
                    }
                    if (cuckooSearch.event_list.Count > 1)
                    {
                        int start_num_car = (int)Math.Ceiling(cuckooSearch.event_list.Count() / (double)common.num_charger_per_car);
                        for (int ci = start_num_car; ci <= mapi; ci++)
                        {
                            cuckooSearch.cs(common.population_size, ci);
                            if (cuckooSearch.best.fitness < 1000000) break;
                        }



                        for (int i = 0; i < cuckooSearch.best.num_node; i++)
                        {
                            int j = cuckooSearch.best.find_Seq(i);
                            carn = carmap[cuckooSearch.best.charge_car_no[j]];
                            currentnode = cuckooSearch.event_list[j].node_id;
                            if (currentnode != 0)
                            {
                                moving_data md = new moving_data();
                                md.target_id = currentnode;
                                md.event_id = cuckooSearch.event_list[j].event_id;
                                cList[carn].moving_seq.Add(md);

                                //移除event_list items，指派充電車後就移除request
                                remove_from_request_list(md.event_id);
                            }
                        }
                    }
                    else
                    {
                        moving_data md = new moving_data();
                        md.target_id = cuckooSearch.event_list[0].node_id;
                        md.event_id = cuckooSearch.event_list[0].event_id;
                        cList[carmap[0]].moving_seq.Add(md);

                        //common.total_gene_iterations += max_same_cnt;
                        remove_from_request_list(md.event_id);
                    }
                }
        */
        public static int find_id_in_angle_sort_list(int id)
        {
            int k;
            for (k = 0; k < GenePopulation.angle_sorted_event_list.Count(); k++)
                if (GenePopulation.angle_sorted_event_list[k].node_id == id)
                    break;
            return k;
        }
        void GENE(List<car> cList)
        {
            int carn, currentnode, samecnt, genIdx;
            int carno = 0, k, tempi;
            int[] prenode = new int[common.max_num_car];
            bool[] firststate = new bool[common.max_num_car];
            int[] carmap = new int[common.available_num_car];
            GenePopulation allpop = null, prepop = null; ;
            GenePopulation.event_list = get_EDF_list(common.nmap.request_charging_list, 0);
            GenePopulation.event_list_NJF = get_NJF_list(common.nmap.request_charging_list, 0);
            gene ss;
            gene bestone = null, allbest = null;
            int popu_size = 20;
            int max_same_cnt = 20;

            common.call_gene_count++;
            int mapi = 0;
            for (int i = 0; i < common.max_num_car; i++)
            {
                if (cList[i].status == 0 || cList[i].status == 2)
                {
                    carmap[mapi++] = i;
                }

            }
            if (GenePopulation.event_list.Count > 1)
            {

                ss = new gene(GenePopulation.event_list.Count);

                popu_size = GenePopulation.event_list.Count;

                if (popu_size <= 6)
                    popu_size = common.fact(popu_size);
                else
                {
                    popu_size = common.population_size;
                }

                if (popu_size < 20) popu_size = 20;

                //    popu_size = common.population_size; //改掉上述所有設定，以傳統genetic algorithm執行
                bestone = null;

                allpop = new GenePopulation();

                int rep_times;
                bestone = null;

                double required_energy = GenePopulation.event_list.Count() * (common.target_ratio - common.request_threshold - 0.1) * common.Origin_RESIDUAL;
                double car_energy = common.num_charger_per_car * common.Origin_RESIDUAL;
                int start_num_car = Math.Min((int)Math.Ceiling(required_energy/ car_energy), common.available_num_car);
                if (common.useMaxCar) start_num_car = common.available_num_car;
                else start_num_car = 1;

                for (int ci = start_num_car; ci <= common.available_num_car; ci++)
                //for (int ci = common.available_num_car; ci <= common.available_num_car; ci++)
                {
                    GenePopulation.num_car_used = ci;
                    allpop.initialize(ss, popu_size);
                    //將allpop的第一個以EDF平均分配工作的結果取代
                    //carno = 0;

                    if (common.EDF_gene)
                    {
                        allpop[0]= gene.randomInstance(GenePopulation.event_list.Count, common.assign_car_method, ci, 1);
                        allpop[0].calcFitness();
                    }
                    if (common.NJF_gene)
                    {
                        allpop[1] = gene.randomInstance(GenePopulation.event_list.Count, common.assign_car_method, ci, 2);
                        allpop[1].calcFitness();
                    }
                    /*
                    int j;
                    for (int i = 0; i < allpop[1].num_node; i++)
                    {
                        for (j = 0; j < GenePopulation.event_list.Count; j++)
                            if (GenePopulation.event_list[j].node_id == event_list_NJF[i].node_id) break;
                        allpop[1].charge_seq[i].destid_in_eventlist = j;
                        int k = nodemap.find_id_in_angle_sort_list(GenePopulation.event_list[j].node_id);

                        int tempi = GenePopulation.assign_charging_car(k, allpop[1].num_node, 1);

                        allpop[1].charge_seq[i].car_no = tempi;
                        //allpop[1].charge_seq[i].car_no = carno;
                        //carno = (carno + 1) % ci;
                    }

                    allpop[1].calcFitness();
                    */
                    //end
                    samecnt = 0;
                    rep_times = common.max_iteration;
                    bestone = null;
                    for (genIdx = 0; genIdx < rep_times; genIdx++)
                    {

                        allpop = allpop.reproduction();

                        if (bestone == null)
                            bestone = (gene)(allpop.First());
                        else
                        {
                            ss = (gene)(allpop.First());
                            if (ss.fitness < bestone.fitness)
                                bestone = ss;
                            else if (ss.fitness == bestone.fitness)
                            {
                                if (ss.gene_equal(bestone))
                                {
                                    samecnt++;
                                    if (samecnt > max_same_cnt) break;
                                    //if (samecnt > Math.Min(20, popu_size)) break;
                                }
                                else
                                {
                                    samecnt = 0;
                                    bestone = ss;
                                }
                            }
                        }
                    }
                    if (allbest == null)
                        allbest = bestone;
                    else
                    {
                        //ss = (gene)(allpop.First());
                        if (bestone.fitness < allbest.fitness)
                            allbest = bestone;
                    }
                    common.total_gene_iterations += genIdx;
                    //if (allbest.fitness < 1000000) break; //find a feasible solution
                }

                //指派充电任务
                int maxcar = 0;
                for (int i = 0; i < allbest.num_node; i++)
                {
                    carn = carmap[allbest.charge_seq[i].car_no];
                    if (allbest.charge_seq[i].car_no > maxcar) maxcar = allbest.charge_seq[i].car_no;
                    currentnode = GenePopulation.event_list[allbest.charge_seq[i].destid_in_eventlist].node_id;
                    if (currentnode != 0)
                    {
                        //if (cList[carn].moving_seq.Count() >= common.num_charger_per_car) continue;
                        moving_data md = new moving_data();
                        md.target_id = currentnode;
                        md.event_id = GenePopulation.event_list[allbest.charge_seq[i].destid_in_eventlist].event_id;
                        cList[carn].moving_seq.Add(md);

                        //移除event_list items，指派充電車後就移除request
                        move_request_to_car(md.event_id, carn);
                    }
                }
            }
            else
            {
                moving_data md = new moving_data();
                md.target_id = GenePopulation.event_list[0].node_id;
                md.event_id = GenePopulation.event_list[0].event_id;
                cList[carmap[0]].moving_seq.Add(md);
                common.total_gene_iterations += max_same_cnt;
                move_request_to_car(md.event_id,carmap[0]);
            }
        }

        bool validate_route(request_event[] route, int starttime, int stoppos)
        {
            int i;
            int arrive_time = starttime;

            for (i = 1; i < stoppos; i++)
            {
                if (route[i].node_id != 0)
                {
                    arrive_time += (int)(distance_matrix[route[i - 1].node_id, route[i].node_id] / common.car_speed);
                    if (arrive_time > route[i].request_time + route[i].timeleft) return false;
                }
            }
            return true;
        }
        int getArrive_time(List<request_event> route, int stoppos)
        {
            int i;
            int arrive_time = common.current_time;

            for (i = 1; i <= stoppos; i++)
            {
                if (route[i].node_id != 0)
                {
                    arrive_time += (int)(distance_matrix[route[i - 1].node_id, route[i].node_id] / common.car_speed);
                }
            }
            return arrive_time;
        }

        List<int> get_Hgrouping(List<request_event> request_charging_list, int sx = -1, int sy = -1, int ex = -1, int ey = -1)
        {
            int i, j, nodeid;
            int num_node = request_charging_list.Count;
            int max_group = 6;
            bool change = true;
            int min_center;
            double min_dist, temp_dist;
            List<for_sorting_distance> node_dists = new List<for_sorting_distance>();
            List<group_info> group_center = new List<group_info>();
            group_info temp_g;
            for (i = 0; i < max_group; i++)
            {
                temp_g = new group_info();
                temp_g.cx = common.rand.Next(common.nmap.width);
                temp_g.cy = common.rand.Next(common.nmap.height);
                temp_g.num_member = 0;
                group_center.Add(temp_g);
            }
            node_dists.Clear();
            for (j = 0; j < num_node; j++)
            {
                for_sorting_distance tempv = new for_sorting_distance();
                tempv.nid = request_charging_list[j].node_id;
                tempv.carno = -1;
                tempv.oldcarno = -1;
                tempv.event_list_order = j;
                node_dists.Add(tempv);
            }

            while (change)
            {
                change = false;

                for (i = 0; i < num_node; i++)
                {
                    min_center = 0;
                    nodeid = node_dists[i].nid;
                    min_dist = common.mydist(common.nmap.node[nodeid].x, common.nmap.node[nodeid].y, group_center[0].cx, group_center[0].cy);
                    for (j = 1; j < max_group; j++)
                    {
                        temp_dist = common.mydist(common.nmap.node[nodeid].x, common.nmap.node[nodeid].y, group_center[j].cx, group_center[j].cy);
                        if (temp_dist < min_dist)
                        {
                            min_center = j;
                            min_dist = temp_dist;
                        }
                    }
                    if (min_center != node_dists[i].carno)
                    {
                        change = true;
                        node_dists[i].carno = min_center;
                    }

                }
                if (change)
                {//重新計算群組中心
                    for (j = 0; j < max_group; j++)
                    {
                        group_center[j].cx = 0; group_center[j].cy = 0;
                        group_center[j].num_member = 0;
                        group_center[j].member_list.Clear();
                    }
                    for (i = 0; i < num_node; i++)
                    {
                        nodeid = node_dists[i].nid;
                        group_center[node_dists[i].carno].cx += common.nmap.node[nodeid].x;
                        group_center[node_dists[i].carno].cy += common.nmap.node[nodeid].y;
                        group_center[node_dists[i].carno].num_member++;
                        group_center[node_dists[i].carno].member_list.Add(nodeid);

                        //node_dists[i].carno = -1; //準備下一輪計算
                    }
                    for (j = 0; j < max_group; j++)
                    {
                        group_center[j].cx /= group_center[j].num_member;
                        group_center[j].cy /= group_center[j].num_member;
                        // group_center[j].num_member = 0;
                    }
                }
            }

            // group_center = find_best_group_seq(group_center);

            List<int> final_seq = new List<int>();
            List<int> temp_seq;
            for (int gi = 0; i < group_center.Count; gi++)
            {

                if (group_center[gi].member_list.Count == 0) continue;
                if (group_center[gi].member_list.Count == 1)
                {
                    final_seq.Add(group_center[gi].member_list[0]);
                }
                else
                {
                    if (gi == 0 && sx == -1)
                    {
                        sx = common.nmap.node[0].x;
                        sy = common.nmap.node[0].y;
                    }
                    else if (gi == group_center.Count - 1 && ex == -1)
                    {
                        ex = common.nmap.node[0].x;
                        ey = common.nmap.node[0].y;
                    }

                }

            }
            return final_seq;
        }
    }
}
