using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{

    class particle
    {
        //////////////////////////
        //                     //
        //    定义粒子的属性   // 
        //                     //
        /////////////////////////
        public double fitness;
        public double[] velocity; //每个位置一个速度
        public int num_node; //待充电节点数
        public int num_car;
        public int[] charge_seq;
        public int[] charge_car_no;
        //粒子的局部最优位置
        public particle best_known;

        public particle(int n)
        {
            num_node = n;
            charge_seq = new int[n];
            charge_car_no = new int[n];
            velocity = new double[n];
            for (int i = 0; i < n; i++)
            {
                charge_seq[i] = -1;
                charge_car_no[i] = -1;
            }
            best_known = null;
        }
        public void copy(particle s)
        {
            int i;
            fitness = s.fitness;
            for (i = 0; i < num_node; i++)
            {
                charge_seq[i] = s.charge_seq[i];
                charge_car_no[i] = s.charge_car_no[i];
            }
            //if (s.best_known != null)
            //   best_known.copy(s.best_known);
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
        /// 适应度计算函数
        public double calfitness()//适应度计算
        {
            double check_result = 0; //用以計算超過時間的部分，在fitness中比較使用
            double[][] max_distance_group = new double[common.max_num_car][];
            double[] accu_time = new double[common.max_num_car];
            bool[] stop_accu = new bool[common.max_num_car];
            bool[] firststate = new bool[common.max_num_car];
            int[] prenode = new int[common.max_num_car];
            double total_dist = 0, max_accu_time;
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
                currentnode = pso.event_list[ordered_charge_seq_id[i]].node_id;

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
                double energy_needed = (common.Origin_RESIDUAL * common.target_ratio - pso.event_list[order].residual + (accu_time[carn] - pso.event_list[order].request_time) * pso.event_list[order].consuming_speed);
                charger_left[carn] -= energy_needed;
                if (accu_time[carn] >= pso.event_list[order].deadline)
                {
                    //if (missed == -1) missed = num_node - i;
                    missed++;
                    check_result += (accu_time[carn] - (pso.event_list[order].deadline));
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

    class pso
    {
        //////////////////////////
        //                     //
        //    指定相关的参数   // 
        //                     //
        /////////////////////////

        private static int particle_number=200;//指定粒子数量
        private static int low = 1;
        private static int high = 40;
        public static int generations = 10000;//指定迭代的代数
        private static double v_max = 30;//指定最大速度
        private static double c1 = 2;//指定信任度1
        private static double c2 = 2;//指定信任度2
        private static double w = 0.6;//指定惯性因子

        public static int num_node; //待充电节点数
        public static int num_car; //可用充电车数
        // 使用前須先設定好要求解的 event_list
        public static List<request_event> event_list = new List<request_event>();
        //粒子群
        public static particle[] ps = new particle[particle_number];

        //粒子的全局最优适应度，是一个值
        public particle global_best=null;
        //Random rand = new Random(Guid.NewGuid().GetHashCode());
        public void psoSearch(int n_node, int n_car)
        {
            int i, same_cnt=0;
            double global_best_fit = Double.MaxValue;
            num_node = n_node;
            num_car = n_car;

            initial(num_node, num_car);
            i = 0;
            while (i < generations)
            {
                renew_particle();
                renew_var();
                if (Math.Abs(global_best.fitness - global_best_fit) < 0.0001)
                {
                    same_cnt++;
                    if (same_cnt > 20) break;
                }                    
                else
                {
                    same_cnt = 0;
                }
                global_best_fit = global_best.fitness;
                i++;
            }
        }

        public void initial(int n_node, int n_car)//粒子初始化
        {
            int i, j, tj;
            bool[] used = new bool[n_node];
            int carn;

            for (i = 0; i < particle_number; i++)
            {

                ps[i] = new particle(n_node);//初始化单一粒子
                carn = 0;
                if (i == 0)
                { //EDF
                    for (j = 0; j < n_node; j++)
                    {
                        ps[i].charge_seq[j] = j;
                        ps[i].charge_car_no[j] = carn;
                        carn = (carn + 1) % num_car; //平均依序分配充電車
                    }
                }
                else
                {
                    for (j = 0; j < n_node; j++)
                        used[j] = false;
                    for (j = 0; j < n_node; j++)
                    {
                        tj = common.rand.Next(n_node);
                        while (used[tj]) tj = (tj + 1) % n_node;
                        used[tj] = true;
                        ps[i].charge_seq[j] = tj;
                        ps[i].charge_car_no[j] = carn;
                        carn = (carn + 1) % num_car; //平均依序分配充電車
                    }
                }
                ps[i].calfitness();
                ps[i].best_known = new particle(n_node);
                ps[i].best_known.copy(ps[i]);
                for (j=0; j<n_node; j++)
                   ps[i].velocity[j] = -v_max + 2 * v_max * 1.0 * common.rand.NextDouble();
                //更新全局最优粒子
                
                if (global_best == null || global_best.fitness>ps[i].fitness)
                {
                    if (global_best == null) global_best = new particle(n_node);
                    global_best.copy(ps[i]);
                }                
            }

           // Console.WriteLine("初始化粒子完成!");
           // Console.WriteLine("开始进行迭代寻优...");
        }
        public class reoder
        {
            public int origin_seq;
            public int ind;
        };
        /// <summary>
        /// 更新粒子位置
        /// </summary>
        public void renew_particle()//每次迭代后需要重新更新粒子所在的位置
        {
            int i, j;
            List<reoder> tlist = new List<reoder>();
            reoder temp_reoder;

            for (i = 0; i < particle_number; i++)
            {
                for (j = 0; j < num_node; j++)
                {
                    ps[i].charge_seq[j] = ((int)(ps[i].charge_seq[j] + ps[i].velocity[j])) % num_node;                    
                }
                //reorder
                //reordering charge seq
                tlist.Clear();
                for (j = 0; j < num_node; j++)
                {
                    temp_reoder = new reoder();
                    temp_reoder.origin_seq = ps[i].charge_seq[j];
                    temp_reoder.ind = j;
                    tlist.Add(temp_reoder);
                }
                tlist.Sort(delegate (reoder req1, reoder req2)
                { //由小排到大，越小越好
                    return req1.origin_seq.CompareTo(req2.origin_seq);
                });
                for (j = 0; j < num_node; j++)
                {
                    ps[i].charge_seq[tlist[j].ind] = j;
                }
                ps[i].calfitness();
            }
        }
        /// <summary>
        /// 更新局部和全局的适应度和位置
        /// </summary>
        public void renew_var()
        {
            int i, j;
            for (i = 0; i < particle_number; i++)
            {
                if (ps[i].fitness < ps[i].best_known.fitness)
                {
                    ps[i].best_known.copy(ps[i]);                    
                }
                if (global_best.fitness > ps[i].fitness)
                    global_best.copy(ps[i]);
            }

            for (i = 0; i < particle_number; i++)//更新个体速度，为下一次迭代做准备
            {
                for (j = 0; j < num_node; j++)
                {
                   
                    ps[i].velocity[j] = ps[i].velocity[j] * w +
                        c1 * 1.0 * common.rand.NextDouble() * (ps[i].best_known.charge_seq[j] - ps[i].charge_seq[j]) +
                        c2 * 1.0 * common.rand.NextDouble() * (global_best.charge_seq[j] - ps[i].charge_seq[j]);
                }
            }
        }
        
    }

}
