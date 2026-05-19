using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public class Population : List<Chromosome>
    {
        static Random random = new Random(2);
        double mutationRate = 0.1;
        static nodemap nmap;
        public Population(nodemap inmap)
        {
            nmap = inmap;
        }
        public void initialize(Chromosome prototype, int popSize)
        {
            this.Clear();
            for (int i = 0; i < popSize; i++)
            {
                Chromosome newChrom = prototype.randomInstance(nmap.max_node);
                if (i == 0)
                {
                    for (int j = 0; j < nmap.max_node; j++)
                        for (int k = 0; k < common.max_num_group; k++)
                        {
                            if (random.Next(2) == 0)
                                ((SqrtChromosome)newChrom).belong_set[j, k] = false;
                            else
                                ((SqrtChromosome)newChrom).belong_set[j, k] = true;
                        }
                }
                newChrom.calcFitness(nmap);
                this.Add(newChrom);
            }
        }

        public Chromosome selection()
        {
            int shoot = random.Next((Count * Count) / 2);
            int select = (int)Math.Floor(Math.Sqrt(shoot * 2));
            if (select == 0) select = 1;
            return (Chromosome)this[select];
        }

        private static int compare(Chromosome a, Chromosome b)
        {
            if (a.fitness > b.fitness) return 1;
            else if (a.fitness < b.fitness) return -1;
            else return 0;
        }

        public Population reproduction()
        {
            this.Sort(compare);
            Population newPop = new Population(nmap);
            for (int i = 0; i < 5; i++)
            {
                newPop.Add(this[i]);
            }
            for (int i = 10; i < Count; i++)
            {
                Chromosome parent1 = selection();
                Chromosome parent2 = selection();

                Chromosome child = parent1.crossover(parent2);
                double prob = random.NextDouble();
                if (prob < mutationRate) child.mutate();
                child.calcFitness(nmap);
                newPop.Add(child);
            }
            newPop.Sort(compare);
            return newPop;
        }

        public void print()
        {
            int i = 1;
            foreach (Chromosome c in this)
            {
                Console.WriteLine("{0:##} : {1}", i, c.ToString());
                i++;
            }
        }
    }


    public class partical
    {
        //   static double gbest;
        //   static int[] gbest_x;
        static int max_node;
        public static int minlevel = 0, maxlevel = 6;
        // static Random rand = new Random();
        public double fit, pbest;
        public int[] px, pbest_x, pv;
        public partical(int nnode)
        {
            max_node = nnode;
            //       if (gbest_x != null)
            //           gbest_x = new int[nnode];
            pv = new int[nnode];
            px = new int[nnode];
            pbest_x = new int[nnode];
            px[0] = maxlevel;
            for (int i = 1; i < nnode; i++)
            {
                pv[i] = common.rand.Next(maxlevel) - 3;
                px[i] = common.rand.Next(maxlevel);
                pbest_x[i] = px[i];
            }

        }
        public double calcfitness(nodemap nmap)
        {
            for (int i = 1; i < max_node; i++)
                nmap.node[i].prange = nodemap.power_rank[px[i]];
            nmap.rebuild_tree();
            fit = nmap.check_node_load();
            return fit;
        }

    }
    public class PSO : List<partical>
    {
        static Random random = new Random(7);
        static int num_Particle = 50;  // Number of particles in population
        static int max_cycle = 50;  // Maximum iteration cycle
        static double C1 = 1.5, C2 = 1.5;
        static nodemap nmap;
        public double gbest;
        public int[] gbest_x;

        public PSO(nodemap inmap)
        {
            nmap = inmap;
        }
        public void initialize()
        {
            this.Clear();
            gbest_x = new int[nmap.max_node];
            gbest = common.MYINFINITE;
            for (int i = 0; i < num_Particle; i++)
            {
                partical newPart = new partical(nmap.max_node);
                newPart.pbest = newPart.calcfitness(nmap);
                if (newPart.pbest < gbest)
                {
                    gbest = newPart.pbest;
                    for (int j = 0; j < nmap.max_node; j++)
                        gbest_x[j] = newPart.pbest_x[j];
                }
                this.Add(newPart);
            }
        }

        private static int compare(partical a, partical b)
        {
            if (a.pbest > b.pbest) return 1;
            else if (a.pbest < b.pbest) return -1;
            else return 0;
        }

        public void optimization()
        {

            int i, j, k;
            double W1;
            int[] tempgbest_x = new int[nmap.max_node];
            double tempgbest = common.MYINFINITE;
            for (i = 0; i < max_cycle; i++)
            {
                W1 = (0.95 - 0.4) / max_cycle * i + 0.4;
                tempgbest = gbest;
                for (k = 0; k < nmap.max_node; k++)
                    tempgbest_x[k] = gbest_x[k];
                for (j = 0; j < num_Particle; j++)
                {
                    for (k = 0; k < nmap.max_node; k++)
                    {

                        this[j].pv[k] = (int)Math.Round(W1 * this[j].pv[k] + C1 * random.NextDouble() * (this[j].pbest_x[k] - this[j].px[k]) + C2 * random.NextDouble() * (gbest_x[k] - this[j].px[k]));
                        this[j].px[k] += this[j].pv[k];
                        if (this[j].px[k] > partical.maxlevel) this[j].px[k] = partical.maxlevel;
                        if (this[j].px[k] < partical.minlevel) this[j].px[k] = partical.minlevel;
                    }
                    this[j].fit = this[j].calcfitness(nmap);
                    if (this[j].fit < this[j].pbest)
                    {
                        for (k = 0; k < nmap.max_node; k++)
                            this[j].pbest_x[k] = this[j].px[k];
                        this[j].pbest = this[j].fit;
                        if (this[j].pbest < tempgbest)
                        {
                            for (k = 0; k < nmap.max_node; k++)
                                tempgbest_x[k] = this[j].pbest_x[k];
                            tempgbest = this[j].pbest;
                        }
                    }
                }
                gbest = tempgbest;
                for (k = 0; k < nmap.max_node; k++)
                    gbest_x[k] = tempgbest_x[k];

            }

        }

    }
}
