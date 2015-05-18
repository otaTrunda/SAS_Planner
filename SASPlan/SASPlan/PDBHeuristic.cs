using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SASPlan
{
    class PDBHeuristic : Heuristic
    {
        private List<int> variables;
        private Dictionary<int, HashSet<int>> edges;
        private List<HashSet<int>> components;
        private bool[] visited;
        private Dictionary<HashSet<int>, Dictionary<int[], int>> patternValues;

        public override string getDescription()
        {
            return "PDB heuristic";
        }

        public PDBHeuristic(Domain dom)
        {
            this.dom = dom;
            //findAdditivePatterns(dom);
        }

        /// <summary>
        /// Creates a database from given pattern
        /// </summary>
        /// <param name="selectedVariables"></param>
        /// <returns></returns>
        public void initializePatterns(HashSet<int> selectedVariables)
        {
            Console.WriteLine("Building the patterns database...");
            this.components = new List<HashSet<int>>();
            components.Add(selectedVariables);
            computeDistances();
        }

        //komponenty souvislosti. V grafu promennych budou spojenne prave kdyz existuje operator ktery je meni naraz
        private void findAdditivePatterns(Domain dom)
        {
            Console.WriteLine("Building the patterns database...");
            variables = new List<int>(dom.variablesCount);
            visited = new bool[dom.variablesCount];
            edges = new Dictionary<int, HashSet<int>>();
            for (int i = 0; i < dom.variablesCount; i++)
            {
                variables.Add(i);
                edges.Add(i, new HashSet<int>());
            }
            foreach (var op in dom.operators)
            {
                for (int i = 0; i < op.effects.Length; i++)
                {
                    for (int j = i+1; j < op.effects.Length; j++)
                    {
                        edges[op.effects[i].effectVariable].Add(op.effects[j].effectVariable);
                        edges[op.effects[j].effectVariable].Add(op.effects[i].effectVariable);
                    }
                }
            }
            findComponents();
            deleteNonGoalComponents();
            Console.WriteLine(components.Count + " patterns found.");
            computeDistances();
            Console.WriteLine("Done");
        }

        private void computeDistances()
        {
            patternValues = new Dictionary<HashSet<int>, Dictionary<int[], int>>();
            foreach (var item in components)
            {
                Console.Write("Computing pattern ");
                foreach (var ff in item)
                    Console.Write(ff + " ");
                Console.WriteLine();
                patternValues.Add(item, new Dictionary<int[], int>(new ArrayEqualityComparer()));
                AbstractState.setNotAbstractedVariables(item);
                computeDistancesToGoal(item);
            }
        }

        private int[] getValues(State s)
        {
            return s.getAllValues();
        }

        private void computeDistancesToGoal(HashSet<int> item)
        {
            IHeap<int, State> fringe = new LeftistHeap<State>();
            insertAllGoalStates(fringe, item);
            long stateSpaceSize = 1;
            foreach (var varr in item)
                stateSpaceSize *= dom.variablesDomainsRange[varr];
            
            Console.WriteLine("Pattern's size: " + item.Count +" state space size: " + stateSpaceSize);

            while (fringe.size() > 0)
            {
                int stateDistance = fringe.getMinKey();
                State state = fringe.removeMin();
                int[] stateValues = getValues(state);
                if (patternValues[item].ContainsKey(stateValues))
                {
                    continue;
                }
                patternValues[item].Add(stateValues, stateDistance);
                //List<HashSet<int>> predecessors = new List<HashSet<int>>();
                var pre = dom.getPredecessors(state);

                foreach (var op in pre.Keys)
                {
                    //HashSet<int> preValues = getValues(pre[op]);
                    if (!patternValues[item].ContainsKey(pre[op].getAllValues()))
                        fringe.insert(stateDistance + op.cost, pre[op]);
                }
            }
        }

        private void insertAllGoalStates(IHeap<int, State> fringe, HashSet<int> patternVariables)
        {
            insertAllGoalStates(fringe, patternVariables, getNextNotAbstractedVariable(-1), new List<int>());
        }

        private void insertAllGoalStates(IHeap<int, State> fringe, HashSet<int> patternVariables, int currentVariable, List<int> values)
        {
            if (values.Count == patternVariables.Count)
            {
                State s = new AbstractState(dom);
                int i = 0;
                foreach (var item in AbstractState.notAbstractedVariablesIndices.Keys)
                {
                    s.setValue(item, values[i]);
                    i++;
                }
                fringe.insert(0, s);
                return;
            }
            if (dom.goalConditions.ContainsKey(currentVariable))
            {
                values.Add(dom.goalConditions[currentVariable]);
                insertAllGoalStates(fringe, patternVariables, getNextNotAbstractedVariable(currentVariable), values);
                values.RemoveAt(values.Count - 1);
                return;
            }
            for (int i = 0; i < dom.variablesDomainsRange[currentVariable]; i++)
            {
                values.Add(i);
                insertAllGoalStates(fringe, patternVariables, getNextNotAbstractedVariable(currentVariable), values);
                values.RemoveAt(values.Count - 1);
            }

        }

        private int getNextNotAbstractedVariable(int currentVar)
        {
            bool found = currentVar < 0;
            foreach (var item in AbstractState.notAbstractedVariablesIndices.Keys)
            {
                if (found)
                    return item;
                if (item == currentVar)
                    found = true;
            }
            return -1;
        }

        private void deleteNonGoalComponents()
        {
            components.RemoveAll(a => !(dom.goalConditions.Keys.Any(b => a.Contains(b))));
            //components.RemoveAll(a => !(intersectsGoal(a)));
        }

        private bool intersectsGoal(HashSet<int> component)
        {
            return dom.goalConditions.Keys.Any(a => component.Contains(a));
            /*
            foreach (var item in dom.goalConditions.Keys)
            {
                if (component.Contains(item))
                    return true;
            }
            return false;
            */
        }

        private void findComponents()
        {
            components = new List<HashSet<int>>();
            for (int i = 0; i < dom.variablesCount; i++)
            {
                if (!visited[i])
                {
                    components.Add(new HashSet<int>());
                    addAllReachable(i, components.Count - 1);
                }
            }
        }

        private void addAllReachable(int variable, int componentNumber)
        {
            if (!visited[variable])
            {
                visited[variable] = true;
                components[componentNumber].Add(variable);
                foreach (var item in edges[variable])              
                    addAllReachable(item, componentNumber);  
            }
        }

        public override int getValue(State state)
        {
            heuristicCalls++;
            int result = 0;
            foreach (var item in patternValues.Keys)
            {
                int[] notAbstractedValues = new int[item.Count];
                int i = 0;
                foreach (var variable in item)
                {
                    notAbstractedValues[i++] = state.getValue(variable);
                }
                if (!patternValues[item].ContainsKey(notAbstractedValues))
                    return int.MaxValue;
                result += patternValues[item][notAbstractedValues];
            }
            return result;
        }
    }
}
