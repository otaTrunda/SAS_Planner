﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SASPlan
{
    class MCTSSolver : HeuristicSearchEngine
    {
        public TreeNode root;
        private Plan bestPlan;
        private int bestValue, worstValue = 0;
        private long steps = 0;
        private int policyIndex = 0;
        private List<SimulationPolicy> simulationPolicies;
        private long stepsWithoutImprovement = 0;
        private BestPerformance perfoHeuristic;

        private void doOneStep(bool quiet = false)
        {
            steps++;
            Plan plan = new Plan(dom, h);
            TreeNode node = root.selectSuccesor(plan);
            plan.finalPosition = node.position.clone();
            plan.runSimulation(simulationPolicies[policyIndex]);
            policyIndex = (policyIndex + 1) % simulationPolicies.Count;
            int value = plan.eval();
            perfoHeuristic.updateStats(plan);
            if (bestValue > value)
            {
                bestValue = value;
                bestPlan = plan;
                printMessage("Best: " + bestValue + "\tSteps taken: " + steps + "\tTree size: " +
                    (root.subtreeSize + 1) + "\tMax depth: " + (root.subtreeDepth + 1), quiet);
                stepsWithoutImprovement = 0;
            }
            if (value > worstValue)
                worstValue = value;
            node.update(worstValue - value);
            if (node.nVisited > TreeNode.visitedBeforeExpansion)
                node.expand();
            stepsWithoutImprovement++;
        }

        public MCTSSolver(Domain dom, Heuristic h)
        {
            this.dom = dom;
            this.h = h;
            this.root = TreeNode.createRoot(dom.initialState);
            TreeNode.dom = dom;
            TreeNode.solver = this;
            //TODO only 1 MCTS solver may run at the same time!!

            bestPlan = null;
            bestValue = int.MaxValue;
            this.perfoHeuristic = new BestPerformance(dom);
            this.simulationPolicies = new List<SimulationPolicy>();
            
            this.simulationPolicies.Add(new SearchEngineSimulationPolicy(new HillClimbingSearch(dom, h), h, dom));
            this.simulationPolicies.Add(new RandomSimulationPolicy(dom));
            this.simulationPolicies.Add(this.perfoHeuristic);
        }

        private void extractPlan()
        {
            this.solution = bestPlan.actions;
            return;

            this.solution = new List<int>();
            TreeNode n = root;
            while (n != null)
            {
                if (n.succesors.Count == 0) break;
                List<int> bestSuccessors = new List<int>();
                double bestVisited = 0;
                for (int i = 0; i < n.succesors.Count; i++)
                {
                    if (n.succesors[i].nVisited > bestVisited)
                    {
                        bestSuccessors.Clear();
                        bestSuccessors.Add(i);
                        bestVisited = n.succesors[i].nVisited;
                        continue;
                    }
                    if (n.succesors[i].nVisited == bestVisited)
                    {
                        bestSuccessors.Add(i);
                    }
                }
                int succ = bestSuccessors[Program.r.Next(bestSuccessors.Count)];
                solution.Add(n.succesors[succ].action);
                n = n.succesors[succ];
            }
        }

        public override int search(bool quiet = false)
        {
            DateTime start = DateTime.Now, end;
            printMessage("search started. MCTS engine " + dom.problemName + ", " + h.ToString(), quiet);
            bestPlan = null;
            bestValue = int.MaxValue;
            while (stepsWithoutImprovement < 2000)
            {
                if (!root.isLeaf && root.succesors.Count <= 0)
                    break;
                for (int j = 0; j < 400; j++)
                {
                    if (root.isLeaf || root.succesors.Count > 0)
                        doOneStep(quiet);
                }
                root.recalculateSubtreeSize();
                printMessage("Best: " + bestValue + "\tSteps taken: " + steps + "\tTree size: " +
                    (root.subtreeSize + 1) + "\tMax depth: " + (root.subtreeDepth + 1) + "\tEBF: " + 
                    (computeEffectiveBranching(root.subtreeSize, root.subtreeDepth)), quiet);
            }
            end = DateTime.Now;
            printMessage("search ended in " + (end - start).TotalSeconds + " seconds", quiet);
            extractPlan();
            printMessage("Best: " + bestValue + "\tSteps taken: " + steps + "\tTree size: " +
                    (root.subtreeSize + 1) + "\tMax depth: " + (root.subtreeDepth + 1), quiet);
            return bestPlan.actions.Count;
        }

        public void doMoreSteps(long howMuch, bool quiet = false)
        {
            for (int i = 0; i < howMuch; i++)
            {
            }
        }
        
        public void prune()
        {
            List<int> solution = (new int[] { 4, 1, 4, 1, 2, 1, 2, 4, 3, 0, 3, 4, 2, 6, 5, 7, 2, 2, 3 }).ToList();
            TreeNode n = root;
            int index = 0;
            while(n.succesors.Count == 1)
            {
                n = n.succesors[0];
                index++;
            }
            var t = n.succesors;
            n.succesors = new List<TreeNode>();
            n.succesors.Add(t[index]);
        }

        private double computeEffectiveBranching(int treeSize, int maxDepth)
        {
            if (treeSize <= 100 || maxDepth < 1)
                return 1;
            double estimate = Math.Log(treeSize, maxDepth + 1);
            double nominator = (1 - Math.Pow(estimate, maxDepth));
            double denumerator = (1 - estimate);
            double size = nominator / denumerator;
            while (Math.Abs((1 - Math.Pow(estimate, maxDepth)) / (1 - estimate) - treeSize) > 1)
            {
                double updateUP = (1 - Math.Pow(estimate, maxDepth)) / (1 - estimate) - treeSize,
                    updateDOWN = (Math.Pow(estimate, maxDepth) * (1 - maxDepth + estimate * (maxDepth - 2)) + 1) / (1 - 2 * estimate + estimate * estimate);

                estimate = estimate - updateUP/updateDOWN;

                nominator = (1 - Math.Pow(estimate, maxDepth));
                denumerator = (1 - estimate);
                size = nominator / denumerator;
            }
            return estimate;
        }   
    }

    class Plan
    {
        private static Random r = new Random();
        public Domain dom;
        public Heuristic h;
        public State finalPosition;
        public List<int> actions;
        public bool isCorrectPlan = true;
        private bool isEvaluated = false;
        private int value;

        public void addAction(Operator a)
        {
            actions.Add(a.orderIndex);

            if (finalPosition != null)
                finalPosition = Operator.apply(a, finalPosition);
        }

        public int eval()
        {
            if (isEvaluated)
                return value;
            //return h.getValue(finalPosition);
            if (dom.isGoalState(finalPosition))
            {
            }
            //return h.getValue(finalPosition);// *h.getValue(finalPosition) + actions.Count + (dom.isGoalState(finalPosition) ? 0 : 500);

            int totalCost = 0;
            for (int i = 0; i < actions.Count; i++)
                totalCost += dom.operators[actions[i]].cost;

            return totalCost;// +h.getValue(finalPosition);// +goalHeuristic.getValue(finalPosition) * goalHeuristic.getValue(finalPosition);
        }

        public void runSimulation(int bestSoFar)
        {
            throw new NotImplementedException();
            /*
            //List<int> solution = (new int[] { 4, 1, 4, 1, 2, 1, 2, 4, 3, 0, 3, 4, 2, 6, 5, 7, 2, 2, 3 }).ToList();
            int totalCost = 0;
            Operator next;
            for (int i = 0; i < maxSimulLength; i++)
			{
                if (dom.isGoalState(finalPosition))
                    break;
                if (totalCost > 2*bestSoFar)
                    break;
                //TODO napsat poradne aby se tam nevolaly zbytecne heurisitky vickrat na stejny stav
                var succesors = dom.getSuccessors(finalPosition);
                if (succesors.Count <= 0)
                    break;
                if (r.NextDouble() < randomWalkChance)                
                    next = succesors.Keys.ElementAt(r.Next(succesors.Count));                
                else
                    next = h.getBestStateIndex(succesors);
                if (r.NextDouble() < (1-allowWorsChance) && h.getValue(succesors[next]) > h.getValue(finalPosition))
                    break; 
                finalPosition = succesors[next];
                actions.Add(next.orderIndex);
            }
             */
        }

        public Plan(Domain d, Heuristic h)
        {
            this.actions = new List<int>();
            this.dom = d;
            this.h = h;
            //this.goalHeuristic = new NotAccomplishedGoalCount(d);
        }

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            foreach (var item in actions)
            {
                b.AppendLine(item.ToString());
            }
            return b.ToString();
        }

        internal void runSimulation(SimulationPolicy simulationPolicy)
        {
            List<int> result = simulationPolicy.runSimulation(this.finalPosition);
            this.value = 0;
            foreach (var item in this.actions)
                this.value += dom.operators[item].cost;
            foreach (var item in result)
            {
                this.actions.Add(item);
                this.value += dom.operators[item].cost;
            }
            this.isEvaluated = true;
        }
    }
    
    class TreeNode
    {
        public static MCTSSolver solver;
        public static Domain dom;
        public static readonly double desiredBias = 0.05;
        private static readonly int minSuccesorPruningLimit = 4, minVisitedPruningLimit = 50;
        private static double realBias = 0.2;
        public static int visitedBeforeExpansion = 2;
        private static double minScore = 1, maxScore = 0;
        private TreeNode parrent;
        public List<TreeNode> succesors;
        public bool isLeaf, isFinished = false;
        public int action; //action that leads from position represented by parrent node to this position
        public State position;
        public double scoreSum, nVisited;
        public int subtreeSize = 0, subtreeDepth = 0;

        public void updateSubtreeDepth(int newDepth)
        {
            if (newDepth > subtreeDepth)
            {
                subtreeDepth = newDepth;
                if (parrent != null)
                    parrent.updateSubtreeDepth(subtreeDepth + 1);
            }
        }

        public int recalculateSubtreeSize()
        {
            if (isLeaf)
                return 1;
            int size = 0;
            int depth = 0;
            foreach (var item in succesors)
            {
                size += item.recalculateSubtreeSize();
                if (item.subtreeDepth > depth)
                    depth = item.subtreeDepth;
            }
            size = size + 1;
            depth = depth + 1;
            this.subtreeDepth = depth;
            this.subtreeSize = size;
            return size;
        }

        public void updateSubtreeSize(int additionalNodesCount)
        {
            this.subtreeSize += additionalNodesCount;
            if (this.parrent != null)
                parrent.updateSubtreeSize(additionalNodesCount);
        }

        private TreeNode(TreeNode parrent)
        {
            this.parrent = parrent;
            this.scoreSum = parrent.scoreSum / parrent.nVisited;
            this.nVisited = 1;
            succesors = new List<TreeNode>();
        }

        private TreeNode()
        {
        }

        public static TreeNode createRoot(State position)
        {
            TreeNode node = new TreeNode();
            node.succesors = new List<TreeNode>();
            node.parrent = null;
            node.scoreSum = 0;
            node.nVisited = 1;
            node.isLeaf = true;
            node.position = position;
            return node;
        }
        
        public void expand()
        {
            if (isFinished)
            {
                removeNode();
                return;
            }
            if (dom.isGoalState(position))
            {
                isFinished = true;
                return;
            }
            this.isLeaf = false;
            var possibleActions = dom.getSuccessors(position);
            if (possibleActions.Count == 0)
            {
                removeNode();
                isFinished = true;
                return;
            }
            foreach (var item in possibleActions.Keys)
            {
                TreeNode succesor = createLeaf(this, item);
                succesor.position = this.position.clone();
                succesor.position = Operator.apply(item, succesor.position);
                succesors.Add(succesor);
                if (succesor.position == null)
                {
                    throw new Exception();
                }
            }
            this.position = null;
            this.updateSubtreeSize(possibleActions.Count);
            this.updateSubtreeDepth(1);
        }
        
        public void removeNode()
        {
            if (parrent != null)
            {
                parrent.succesors.Remove(this);
                if (parrent.succesors.Count == 0)
                    parrent.removeNode();
            }
        }
        
        public static TreeNode createLeaf(TreeNode parrent, Operator move)
        {
            TreeNode node = new TreeNode(parrent);
            node.isLeaf = true;
            node.action = move.orderIndex;
            return node;
        }

        private void updateBias(double score)
        {
            //desiredBias *= 0.9999;
            if (score < minScore)
                minScore = score;
            if (score > maxScore)
                maxScore = score;
            realBias = desiredBias * ((maxScore - minScore) + minScore);
        }

        public void update(double score)
        {
            this.nVisited++;
            this.scoreSum += score;
            if (this.parrent != null)
                parrent.update(score);
            else
                updateBias(score);
        }

        public double eval()
        {
            if (parrent == null)
                return 0;
            double expectation = scoreSum / nVisited;
            double urgency = realBias * Math.Sqrt(Math.Log(parrent.nVisited) / nVisited);
            return expectation + urgency;
        }

        private double getExpectation()
        {
            return scoreSum / nVisited;
        }

        public void prune()
        {
            if (nVisited < minVisitedPruningLimit)
                return;
            int worst = 0, best = 0;
            for (int i = 0; i < succesors.Count; i++)
            {
                if (succesors[i].getExpectation() < succesors[worst].getExpectation())
                    worst = i;
                if (succesors[i].getExpectation() > succesors[best].getExpectation())
                    best = i;
                succesors[i].prune();
            }
            //succesors[best].prune();
            if (succesors.Count > minSuccesorPruningLimit)
                succesors.RemoveAt(worst);
        }

        public TreeNode selectSuccesor(Plan simulPlan)
        {
            if (this.isLeaf)
                return this;
            if (this.succesors.Count == 0)
            {
                removeNode();
                simulPlan.actions.RemoveAt(simulPlan.actions.Count - 1);
                return parrent.selectSuccesor(simulPlan);
            }
            double max = -10;
            TreeNode bestSuccesor = null;
            foreach (var item in succesors)
            {
                if (item.isFinished)
                {
                    continue;
                }
                double val = item.eval();
                if (val > max)
                {
                    max = val;
                    bestSuccesor = item;
                }
            }
            if (bestSuccesor == null)
            {
                this.isFinished = true;
                bestSuccesor = succesors[0];
            }
            simulPlan.addAction(dom.operators[bestSuccesor.action]);
            return bestSuccesor.selectSuccesor(simulPlan);
        }
    }

    abstract class SimulationPolicy
    {
        public abstract List<int> runSimulation(State s);
    }

    class SearchEngineSimulationPolicy : SimulationPolicy
    {
        private HeuristicSearchEngine engine;
        private Heuristic h;
        private Domain d;

        public SearchEngineSimulationPolicy(HeuristicSearchEngine engine, Heuristic h, Domain d)
        {
            this.engine = engine;
            this.h = h;
            this.d = d;
        }

        public override List<int> runSimulation(State s)
        {
            this.d.initialState = s;
            engine.search(true);
            return engine.getSolution();
        }
    }

    class RandomSimulationPolicy : SimulationPolicy
    {
        public int maxLenght = int.MaxValue;
        private List<int> simulation;
        private Domain dom;

        public RandomSimulationPolicy(Domain dom)
        {
            this.simulation = new List<int>();
            this.dom = dom;
        }

        public override List<int> runSimulation(State s)
        {
            simulation.Clear();
            State state = s;
            for (int i = 0; i < maxLenght; i++)
            {
                if (dom.isGoalState(state))
                    break;
                var successors = dom.getSuccessors(state);
                Operator op = successors.Keys.ElementAt(Program.r.Next(successors.Keys.Count));                
                simulation.Add(op.orderIndex);
                state = Operator.apply(op, state);
            }
            return simulation;
        }
    }

    class BestResponse : SimulationPolicy
    {
        public void updateStats(Plan simulation)
        {

        }

        public override List<int> runSimulation(State s)
        {
            throw new NotImplementedException();
        }
    }

    class BestPerformance : SimulationPolicy
    {
        public int maxLenght = int.MaxValue;
        private List<int> simulation;
        private Domain dom;
        private double[] simResultSum;
        private int[] simulationsCount;
        List<double> cummulatedValues;
        List<int> operatorIndices;
        double tournamentSizeProportional = 0.7;

        public BestPerformance(Domain dom)
        {
            this.dom = dom;
            this.cummulatedValues = new List<double>();
            this.operatorIndices = new List<int>();
            this.simResultSum = new double[dom.operators.Count];
            this.simulation = new List<int>();
            this.simulationsCount = new int[dom.operators.Count];
        }

        public void updateStats(Plan simulation)
        {
            foreach (var item in simulation.actions)
            {
                simResultSum[item] = simResultSum[item] + simulation.eval();
                simulationsCount[item] = simulationsCount[item] + 1;
            }
        }

        /// <summary>
        /// Vyuziva turnajovou selekci
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public override List<int> runSimulation(State s)
        {
            simulation.Clear();
            State state = s;
            for (int i = 0; i < maxLenght; i++)
            {
                if (dom.isGoalState(state))
                    break;
                var successors = dom.getSuccessors(state);
                if (successors.Count <= 0)
                    return simulation;
                int opIndex = -1;
                double opPerformance = double.MaxValue;
                while (opIndex == -1)
                    foreach (var item in successors.Keys)
                    {
                        if (Program.r.NextDouble() <= tournamentSizeProportional)
                        {
                            if (opPerformance > getOperatorPerformance(item.orderIndex))
                            {
                                opPerformance = getOperatorPerformance(item.orderIndex);
                                opIndex = item.orderIndex;
                            }
                        }
                    }

                simulation.Add(opIndex);
                state = Operator.apply(dom.operators[opIndex], state);
            }
            return simulation;
        }

        private double getOperatorPerformance(int opIndex)
        {
            return simResultSum[opIndex] / (simulationsCount[opIndex] + 1);
        }

        /// <summary>
        /// Metoda nepracuje spravne - uprednostnuje akce ktere vedou na vyssi vysledek simulace (ma to byt naopak)
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public List<int> runSimulationRoulette(State s)
        {
            simulation.Clear();
            State state = s;
            for (int i = 0; i < maxLenght; i++)
            {
                if (dom.isGoalState(state))
                    break;
                var successors = dom.getSuccessors(state);
                cummulatedValues.Clear();
                operatorIndices.Clear();
                double totalSum = 0;
                foreach (var item in successors.Keys)
                {
                    totalSum += simResultSum[item.orderIndex] / (simulationsCount[item.orderIndex] + 1);
                    cummulatedValues.Add(totalSum);
                    operatorIndices.Add(item.orderIndex);
                }
                double rand = Program.r.NextDouble() * totalSum;
                int opIndex = 0;
                for (int j = 0; j < cummulatedValues.Count; j++)
                {
                    if (rand <= cummulatedValues[j])
                    {
                        opIndex = operatorIndices[j];
                        break;
                    }
                }

                simulation.Add(opIndex);
                state = Operator.apply(dom.operators[opIndex], state);
            }
            return simulation;
        }
    }
}
