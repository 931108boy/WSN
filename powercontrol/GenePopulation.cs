using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class GenePopulation : List<gene>
    { // 使用前須先設定好要求解的 event_list
        static Random random = new Random(2);
        public static List<request_event> event_list = new List<request_event>();
        public static List<request_event> event_list_NJF = new List<request_event>();
        //angle_sorted_event_list 在initialize中重新建立
        public static List<request_event> angle_sorted_event_list = new List<request_event>();
        public static List<event_block> block_list = new List<event_block>();

        public static int phase = 1; //0: block  1:normal
        public static int num_gene;
        public static double event_distance_thd = 50;
        public static int num_car_used;
        double mutationRate = 0.2;
        public static List<for_sorting_distance> node_dists = new List<for_sorting_distance>();
        public static void grouping()
        {
            int i, j, nodeid;
            int avg_member;
            bool change = true;
            group_info[] group_center = new group_info[num_car_used];
            for (i = 0; i < num_car_used; i++)
            {
                group_center[i] = new group_info();
                group_center[i].cx = random.Next(common.nmap.width);
                group_center[i].cy = random.Next(common.nmap.height);
            }
            node_dists.Clear();
            for (j = 0; j < num_gene; j++)
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
                for (i = 0; i < num_gene; i++)
                {
                    node_dists[i].carno = node_dists[i].carno;
                }
                for (i = 0; i < num_car_used; i++)
                {
                    for (j = 0; j < num_gene; j++)
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
                    for (i = 0; i < num_gene; i++)
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
                        // node_dists[i].carno = -1; //準備下一輪計算
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
        /*
                public static int assign_charging_car(int event_list_id, int offset, int num_node_to_charge, int method = 2)
                {
                    double midv;
                    int qv, rv;
                    int avg_charge = (int)Math.Ceiling((double)(num_node_to_charge) / num_car_used);
                    int angle_sorted_seq, i;
                    for (i = 0; i < angle_sorted_event_list.Count; i++)
                        if (angle_sorted_event_list[i].node_id == event_list[event_list_id].node_id) break;
                    angle_sorted_seq = i;
                    angle_sorted_seq -= offset;
                    if (angle_sorted_seq < 0)
                        angle_sorted_seq += num_node_to_charge;

                    method = 1;
                    switch (method)
                    {
                        case 0: //隨機分配
                            return random.Next(num_car_used);
                            break;
                        case 1: //以充電節點個數平均分配，沒有中間機率
                                //midv = avg_charge / 2.0;

                            qv = (int)Math.Floor(angle_sorted_seq / (double)avg_charge);
                            return qv;
                        case 2: //以充電節點個數平均分配，有中間機率
                            int tt = angle_sorted_seq - offset;
                            if (tt < 0) tt += num_node_to_charge;
                            midv = avg_charge / 2.0;
                            qv = (int)Math.Floor(angle_sorted_seq / (double)avg_charge);
                            rv = (int)(angle_sorted_seq % (double)avg_charge);
                            if (qv == 0 && rv <= midv) return qv;
                            else if (angle_sorted_seq >= qv * avg_charge + midv) return qv;
                            else
                            {
                                // if ((double)rv / midv >= 0.)
                                double p = random.NextDouble();
                                double tempp;
                                if (rv <= midv)
                                {
                                    tempp = (double)(rv + midv) / avg_charge;
                                    if (tempp >= 0.55) return qv;
                                    else if (p <= tempp) return qv;
                                    else return qv - 1;
                                }
                                else
                                {
                                    tempp = 1 - (double)(rv - midv) / avg_charge;
                                    if (tempp >= 0.55) return qv;
                                    else if (p <= tempp) return qv;
                                    else return qv + 1;
                                }
                            }
                        //break;
                        case 3: //以360度平均分配，沒有中間機率
                            break;
                        case 4: //以360度平均分配，有中間機率
                            break;
                        case 5: //以k-center平均分配
                            return node_dists[event_list_id].carno;
                    }
                    return 1;
                }
        */
        public static int assign_charging_car(int event_list_id, int num_node_to_charge, int car_used, int method = 1)
        {
            double midv;
            int qv, rv;
            int avg_charge = (int)Math.Ceiling((double)(num_node_to_charge) / car_used);
            int angle_sorted_seq, i;
            for (i = 0; i < angle_sorted_event_list.Count; i++)
                if (angle_sorted_event_list[i].node_id == event_list[event_list_id].node_id) break;
            angle_sorted_seq = i;
            if (common.angle_sorting) method = 1;
            else method = 0;
            switch (method)
            {
                case 0: //隨機分配
                    return random.Next(car_used);
                    break;
                case 1: //以充電節點個數平均分配，沒有中間機率
                        //midv = avg_charge / 2.0;
                    qv = (int)Math.Floor(angle_sorted_seq / (double)avg_charge);
                    return qv;
                case 2: //以充電節點個數平均分配，有中間機率
                    midv = avg_charge / 2.0;
                    qv = (int)Math.Floor(angle_sorted_seq / (double)avg_charge);
                    rv = (int)(angle_sorted_seq % (double)avg_charge);
                    if (qv == 0 && rv <= midv) return qv;
                    else if (angle_sorted_seq >= qv * avg_charge + midv) return qv;
                    else
                    {
                        // if ((double)rv / midv >= 0.)
                        double p = random.NextDouble();
                        double tempp;
                        if (rv <= midv)
                        {
                            tempp = (double)(rv + midv) / avg_charge;
                            if (tempp >= 0.55) return qv;
                            else if (p <= tempp) return qv;
                            else return qv - 1;
                        }
                        else
                        {
                            tempp = 1 - (double)(rv - midv) / avg_charge;
                            if (tempp >= 0.55) return qv;
                            else if (p <= tempp) return qv;
                            else return qv + 1;
                        }
                    }
                //break;
                case 3: //以360度平均分配，沒有中間機率
                    break;
                case 4: //以360度平均分配，有中間機率
                    break;
                case 5: //以k-means平均分配
                    return node_dists[event_list_id].carno;
            }
            return 1;
        }
        public GenePopulation()
        {
        }
        public void initialize(gene prototype, int popSize)
        {
            int i;
            this.Clear();
            num_gene = event_list.Count;
            //node_dists = new List<for_sorting_distance>();

            angle_sorted_event_list.Clear();
            if (common.assign_car_method == 5)
                grouping();
            else
            {
                for (i = 0; i < event_list.Count; i++)
                {
                    angle_sorted_event_list.Add(event_list[i]);
                }
                angle_sorted_event_list.Sort(delegate (request_event req1, request_event req2)
                {
                    return common.nmap.node[req1.node_id].angle.CompareTo(common.nmap.node[req2.node_id].angle);
                });
                //隨機調整 角度排序序列的起始點
                //应该改到randomInstance里
                i = random.Next(angle_sorted_event_list.Count());
                for (int k = 0; k < i; k++)
                {
                    request_event rq = angle_sorted_event_list[0];
                    angle_sorted_event_list.RemoveAt(0);
                    angle_sorted_event_list.Add(rq);
                }
                
            }

            gene newGene = gene.randomInstance(num_gene, common.assign_car_method, num_car_used, 0);
            this.Add(newGene);
            for (i = 1; i < popSize; i++)
            {
                newGene = gene.randomInstance(num_gene, common.assign_car_method, num_car_used, 0);

                newGene.calcFitness();
                this.Add(newGene);
            }
        }

        public static void generate_block()
        {
            int i, j, k, neweventid, blockeventid;
            block_list.Clear();
            for (i = 0; i < event_list.Count; i++)
            {
                neweventid = event_list[i].node_id;

                for (j = 0; j < block_list.Count; j++)
                {
                    for (k = 0; k < block_list[j].num_event; k++)
                    {
                        blockeventid = GenePopulation.event_list[block_list[j].event_list[k]].node_id;
                        if (common.nmap.distance_matrix[neweventid, blockeventid] <= event_distance_thd)
                            break;
                    }
                    if (k < block_list[j].num_event) break;
                }
                if (j >= block_list.Count)
                    block_list.Add(new event_block(i));
                else
                    block_list[j].add_member(i);
            }
        }
        public gene selection()
        {
            int maxrange = Count;  //Count: List 的屬性
            if (random.Next() < 0.8) maxrange = Math.Min(maxrange, 10);
            int shoot = random.Next((maxrange * maxrange));
            //int select = (int)Math.Floor(Math.Sqrt(shoot * 2));
            int select = (int)Math.Floor(Math.Sqrt(shoot)) + 1;
            select = maxrange - select;
            if (select < 0) select = 0;
            //if (select == 0) select = 1;
            return (gene)this[select];
        }

        public static int compare(gene a, gene b)
        {
            if (a.fitness > b.fitness) return 1;
            else if (a.fitness < b.fitness) return -1;
            else return 0;
        }
        public void remove_duplicate()
        {

        }

        public bool check_population()
        {
            for (int a = 0; a < Count; a++)
            {
                if (!this[a].check_gene())
                    return false;
            }
            return true;
        }
        public GenePopulation reproduction()
        {
            int i, reserv_leng, new_leng, num_node;

            gene tempgene = new gene(GenePopulation.num_gene);
            double temp_fit;
            bool flag;

            num_node = this[0].num_node;
            this.Sort(compare);
            reserv_leng = (int)(this.Count * 0.2);
            if (reserv_leng == 0) reserv_leng = 1;
            new_leng = (int)(this.Count * 0.2);
            GenePopulation newGenePop = new GenePopulation();
            newGenePop.Clear();
            for (i = 0; i < reserv_leng; i++)
            {
                newGenePop.Add(this[i]);
            }
            gene child, child2;
            for (i = reserv_leng; i < Count - new_leng; i++)
            {
                do
                {

                    gene parent1 = selection();
                    gene parent2 = selection();

                    child = parent1.crossover(parent2);
                    child2 = parent2.crossover(parent1);
                    if (child.calcFitness() > child2.calcFitness())
                        child = child2;
                    temp_fit = child.calcFitness();
                    double prob = random.NextDouble();
                    //if (false)
                    if (prob < mutationRate)
                    {
                        gene temp_child = new gene(GenePopulation.num_gene);
                        gene.copy_value(child, temp_child);
                        temp_child.mutate();
                        //   if (child.charge_seq.Count() > 3)
                        //      child.mutate();
                        //temp_child.calcFitness();

                        if (temp_child.check_gene() && temp_child.calcFitness() < temp_fit)
                        { //產生更好的基因則替換
                            child = temp_child;
                            // gene.copy_value(temp_child, child);
                        }

                    }
                    flag = false; ;

                } while (flag);
                newGenePop.Add(child);
            }
            // int tmethod=0;
            // if (common.assign_car_method > 0) tmethod = 2;
            for (i = Count - new_leng; i < Count; i++)
            {
                newGenePop.Add(gene.randomInstance(num_node, common.assign_car_method, num_car_used, 0));
            }
            newGenePop.Sort(compare);

            return newGenePop;
        }

        public void print()
        {
            int i = 1;
            foreach (gene c in this)
            {
                Console.WriteLine("{0:##} : {1}", i, c.ToString());
                i++;
            }
        }
    }

}
