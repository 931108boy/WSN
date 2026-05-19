using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class cuckoo
    {
        public double fitness;
        public int num_node;
        public int num_car;
        public int[] charge_seq;
        public int[] charge_car_no;

        public cuckoo(int n)
        {
            num_node = n;
            charge_seq = new int[n];
            charge_car_no = new int[n];
            for (int i = 0; i < n; i++)
            {
                charge_seq[i] = -1;
                charge_car_no[i] = -1;
            }
        }
        public void copy(cuckoo s)
        {
            int i;
            fitness = s.fitness;
            for (i = 0; i < num_node; i++)
            {
                charge_seq[i] = s.charge_seq[i];
                charge_car_no[i] = s.charge_car_no[i];
            }
        }
        public int find_Seq(int i)
        {// 找尋第i個充電的節點是在充電任務序列中的第幾個
            int j;
            for (j = 0; j < num_node; j++)
            {
                if (charge_seq[j] == i) break;
            }
            return j;
        }
        public double calfitness()
        {
            double check_result = 0; //用以計算超過時間的部分，在fitness中比較使用
            double[][] max_distance_group = new double[common.max_num_car][];
            double[] accu_time = new double[common.max_num_car];
            bool[] stop_accu = new bool[common.max_num_car];
            bool[] firststate = new bool[common.max_num_car];
            int[] prenode = new int[common.max_num_car];
            double total_dist = 0, max_accu_time, max_accu_group_dif, tempv;
            int carn, currentnode, car_used;
            int[] ordered_charge_seq_id = new int[num_node];
            int[] ordered_charge_car_no = new int[num_node];
            double[] charger_left = new double[common.max_num_car];
            int missed = 0;

            for (carn = 0; carn < common.max_num_car; carn++)
            {
               
                accu_time[carn] = common.current_time;
                firststate[carn] = true;
                prenode[carn] = -1;
                charger_left[carn] = common.nmap.car_list[carn].mycharger[0].residual;
                stop_accu[carn] = common.car_no_return;
            }            

            //依充電順序排列節點與使用的充電車
            for (int i = 0; i < num_node; i++)
            {
                int j = find_Seq(i);

                ordered_charge_seq_id[i] = j;
                ordered_charge_car_no[i] = charge_car_no[j];
            }

            for (int i = 0; i < num_node; i++)
            {
                carn = ordered_charge_car_no[i];
                currentnode = cuckooSearch.event_list[ordered_charge_seq_id[i]].node_id;
                
                if (firststate[carn])
                {
                    accu_time[carn] = common.mydist(common.nmap.car_list[carn].x, common.nmap.car_list[carn].y, common.nmap.node[currentnode].x, common.nmap.node[currentnode].y) / common.car_speed;
                    total_dist += common.mydist(common.nmap.car_list[carn].x, common.nmap.car_list[carn].y, common.nmap.node[currentnode].x, common.nmap.node[currentnode].y);
                    firststate[carn] = false;
                }
                else
                {

                    if (charger_left[carn] == 0)
                    {
                        //回基地台
                        accu_time[carn] += common.mydist(common.nmap.car_list[carn].x, common.nmap.car_list[carn].y, common.nmap.node[0].x, common.nmap.node[0].y) / common.car_speed;
                        total_dist += common.mydist(common.nmap.car_list[carn].x, common.nmap.car_list[carn].y, common.nmap.node[0].x, common.nmap.node[0].y);
                        charger_left[carn] = common.num_charger_per_car * common.Origin_RESIDUAL;
                        //
                        stop_accu[carn] = true;
                        accu_time[carn] += common.mydist(common.nmap.node[0].x, common.nmap.node[0].y, common.nmap.node[currentnode].x, common.nmap.node[currentnode].y) / common.car_speed;
                        //total_dist += common.mydist(common.nmap.node[0].x, common.nmap.node[0].y, common.nmap.node[currentnode].x, common.nmap.node[currentnode].y);
                    }
                    else
                    {
                        accu_time[carn] += (common.nmap.distance_matrix[prenode[carn], currentnode] / common.car_speed);
                        if (!stop_accu[carn])
                            total_dist += common.nmap.distance_matrix[prenode[carn], currentnode];
                    }

                }
                prenode[carn] = currentnode;
                int order = ordered_charge_seq_id[i];
                double energy_needed = (common.Origin_RESIDUAL * common.target_ratio - cuckooSearch.event_list[order].residual + (accu_time[carn] - cuckooSearch.event_list[order].request_time) * cuckooSearch.event_list[order].consuming_speed);
                charger_left[carn] -= energy_needed;
                if (accu_time[carn] >= cuckooSearch.event_list[order].deadline)
                {
                    //if (missed == -1) missed = num_node - i;
                    missed++;
                    check_result += (accu_time[carn] - (cuckooSearch.event_list[order].deadline));
                }
                if (common.charging_time_include)
                {
                    accu_time[carn] += energy_needed / common.charging_speed;

                }

            }
            for (carn = 0; carn < common.max_num_car; carn++)
            {
                if (!firststate[carn])
                {//出发的车需要回到基地台
                    if (!stop_accu[carn])
                    {
                        //total_dist += common.mydist(common.nmap.car_list[carn].x, common.nmap.car_list[carn].y, common.nmap.node[0].x, common.nmap.node[0].y);
                        total_dist += common.nmap.distance_matrix[prenode[carn], 0];
                    }
                }
            }
            max_accu_time = 0;
            car_used = 0;
            
            //fitness = check_result * 1000000 + car_used * 0 + total_dist * common.calc_c + max_accu_time * common.calc_b + max_accu_group_dif * common.calc_a;
            fitness = check_result * 1000000 + car_used * 0 + total_dist * common.calc_c + max_accu_time * common.calc_b;

            return fitness;
        }
    }
    public class cuckooSearch
    {
        public static double Pamax = 0.8;
        public static double Pamin = 0.05;
        public static double alpha_max = 0.5, alpha_min = 0.005;
        public static double Pa, alpha;
        // 使用前須先設定好要求解的 event_list
        public static List<request_event> event_list = new List<request_event>();
        public static cuckoo best;
        public static int num_node;
        public static Random rand = new Random(2);
        public static List<cuckoo> population = new List<cuckoo>();
        public static List<for_sorting_distance> node_dists = new List<for_sorting_distance>();
        // static int[] mapno = new int[common.max_num_car];
        // static int mapind;
        public static void cs(int popsize, int num_car)
        {
            double best_fit;
            int same_cnt = 0, i;
            num_node = event_list.Count;

            //if (common.assign_car_method == 5) grouping(num_car);
            /*
            mapind = 0;
            for (i = 0; i < common.max_num_car; i++)
            { // 找出目前可用的充電車
                if (common.nmap.car_list[i].status == 0 || common.nmap.car_list[i].status == 2)
                {
                    mapno[mapind++] = i;
                }
            }
            */
            initialize(popsize, num_node, num_car);

            int MaxGeneration = common.max_iteration;
            int t = 0;
            cuckoo temp;
            best = new cuckoo(num_node);
            best.copy(population[0]);
            best_fit = best.fitness;

            while (t < MaxGeneration)
            {
                temp = build_new_cuckoo(num_node, num_car);
                Pa = Pamax - (double)t / MaxGeneration * (Pamax - Pamin);
                alpha = alpha_max * Math.Exp((double)t / MaxGeneration * Math.Log(alpha_min / alpha_max));
                i = rand.Next(popsize);
                if (population[i].fitness > temp.fitness)
                {
                    population.RemoveAt(i);
                    population.Add(temp);
                }
                move_cuckoo(num_node, num_car);
                population.Sort(delegate (cuckoo req1, cuckoo req2)
                { //由小排到大，越小越好                    
                    return req1.fitness.CompareTo(req2.fitness);
                });
                int remove_size = (int)(popsize * Pa);
                population.RemoveRange(popsize - remove_size, remove_size);
                for (i = 0; i < remove_size; i++)
                {
                    temp = build_new_cuckoo(num_node, num_car);
                    population.Add(temp);
                }
                population.Sort(delegate (cuckoo req1, cuckoo req2)
                { //由小排到大，越小越好
                    return req1.fitness.CompareTo(req2.fitness); ;
                });
                if (population[0].fitness<best.fitness)
                  best.copy(population[0]);
                if (Math.Abs(best_fit - best.fitness) < 0.000001)
                    same_cnt++;
                else
                    same_cnt = 0;
                
                best_fit = best.fitness;
                if (same_cnt >= 20) break;
                t++;
            }
        }
        public static cuckoo build_new_cuckoo(int num_node, int num_car)
        {
            cuckoo temp;
            int[] num_job_car = new int[common.max_num_car];

            int carn;
            int k;

            for (int i = 0; i < common.max_num_car; i++)
            { // 找出目前可用的充電車
                num_job_car[i] = 0; //指定給各車的充電任務數
            }

            temp = new cuckoo(num_node);
            carn = 0;
            for (int j = 0; j < num_node; j++)
            {
                k = rand.Next(num_node);
                while (temp.charge_seq[k] != -1) k = (k + 1) % num_node;
                temp.charge_seq[k] = j;
                if (common.assign_car_method == 5)
                {
                    int tempi = node_dists[k].carno;
                    if (num_job_car[tempi] == common.num_charger_per_car)
                    {
                        int si = (tempi + 1) % num_car;
                        while (si != tempi)
                        {
                            if (num_job_car[si] < common.num_charger_per_car) break;
                            si = (si + 1) % num_car;
                        }
                        tempi = si;
                    }
                    num_job_car[tempi]++;

                    temp.charge_car_no[k] = tempi;
                }
                else
                {
                    temp.charge_car_no[j] = carn;
                    carn = (carn + 1) % num_car; //平均依序分配充電車
                }
                //temp.charge_car_no[k] = rand.Next(num_car);
            }
            temp.calfitness();
            return temp;
        }

        public static void initialize(int popSize, int num_node, int num_car)
        {
            cuckoo temp;
            int carn, i, j, n, k;
            population.Clear();


            //將EDF放入
            temp = new cuckoo(num_node);
            carn = 0;
            for (j = 0; j < num_node; j++)
            {
                temp.charge_seq[j] = j;
                temp.charge_car_no[j] = carn;
                carn = (carn + 1) % num_car; //平均依序分配充電車
            }


            temp.calfitness();
            population.Add(temp);
            n = 1;
            for (i = num_car; i <= num_car; i++)
            {
                if (i < num_car) k = (int)(popSize / num_car);
                else k = popSize - n;
                for (j = 1; j <= k; j++)
                {
                    temp = build_new_cuckoo(num_node, i);
                    population.Add(temp);
                    n++;
                    if (n >= popSize) break;
                }
                if (n >= popSize) break;
            }
        }
        public class reoder
        {
            public int origin_seq;
            public int ind;
        };
        public static void move_cuckoo(int num_node, int num_car)
        {//using Levy flight
            List<reoder> tlist = new List<reoder>();
            reoder temp_reoder;
            double beta, sigma, u, v, step, stepsize;
            int n = 2; //dimension of solution

            beta = 3 / 2;
            sigma = Math.Pow((GammaDistribution.Gamma(1 + beta) * Math.Sin(Math.PI * beta / 2) / (GammaDistribution.Gamma((1 + beta) / 2) * beta * Math.Pow(2, ((beta - 1) / 2)))), (1 / beta));
            for (int nest = 0; nest < population.Count; nest++)
            {
                for (int j = 0; j < num_node; j++)
                {
                    u = rand.NextDouble() * sigma;
                    v = rand.NextDouble();
                    step = u / Math.Pow(v, (1 / beta));
                    stepsize = alpha * step * (population[nest].charge_seq[j] - best.charge_seq[j]);
                    population[nest].charge_seq[j] += (int)(stepsize * rand.NextDouble());
                    stepsize = alpha * step * (population[nest].charge_car_no[j] - best.charge_car_no[j]);
                    //stepsize = 0;
                    population[nest].charge_car_no[j] += (int)(stepsize * rand.NextDouble());
                    while (population[nest].charge_car_no[j] < 0)
                        population[nest].charge_car_no[j] += num_car;
                    population[nest].charge_car_no[j] = population[nest].charge_car_no[j] % num_car;

                }
                //reordering charge seq
                tlist.Clear();
                for (int j = 0; j < num_node; j++)
                {
                    temp_reoder = new reoder();
                    temp_reoder.origin_seq = population[nest].charge_seq[j];
                    temp_reoder.ind = j;
                    tlist.Add(temp_reoder);
                }
                tlist.Sort(delegate (reoder req1, reoder req2)
                { //由小排到大，越小越好
                    return req1.origin_seq.CompareTo(req2.origin_seq);
                });
                for (int j = 0; j < num_node; j++)
                {
                    population[nest].charge_seq[tlist[j].ind] = j;
                }
                population[nest].calfitness();
            }
        }

        public static void grouping(int num_car_used)
        {
            int i, j, nodeid;
            int avg_member;
            bool change = true;
            group_info[] group_center = new group_info[num_car_used];
            for (i = 0; i < num_car_used; i++)
            {
                group_center[i] = new group_info();
                group_center[i].cx = rand.Next(common.nmap.width);
                group_center[i].cy = rand.Next(common.nmap.height);
            }
            node_dists.Clear();
            for (j = 0; j < num_node; j++)
            {
                for_sorting_distance tempv = new for_sorting_distance();
                tempv.nid = event_list[j].node_id;
                tempv.carno = -1;
                tempv.oldcarno = -1;
                tempv.event_list_order = j;
                node_dists.Add(tempv);
            }

            avg_member = (int)Math.Ceiling(event_list.Count() / (double)num_car_used);
            while (change)
            {
                change = false;
                for (i = 0; i < num_node; i++)
                {
                    node_dists[i].carno = node_dists[i].carno; //?
                }
                for (i = 0; i < num_car_used; i++)
                {
                    for (j = 0; j < num_node; j++)
                    {
                        if (node_dists[j].carno == -1)
                        {
                            nodeid = node_dists[j].nid;
                            node_dists[j].dist = common.mydist(common.nmap.node[nodeid].x, common.nmap.node[nodeid].y, group_center[i].cx, group_center[i].cy);
                        }
                    }
                    node_dists.Sort(delegate (for_sorting_distance req1, for_sorting_distance req2)
                    {
                        double v1, v2;
                        v1 = req1.dist + req1.carno * 100000; //如此未assign carno的rec會被排在前面
                        v2 = req2.dist + req2.carno * 100000;
                        return v1.CompareTo(v2);
                    });
                    for (j = 0; j < avg_member; j++)
                    {
                        if (node_dists[j].carno >= 0) break;
                        node_dists[j].carno = i;
                    }
                }
                //if (change)
                {//重新計算群組中心
                    for (j = 0; j < num_car_used; j++)
                    {
                        group_center[j].cx = 0; group_center[j].cy = 0;
                        group_center[j].num_member = 0;
                    }
                    for (i = 0; i < num_node; i++)
                    {
                        nodeid = node_dists[i].nid;
                        group_center[node_dists[i].carno].cx += common.nmap.node[nodeid].x;
                        group_center[node_dists[i].carno].cy += common.nmap.node[nodeid].y;
                        group_center[node_dists[i].carno].num_member++;
                        if (node_dists[i].carno != node_dists[i].oldcarno)
                        {
                            change = true;
                            node_dists[i].oldcarno = node_dists[i].carno;
                        }
                        //node_dists[i].carno = -1; //準備下一輪計算
                    }
                    for (j = 0; j < num_car_used; j++)
                    {
                        group_center[j].cx /= group_center[j].num_member;
                        group_center[j].cy /= group_center[j].num_member;
                        group_center[j].num_member = 0;
                    }
                }
            }

            node_dists.Sort(delegate (for_sorting_distance req1, for_sorting_distance req2)
            {
                return req1.event_list_order.CompareTo(req2.event_list_order);
            });

        }

    }
}
