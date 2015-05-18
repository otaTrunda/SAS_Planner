using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SASPlan
{
    class TestResult
    {
        public static int IDStart = 0, currentID = 0;

        public Domain d;
        public HashSet<int> pattern;
        public double creation, search;
        public int nodes;

        public TestResult(Domain d, HashSet<int> pattern, double creationTime, double searchTime, int expandedNodes)
        {
            this.d = d;
            this.pattern = pattern;
            this.creation = creationTime;
            this.search = searchTime;
            this.nodes = expandedNodes;
        }
    }
}
