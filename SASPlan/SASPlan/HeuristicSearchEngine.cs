using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SASPlan
{
    abstract class HeuristicSearchEngine
    {
        protected Domain dom;
        protected Heuristic h;
        /// <summary>
        /// Solution to the planning problem. After procedure "search" ends, this variable should contain a solution. Sequence of operators to use, operators are indexed by thier position in the input file.
        /// </summary>
        protected List<int> solution;

        public List<int> getSolution()
        {
            return solution;
        }

        public void setDomain(Domain dom)
        {
            this.dom = dom;
        }

        protected void printMessage(string message, bool quiet)
        {
            if (!quiet)
                Console.WriteLine(message);
        }

        public abstract int search(bool quiet = false);

    }
}
