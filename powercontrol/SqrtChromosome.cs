using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class SqrtChromosome : Chromosome
    {
        public static Random random = new Random(7);


        public string value;

        //   public double k = 2;
        public SqrtChromosome(int num_node)
        {
            max_node = num_node;
            power_set = new int[max_node];
            belong_set = new bool[max_node, common.max_num_group];
        }
        public override double calcFitness(nodemap nodemap)
        { //to do
          // 計算每一群的覆蓋率，必須都大於指定覆蓋率，計算平均覆蓋率A (越高越好)
          // 假設每次節點出現於分群之中，就耗一定電量，計算一輪之後，耗電最多的節點耗電 B (越小越好)
          // fitness = A/B，越大越好
          /*
          for (int i = 1; i < max_node; i++)
              nodemap.node[i].prange = power_set[i];
          nodemap.rebuild_tree();
          fitness = nodemap.check_node_load();
          */
            double coverage = 0;
            double energyC = 0;
            for (int i = 1; i < common.max_num_group; i++)
            {
                //nodemap.clearset(); //清除使用標記
                //nodemap.markuse(); //設定使用標記，並累計耗能
                //coverage += nodemap.cal_coverage(); //累計coverage
            }
            coverage = coverage / common.max_num_group;
            //energyC = nodemap.find_max_count();
            fitness = coverage / energyC;
            return fitness;
        }

        public override Chromosome crossover(Chromosome spouse)
        {
            //       SqrtChromosome ss = spouse as SqrtChromosome;
            int ni;
            int cutIdx = random.Next(max_node);

            SqrtChromosome child = new SqrtChromosome(max_node);

            ni = 0;
            for (int i = cutIdx; i < max_node; i++)
                child.power_set[ni++] = ((SqrtChromosome)spouse).power_set[i];
            for (int i = 0; i < cutIdx; i++)
                child.power_set[ni++] = power_set[i];

            return child;
        }

        public override void mutate()
        {
            int cutIdx = random.Next(max_node);
            if (cutIdx == 0) cutIdx = 1;
            power_set[cutIdx] = nodemap.power_rank[random.Next(7)];

        }

        public override Chromosome randomInstance(int num_node)
        {
            SqrtChromosome chrom = new SqrtChromosome(num_node);
            chrom.max_node = num_node;
            chrom.power_set[0] = nodemap.power_rank[6];
            for (int i = 1; i < num_node; i++)
                chrom.power_set[i] = nodemap.power_rank[random.Next(7)];

            return chrom;
        }

    }
}
