using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class gene
    {
        public bool rotated;
        public double fitness;
        //seq, carnum, seq 值固定為0~nodenum-1, carnum則為0~car_num-1，每個car充電的順序則為以相同carnum的節點，照nodenum排序
        // charge_seq 為 [nodenum, 2]的陣列, [i,0]為node_num節點編號, [i,1]為 carnum充電車編號， 陣列駐標則為充電seq.
        // [i, 0] 更改為 Gene_Population 中 List<request_event> event_list 中的編號
        public charge_info[] charge_seq;
        public int num_node;
        public gene(int x)
        {
            num_node = x;
            rotated = false;
            charge_seq = new charge_info[num_node];
            for (int i = 0; i < num_node; i++)
                charge_seq[i] = new charge_info();
        }
        public int length()
        {
            return num_node;
        }
        public bool gene_equal(gene x)
        {
            for (int i = 0; i < x.num_node; i++)
            {
                if (!x.charge_seq[i].equals(this.charge_seq[i])) return false;
            }
            return true;
        }

        public bool check_gene()
        {
            for (int b = 0; b < charge_seq.Count(); b++)
            {
                if (charge_seq[b].destid_in_eventlist < 0) return false;
                for (int c = b + 1; c < charge_seq.Count(); c++)
                    if (charge_seq[c].destid_in_eventlist == charge_seq[b].destid_in_eventlist)
                    {
                        return false;
                    }
            }
            return true;
        }
        public double calcFitness()
        { // Fitness 值越小越好
            double check_result = 0; //用以計算超過時間的部分，在fitness中比較使用
            double[][] max_distance_group = new double[common.max_num_car][];
            double[] accu_time = new double[common.max_num_car];
            bool[] firststate = new bool[common.max_num_car];
            bool[] stop_accu = new bool[common.max_num_car];
            int[] prenode = new int[common.max_num_car];
            double[] charger_left = new double[common.max_num_car];
            int missed = 0;
            double total_dist = 0, max_accu_time, max_accu_group_dif, difx, dify, tempv;
            int carn, currentnode, car_used;
            List<charge_info> temp_list = new List<charge_info>();
            charge_info tmp;

            for (carn = 0; carn < common.max_num_car; carn++)
            {
                max_distance_group[carn] = new double[4]; //max distance
                max_distance_group[carn][0] = 0;
                max_distance_group[carn][1] = 0; //sum of x =>avg of x
                max_distance_group[carn][2] = 0; //sum of y =>avg of y
                max_distance_group[carn][3] = 0; //total count

                accu_time[carn] = common.current_time;
                firststate[carn] = true;
                prenode[carn] = -1;
                charger_left[carn] = common.nmap.car_list[carn].mycharger[0].residual;
                stop_accu[carn] = common.car_no_return;
            }
            //分群?
            for (int i = 0; i < num_node; i++)
            {
                carn = charge_seq[i].car_no;
                currentnode = GenePopulation.event_list[charge_seq[i].destid_in_eventlist].node_id;
                max_distance_group[carn][1] += common.nmap.node[currentnode].x;
                max_distance_group[carn][2] += common.nmap.node[currentnode].y;
                max_distance_group[carn][3]++;
            }
            for (carn = 0; carn < common.max_num_car; carn++)
            {
                if (max_distance_group[carn][3] > 0)
                {
                    max_distance_group[carn][1] = max_distance_group[carn][1] / max_distance_group[carn][3];
                    max_distance_group[carn][2] = max_distance_group[carn][2] / max_distance_group[carn][3];
                }
            }
            max_accu_group_dif = 0;
            for (carn = 0; carn < common.max_num_car; carn++)
            {
                max_accu_group_dif += max_distance_group[carn][0];
            }
            temp_list.Clear();
            if (GenePopulation.phase == 0)
            {
                for (int i = 0; i < num_node; i++)
                {
                    for (int j = 0; j < GenePopulation.block_list[charge_seq[i].destid_in_eventlist].num_event; j++)
                    {
                        tmp = new charge_info();
                        tmp.destid_in_eventlist = GenePopulation.block_list[charge_seq[i].destid_in_eventlist].event_list[j];
                        tmp.car_no = charge_seq[i].car_no;
                        temp_list.Add(tmp);
                    }

                }
            }
            else
            {
                for (int i = 0; i < num_node; i++)
                {
                    temp_list.Add(charge_seq[i]);
                }
            }

            for (int i = 0; i < num_node; i++)
            {
                carn = temp_list[i].car_no;
                currentnode = GenePopulation.event_list[temp_list[i].destid_in_eventlist].node_id;
                tempv = common.mydist(max_distance_group[carn][1], max_distance_group[carn][2], common.nmap.node[currentnode].x, common.nmap.node[currentnode].y);

                if (tempv > max_distance_group[carn][0])
                    max_distance_group[carn][0] = tempv;

                if (firststate[carn])
                {
                    accu_time[carn] += common.mydist(common.nmap.car_list[carn].x, common.nmap.car_list[carn].y, common.nmap.node[currentnode].x, common.nmap.node[currentnode].y) / common.car_speed;
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
                        charger_left[carn] = common.num_charger_per_car*common.Origin_RESIDUAL;
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
                int order = temp_list[i].destid_in_eventlist;
                double energy_needed = (common.Origin_RESIDUAL * common.target_ratio - GenePopulation.event_list[order].residual + (accu_time[carn] - GenePopulation.event_list[order].request_time) * GenePopulation.event_list[order].consuming_speed);
                charger_left[carn] -=energy_needed;
                if (accu_time[carn] >= GenePopulation.event_list[temp_list[i].destid_in_eventlist].deadline)
                {
                    //if (missed == -1) missed = num_node - i;
                    missed++;
                    check_result += (accu_time[carn] - (GenePopulation.event_list[temp_list[i].destid_in_eventlist].deadline));
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
            max_accu_time = 0;  //前面都没有在算
            car_used = 0;
            /*
            for (car_used=0, carn = 0; carn < common.max_num_car; carn++)
            {//判斷用幾部車，如果要用越少車越好，則須在下面加入car_used的權重
                if (!firststate[carn])
                {
                    car_used++;
                    if (accu_time[carn] > max_accu_time) max_accu_time = accu_time[carn];
                    if (!common.car_no_return)
                        total_dist += common.nmap.distance_matrix[prenode[carn], 0]; //加上充電車回基地台的成本
                }
            }
            */
            //if (missed == -1) missed = 0;
//            fitness = missed * 1000000 + check_result * 1000 + car_used * 0 + total_dist * common.calc_c + max_accu_time * common.calc_b + max_accu_group_dif * common.calc_a;
            fitness = check_result * 1000000 + car_used * 0 + total_dist * common.calc_c + max_accu_time * common.calc_b + max_accu_group_dif * common.calc_a;

            return fitness;
        }

        public gene crossover(gene spouse)
        {
            int ni = 0, firstempty = -1;
            int cutIdx = common.rand.Next(num_node);
            if (cutIdx < 1) cutIdx = 1;
            gene child = new gene(num_node);
            bool[] used = new bool[num_node];
            for (int i = 0; i < num_node; i++)
            {
                used[i] = false;
                child.charge_seq[i].destid_in_eventlist = -1;
            }
            for (int i = 0; i < cutIdx; i++, ni++)
            {
                child.charge_seq[ni] = charge_seq[i];
                used[charge_seq[i].destid_in_eventlist] = true;
            }
            for (int i = 0; i < num_node; i++)
            {
                if (!used[spouse.charge_seq[i].destid_in_eventlist])
                {
                    child.charge_seq[ni] = spouse.charge_seq[i];
                    //為保持各充電車充電的節點數不超過原先設定，因此在crossover時不變動各充電節點使用的充電車
                    for (int j = cutIdx; j < num_node; j++)
                        if (charge_seq[j].destid_in_eventlist == spouse.charge_seq[i].destid_in_eventlist)
                        {
                            child.charge_seq[ni].car_no = charge_seq[j].car_no;
                            break;
                        }
                    ni++;
                }
            }
            /*
                    for (int i = cutIdx; i < num_node; i++, ni++)
                    {
                        if (!used[spouse.charge_seq[i].destid_in_eventlist])
                        {
                            child.charge_seq[ni] = spouse.charge_seq[i];
                            used[spouse.charge_seq[i].destid_in_eventlist] = true;
                        }
                        else
                        {
                            if (firstempty == -1) firstempty = ni;
                        }
                    }
                    //todo
                    for (int i = 0; i < num_node; i++)
                    {
                        if (!used[spouse.charge_seq[i].destid_in_eventlist])
                        {
                            while (firstempty < num_node && child.charge_seq[firstempty].destid_in_eventlist >= 0) firstempty++;
                            if (firstempty == num_node) break;
                            child.charge_seq[firstempty] = spouse.charge_seq[i];
                            used[spouse.charge_seq[i].destid_in_eventlist] = true;
                        }
                    }
            */
            return child;
        }
        public void shift_mutate()
        {
            charge_info temp = charge_seq[0];
            for (int j = 0; j < num_node - 1; j++)
            {
                charge_seq[j] = charge_seq[j + 1];
            }
            charge_seq[num_node - 1] = temp;
        }
        public void mutate()
        {
            int cutIdx1 = common.rand.Next(num_node);
            int cutIdx2 = common.rand.Next(num_node);
            if (cutIdx1 == cutIdx2) return;
            //charge_info temp= new charge_info();
            int tid, tcar;
            tid = charge_seq[cutIdx1].destid_in_eventlist;
            tcar = charge_seq[cutIdx1].car_no;

            charge_seq[cutIdx1].destid_in_eventlist = charge_seq[cutIdx2].destid_in_eventlist;
            charge_seq[cutIdx1].car_no = charge_seq[cutIdx2].car_no;

            charge_seq[cutIdx2].destid_in_eventlist = tid;
            charge_seq[cutIdx2].car_no = tcar;

        }
        /*
                public static gene randomInstance(int num_node_to_charge, int method, int num_car_used = 1, bool randomness = true)
                { // num_node 待充電sensor數
                    int carno, mapind;
                    int[] mapno = new int[common.max_num_car];
                    int[] num_job_car = new int[common.max_num_car];
                    gene newgene = new gene(num_node_to_charge);
                    int i, j;
                    for (i = 0; i < num_node_to_charge; i++)
                        newgene.charge_seq[i].destid_in_eventlist = -1;
                    //       int num_car_used= common.rand.Next(Math.Min(common.max_num_car,common.available_num_car))+1; //先決定使用幾台充電車
        */
        /* mapping 可以移至 global */
        /*
                    mapind = 0;
                    for (i = 0; i < common.max_num_car; i++)
                    { // 找出目前可用的充電車
                        num_job_car[i] = 0; //指定給各車的充電任務數
                        if (common.nmap.car_list[i].status == 0 || common.nmap.car_list[i].status == 2)
                        {
                            mapno[mapind++] = i;
                        }
                    }

                    int k = common.rand.Next(num_node_to_charge);
                    int kk;
                    bool[] used = new bool[num_node_to_charge];
                    for (j = 0; j < num_node_to_charge; j++) used[j] = false;
                    j = 0;
                    for (i = k; true;)
                    {
                        if (randomness)
                        {
                            kk = common.rand.Next(num_node_to_charge);
                            while (used[kk]) kk = (kk + 1) % num_node_to_charge;
                            used[kk] = true;
                        }
                        else
                        {
                            for (kk = 0; kk < num_node_to_charge; kk++)
                                if (GenePopulation.angle_sorted_event_list[i].node_id == common.nmap.request_charging_list[kk].node_id)
                                    break;

                        }
                        //將待充電節點串列中第i個節點放到新充電順序中的第j個位置
                        newgene.charge_seq[j].destid_in_eventlist = kk;

                        //carno = common.rand.Next(mapind);
                        //carno = mapno[carno];
                        int tempi = GenePopulation.assign_charging_car(kk, k, num_node_to_charge, method);
                        if (num_job_car[tempi] == common.num_charger_per_car)
                        {
                            int si = (tempi + 1) % mapind;
                            while (si != tempi)
                            {
                                if (num_job_car[si] < common.num_charger_per_car) break;
                                si = (si + 1) % mapind; ;
                            }
                            tempi = si;
                        }
                        num_job_car[tempi]++;
                        //carno = mapno[tempi]; //留待GENE最后再map

                        newgene.charge_seq[j++].car_no = tempi;
                        i = (i + 1) % num_node_to_charge;
                        if (i == k) break;
                    }
                    */
        /*
                    for (i = 0; i < num_node_to_charge; i++)
                    {
                        if (randomness)
                        {
                            j = common.rand.Next(num_node_to_charge);
                            while (newgene.charge_seq[j].destid_in_eventlist >= 0) j = (j + 1) % num_node_to_charge;
                        }
                        else
                        {
                            for (int k=0; k<GenePopulation.angle_sorted_event_list.Count(); k++)
                                if (GenePopulation.angle_sorted_event_list[k].node_id==)
                            j = i;
                        }
                        //將待充電節點串列中第i個節點放到新充電順序中的第j個位置
                        newgene.charge_seq[j].destid_in_eventlist = i;

                        //carno = common.rand.Next(mapind);
                        //carno = mapno[carno];
                        int tempi = GenePopulation.assign_charging_car(i, num_node_to_charge, method);
                        if (num_job_car[tempi] == common.num_charger_per_car)
                        {
                            int si = (tempi + 1) % mapind;
                            while (si != tempi)
                            {
                                if (num_job_car[si] < common.num_charger_per_car) break;
                                si = (si + 1) % mapind; ;
                            }
                            tempi = si;
                        }
                        num_job_car[tempi]++;
                        //carno = mapno[tempi]; //留待GENE最后再map

                        newgene.charge_seq[j].car_no = tempi;
                    }
        */
        /*
                    newgene.calcFitness(); //todo:考慮基因不重複產生
                    return newgene;
                }
        */
        
        public static gene randomInstance(int num_node_to_charge, int method, int num_car_used, int type)
        { // num_node 待充電sensor數
            //type=0: random, 1:EDF, 2:NJF
            int carno, mapind;
            int[] mapno = new int[common.max_num_car];
            int[] num_job_car = new int[common.max_num_car];
            gene newgene = new gene(num_node_to_charge);
            int i, j, nid;
            for (i = 0; i < num_node_to_charge; i++)
                newgene.charge_seq[i].destid_in_eventlist = -1;
            //       int num_car_used= common.rand.Next(Math.Min(common.max_num_car,common.available_num_car))+1; //先決定使用幾台充電車
            // mapping 可以移至 global 

            mapind = 0;
            for (i = 0; i < common.max_num_car; i++)
            { // 找出目前可用的充電車
                num_job_car[i] = 0; //指定給各車的充電任務數
                if (common.nmap.car_list[i].status == 0 || common.nmap.car_list[i].status == 2)
                {
                    mapno[mapind++] = i;
                    if (mapind >= num_car_used) break;
                }
            }

            carno = 0;
            for (i = 0; i < num_node_to_charge; i++)
            {
                nid = GenePopulation.event_list[i].node_id;
                /*
                if (randomness)
                {
                    j = common.rand.Next(num_node_to_charge);
                    while (newgene.charge_seq[j].destid_in_eventlist >= 0) j = (j + 1) % num_node_to_charge;
                }
                else
                {
                    //  for (int k=0; k<GenePopulation.angle_sorted_event_list.Count(); k++)
                    //      if (GenePopulation.angle_sorted_event_list[k].node_id==)
                    j = i;
                }
                */
                j = 0;
                if (type == 0)
                {
                    j = common.rand.Next(num_node_to_charge);
                    while (newgene.charge_seq[j].destid_in_eventlist >= 0) j = (j + 1) % num_node_to_charge;
                }
                    
                else if (type == 1)
                    j = i; //event_list 已是EDF顺序
                else if (type == 2)
                {
                    for (j = 0; j < GenePopulation.event_list_NJF.Count; j++)
                        if (GenePopulation.event_list_NJF[j].node_id == nid) break;
                }
                
                //將待充電節點串列中第i個節點放到新充電順序中的第j個位置
                newgene.charge_seq[j].destid_in_eventlist = i; //在GENE中，将 GenePopulation.event_list改成 request list 的 EDF 结果
                int tempi;
                if (common.angle_sorting) //angle_sorting 控制此项
                { //找出充电顺序中的i个节点在角度排序中的位置 ci, 以便后续用角度分配车辆

                    //int k = nodemap.find_id_in_angle_sort_list(nid);

                    tempi = GenePopulation.assign_charging_car(i, num_node_to_charge, mapind, 1);
                }
                else
                {
                    //tempi = carno;
                    //carno = (carno + 1) % mapind;
                    tempi = common.rand.Next(mapind);
                } 

                if (num_job_car[tempi] == common.num_charger_per_car)
                {
                    int si = (tempi + 1) % mapind;
                    while (si != tempi)
                    {
                        if (num_job_car[si] < common.num_charger_per_car) break;
                        si = (si + 1) % mapind; ;
                    }
                    tempi = si;
                }
                num_job_car[tempi]++;
                //carno = mapno[tempi]; //留待GENE最后再map
                
                newgene.charge_seq[j].car_no = tempi;
            }

            newgene.calcFitness(); //todo:考慮基因不重複產生
            return newgene;
        }
        
        public static void copy_value(gene from, gene to)
        {
            //Array.Copy(from.active_set, to.active_set, from.active_set.Length);
            to.fitness = from.fitness;
            to.num_node = from.num_node;
            for (int i = 0; i < to.num_node; i++)
            {
                to.charge_seq[i].destid_in_eventlist = from.charge_seq[i].destid_in_eventlist;
                to.charge_seq[i].car_no = from.charge_seq[i].car_no;
            }
        }
    }

}
