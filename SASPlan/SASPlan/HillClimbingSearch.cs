using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SASPlan
{
    class HillClimbingSearch : HeuristicSearchEngine
    {
        private State currentState;

        public override int search(bool quiet = false)
        {
            List<int> bestOperators = new List<int>(); //list of operators that are equally good and all of them are best
            solution = new List<int>();
            printMessage("search started. HillClimbingSearch on " + dom.problemName + ", " + h.ToString(), quiet);
            DateTime start = DateTime.Now;

            int length = 0;
            currentState = dom.initialState;
            while (!dom.isGoalState(currentState))
            {
                var successors = dom.getSuccessors(currentState);
                if (successors.Count == 0)
                {
                    printMessage("search FAILED - deadend reached", quiet);
                    break;
                }
                int bestVal = int.MaxValue;
                Operator bestOp = null;
                
                foreach (var op in successors.Keys)
                {
                    int val = op.cost + h.getValue(successors[op]);
                    if (val < bestVal)
                    {
                        bestVal = val;
                        bestOperators.Clear();
                        bestOperators.Add(op.orderIndex);
                    }
                    if(val == bestVal)
                        bestOperators.Add(op.orderIndex);
                }
                bestOp = dom.operators[bestOperators[Program.r.Next(bestOperators.Count)]];
                solution.Add(bestOp.orderIndex);
                currentState = successors[bestOp];
                length += bestOp.cost;
            }
            DateTime end = DateTime.Now;
            printMessage("search ended in " + (end - start).TotalSeconds + " seconds", quiet);
            printMessage("plan length " + length, quiet);
            return length;
        }

        public HillClimbingSearch(Domain d, Heuristic h)
        {
            this.dom = d;
            this.h = h;
        }


    }
}
