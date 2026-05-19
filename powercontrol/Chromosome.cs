using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    public abstract class Chromosome
    {
        public double fitness;
        public int[] power_set;
        public bool[,] belong_set;
        public int max_node;
        abstract public double calcFitness(nodemap nodemap);
        abstract public Chromosome crossover(Chromosome spouse);
        abstract public void mutate();
        abstract public Chromosome randomInstance(int num_node);
    }

}
