using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SASPlan
{
    abstract class Heuristic
    {
        protected Domain dom;
        public abstract int getValue(State state);
        public abstract string getDescription();
        public long heuristicCalls = 0;

        public override string ToString()
        {
            return getDescription();
        }

        public Operator getBestStateIndex(Dictionary<Operator, State> states)
        {
            Operator best = null;
            int bestValue = int.MaxValue;
            foreach (var item in states.Keys)
            {
                int val = getValue(states[item]);
                if (val < bestValue)
                {
                    best = item;
                    bestValue = val;
                }
            }
            return best;
        }
    }

    class BlindHeuristic : Heuristic
    {
        public override int getValue(State state)
        {
            heuristicCalls++;
            return 0;
        }

        public override string getDescription()
        {
            return "Blind heuristic";
        }
    }

    class NotAccomplishedGoalCount : Heuristic
    {
        public override int getValue(State state)
        {
            heuristicCalls++;
            int result = 0;
            foreach (var item in dom.goalConditions.Keys)
            {
                if (!state.hasValue(item, dom.goalConditions[item]))
                    result++;
            }
            return result;
        }

        public override string getDescription()
        {
            return "Not Accomplished Goals Count heuristic";
        }

        public NotAccomplishedGoalCount(Domain d)
        {
            this.dom = d;
        }
    }

    class AbstractStateSizeHeuristic : Heuristic
    {
        public override int getValue(State state)
        {
            heuristicCalls++;
            if (state is SasState)
            {
                int result = 0;
                foreach (var item in dom.goalConditions.Keys)
                {
                    if (!state.hasValue(item, dom.goalConditions[item]))
                        result++;
                }
                return result;
            }
            else
            {
                Red_BlackState s = (Red_BlackState)state;
                return 10000 - 10 * s.size();
            }
        }

        public override string getDescription()
        {
            return "Abstract state size heuristic";
        }

        public AbstractStateSizeHeuristic(Domain d)
        {
            this.dom = d;
        }
    }

    class DeleteRelaxationHeuristic_Perfect : Heuristic
    {
        private Red_BlackDomain rbDom;
        private AStarSearch ast;

        public override string getDescription()
        {
            return "Perfect delete relaxation heuristic";
        }
        
        public override int getValue(State state)
        {
            heuristicCalls++;
            rbDom.initialState = new Red_BlackState(state, rbDom);
            this.ast = new AStarSearch(rbDom, new AbstractStateSizeHeuristic(rbDom));
            return ast.search(true);
        }

        public DeleteRelaxationHeuristic_Perfect(Domain d)
        {
            this.dom = d;
            this.rbDom = Red_BlackDomain.readFromFile(d.problemName);
            rbDom.makeAllAbstracted();
        }
    }

    class PlannigGraphLayersHeuristic : Heuristic
    {
        private Red_BlackDomain rbDom;

        public override int getValue(State state)
        {
            heuristicCalls++;
            int result = 0;
            State s = new Red_BlackState(state, rbDom);
            while (!dom.isGoalState(s))
            {
                var operators = rbDom.getApplicableRelevantOps(s);
                if (operators == null || operators.Count == 0)
                    return int.MaxValue / 2;
                
                foreach (var item in operators)
                {
                    s = Operator.apply(item, s);
                }
                //s = Operator.apply(op, s);
                result++;
            }
            /*
            PlanningGraphComputation pgc = new PlanningGraphComputation(this.dom);
            pgc.computePlanningGraph(state);
            if (pgc.OpsLayers.Count != result)
            {
                Console.WriteLine("chyba");
            }
             */
            return result;
        }

        public int getValue1Overestimating(State state)
        {
            int result = 0;
            State s = new Red_BlackState(state, rbDom);
            while (!dom.isGoalState(s))
            {
                var op = dom.getApplicableRelevantOp(s);
                if (op == null)
                    return int.MaxValue / 2;
                /*
                foreach (var item in operators)
                {
                    s = Operator.apply(item, s);
                }*/
                s = Operator.apply(op, s);
                result++;
            }
            return result;
        }

        public override string getDescription()
        {
            return "PlannigGraph Layers Count heuristic";
        }

        public PlannigGraphLayersHeuristic(Domain d)
        {
            this.dom = d;
            this.rbDom = Red_BlackDomain.readFromFile(d.problemName);
            rbDom.makeAllAbstracted();
        }
    }

    class PlanningGraphComputation
    {
        private Red_BlackDomain rbDom;
        public List<State> stateLayers;
        public List<List<Operator>> OpsLayers;
        public Dictionary<int, Dictionary<int, Dictionary<int, List<int>>>> supportOp;
        public bool isCutOff = false;
        private const int cutOffLimit = 100;

        /// <summary>
        /// Returns list of indices of all operators that can accomplish the given fact in the given fact-layer. 
        /// Operators are described by their indices in the privious op-layer.
        /// If the fact is already present in the previous fact-layer, this method returns null.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="variable"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public List<int> getSupport(int layer, int variable, int value)
        {
            if (!supportOp.ContainsKey(layer) ||
                !supportOp[layer].ContainsKey(variable) ||
                !supportOp[layer][variable].ContainsKey(value))
                return null;
            return supportOp[layer][variable][value];
        }

        /// <summary>
        /// Only adds a support for newly accomplished facts. If the fact has already been accomplished before, then this method should not be called on that fact.
        /// Support is an operator that accomplished the fact. Operator is described by its index in the previous op-layer.
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="variable"></param>
        /// <param name="value"></param>
        /// <param name="support"></param>
        private void addSupport(int layer, int variable, int value, int support)
        {
            if (!supportOp.ContainsKey(layer))
                supportOp.Add(layer, new Dictionary<int, Dictionary<int, List<int>>>());
            if (!supportOp[layer].ContainsKey(variable))
                supportOp[layer].Add(variable, new Dictionary<int, List<int>>());
            if (!supportOp[layer][variable].ContainsKey(value))
                supportOp[layer][variable].Add(value, new List<int>());
            supportOp[layer][variable][value].Add(support);     
        }

        public void computePlanningGraph(State state)
        {
            stateLayers.Clear();
            OpsLayers.Clear();
            supportOp.Clear();
            isCutOff = false;
            State s = new Red_BlackState(state, rbDom);
            stateLayers.Add(s);

            while (!rbDom.isGoalState(stateLayers[stateLayers.Count-1]))
            {
                bool addedSomething = false;
                s = stateLayers[stateLayers.Count-1].clone();
                var ops = rbDom.getApplicableRelevantOps(s);
                for (int o = 0; o < ops.Count; o++)
                {
                    var item = ops[o];
                    s = Operator.apply(item, s);
                    for (int i = 0; i < item.effects.Length; i++)
                    {
                        var eff = item.effects[i];
                        if (!stateLayers[stateLayers.Count-1].hasValue(eff.effectVariable, eff.effectValue))
                        {
                            addSupport(OpsLayers.Count, eff.effectVariable, eff.effectValue, o);
                            addedSomething = true;
                        }
                    }
                }
                OpsLayers.Add(ops);
                stateLayers.Add(s);
                if (!addedSomething)
                    break;
                if (stateLayers.Count > cutOffLimit)
                {
                    isCutOff = true;
                    break;
                }
            }

            /*
            for (int i = 0; i < rbDom.variablesCount; i++)
            {
                for (int j = 0; j < rbDom.variablesDomainsRange[i]; j++)
                {
                    if (stateLayers[stateLayers.Count - 1].hasValue(i, j))
                    {
                        var support = getSupport(stateLayers.Count - 2, i, j);
                    }

                }
            }*/
        }

        public PlanningGraphComputation (Domain d)
	    {
            this.rbDom = Red_BlackDomain.readFromFile(d.problemName);
            rbDom.makeAllAbstracted();
            this.OpsLayers = new List<List<Operator>>();
            this.stateLayers = new List<State>();
            this.supportOp = new Dictionary<int, Dictionary<int, Dictionary<int, List<int>>>>();
	    }
    }

    class FFHeuristic : Heuristic
    {
        private PlanningGraphComputation PG;
        //private Red_BlackDomain rbDom;

        //Dalo by se asi urychlit: misto dictionary pouzit normalne pole, mit tam vsechny promenne (ne jen ty potrebne), ale u tech nepotrebnych by ten list byl prazdny
        //Ty listy by se nevytvarely vzdycky znoval, ale pouze by se Clearovaly. Navic se daji ty listy nahradit HashSet aby addNewGoalRequest bylo rychlejsi

        private Dictionary<int, List<int>> notAchievedGoals = new Dictionary<int, List<int>>(),
                notAchievedGoalsNew = new Dictionary<int, List<int>>(),
                dummyVar;
        private List<Operator> relaxedPlan = new List<Operator>();

        private void addNewGoalRequest(Dictionary<int, List<int>> notAchievedGoals, int variable, int value)
        {
            if (!notAchievedGoals.ContainsKey(variable))
            {
                notAchievedGoals.Add(variable, new List<int>());
                notAchievedGoals[variable].Add(value);
                return;
            }
            if (!notAchievedGoals[variable].Contains(value))
            {
                notAchievedGoals[variable].Add(value);
            }
        }

        public List<Operator> getRelaxedPlan(State state)
        {
            relaxedPlan.Clear();

            PG.computePlanningGraph(state);

            if (PG.isCutOff)
                return relaxedPlan;

            notAchievedGoals.Clear();
            notAchievedGoalsNew.Clear();
            foreach (var item in dom.goalConditions.Keys)
            {
                notAchievedGoalsNew.Add(item, new List<int>());
                notAchievedGoalsNew[item].Add(dom.goalConditions[item]);
            }
            for (int i = PG.stateLayers.Count - 1; i >= 0; i--)
            {
                dummyVar = notAchievedGoals;
                notAchievedGoals = notAchievedGoalsNew;
                notAchievedGoalsNew = dummyVar;
                    //value swapping using the third variable as a placeholder
                notAchievedGoalsNew.Clear();

                foreach (var variable in notAchievedGoals.Keys)
                {
                    foreach (var value in notAchievedGoals[variable])
                    {
                        var support = PG.getSupport(i, variable, value);
                        if (support == null)
                        {
                            addNewGoalRequest(notAchievedGoalsNew, variable, value);
                        }
                        else
                        {
                            foreach (var supp in support)
                            {
                                Operator op = PG.OpsLayers[i][supp];
                                relaxedPlan.Insert(0, op);
                                for (int precondIndex = 0; precondIndex < op.preconditionVariables.Length; precondIndex++)
                                {
                                    addNewGoalRequest(notAchievedGoalsNew, op.preconditionVariables[precondIndex], op.preconditionValues[precondIndex]);
                                }
                                foreach (var item in op.effects)
                                {
                                    for (int precondIndex = 0; precondIndex < item.conditionVariables.Length; precondIndex++)
                                    {
                                        addNewGoalRequest(notAchievedGoalsNew, item.conditionVariables[precondIndex], item.conditionValues[precondIndex]);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return relaxedPlan;
        }

        public override int getValue(State state)
        {
            heuristicCalls++;
            PG.computePlanningGraph(state);
            int result = 0;

            if (PG.isCutOff)
                return int.MaxValue;

            notAchievedGoals.Clear();
            notAchievedGoalsNew.Clear();
            foreach (var item in dom.goalConditions.Keys)
            {
                notAchievedGoalsNew.Add(item, new List<int>());
                notAchievedGoalsNew[item].Add(dom.goalConditions[item]);
            }
            for (int i = PG.stateLayers.Count-1; i >= 0; i--)
            {
                dummyVar = notAchievedGoals;
                notAchievedGoals = notAchievedGoalsNew;
                notAchievedGoalsNew = dummyVar;
                    //value swapping using the third variable as a placeholder
                notAchievedGoalsNew.Clear();

                foreach (var variable in notAchievedGoals.Keys)
                {
                    foreach (var value in notAchievedGoals[variable])
                    {
                        var support = PG.getSupport(i, variable, value);
                        if (support == null)
                        {
                            addNewGoalRequest(notAchievedGoalsNew, variable, value);
                        }
                        else
                        {
                            foreach (var supp in support)
                            {
                                result += support.Count;
                                Operator op = PG.OpsLayers[i][supp];
                                for (int precondIndex = 0; precondIndex < op.preconditionVariables.Length; precondIndex++)
			                    {
                                    addNewGoalRequest(notAchievedGoalsNew, op.preconditionVariables[precondIndex], op.preconditionValues[precondIndex]);
                                }
                                foreach (var item in op.effects)
                                {
                                    for (int precondIndex = 0; precondIndex < item.conditionVariables.Length; precondIndex++)
                                    {
                                        addNewGoalRequest(notAchievedGoalsNew, item.conditionVariables[precondIndex], item.conditionValues[precondIndex]);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        public override string getDescription()
        {
            return "Fast Forward heuristic";
        }

        public FFHeuristic(Domain d)
        {
            this.dom = d;
            //this.rbDom = Red_BlackDomain.readFromFile(d.problemName);
            //rbDom.makeAllAbstracted();
            this.PG = new PlanningGraphComputation(d);
        }
    }

    class RBHeuristic : Heuristic
    {
        KnowledgeHolder domainKnowledge;
        FFHeuristic ffHeuristic;
        private int planValue;
        List<Operator> relaxedPlan,
            unrelaxedPlan;
        ///// <summary>
        ///// R = set of currentlly achieved red values, B = set of black variables reacheable according to R
        ///// </summary>
        Dictionary<int, HashSet<int>> R, B;
        Dictionary<int, int> toAccomplish;
        State currentState;

        private void accomplish()
        {

        }

        private void unrelax()
        {
            unrelaxedPlan.Clear();
            unrelaxedPlan.Add(relaxedPlan[0]);
            currentState = Operator.apply(relaxedPlan[0], currentState);
            for (int i = 1; i < relaxedPlan.Count; i++)
            {
                toAccomplish.Clear();
                for (int j = 0; j < relaxedPlan[i].preconditionVariables.Length; j++)
                {
                    if (!dom.isAbstracted(relaxedPlan[i].preconditionVariables[j]))
                    {
                        toAccomplish.Add(relaxedPlan[i].preconditionVariables[j], relaxedPlan[i].preconditionValues[j]);
                    }
                }
                accomplish();
                unrelaxedPlan.Add(relaxedPlan[i]);
                currentState = Operator.apply(relaxedPlan[i], currentState);
            }
            toAccomplish.Clear();
            foreach (var item in dom.goalConditions.Keys)
            {
                if (!dom.isAbstracted(item))
                    toAccomplish.Add(item, dom.goalConditions[item]);
            }
            accomplish();
        }

        private void reset()
        {
            planValue = 0;
            foreach (var item in R.Values)
            {
                item.Clear();
            }

            foreach (var item in B.Values)
            {
                item.Clear();
            }
        }

        private void init(Domain d)
        {
            this.domainKnowledge = KnowledgeHolder.compute(d);
            this.ffHeuristic = new FFHeuristic(d);
            this.toAccomplish = new Dictionary<int, int>();
            //R = new Dictionary<int, HashSet<int>>();
            //B = new Dictionary<int, HashSet<int>>();
            //for (int i = 0; i < d.variablesCount; i++)
            //{
            //    if (d.isAbstracted(i))
            //    {
            //        R.Add(i, new HashSet<int>());
            //        R[i].Add(d.initialState.getValue(i));
            //    }
            //    else
            //    {
            //        B.Add(i, new HashSet<int>());
            //        B[i].Add(d.initialState.getValue(i));
            //    }
            //}
        }

        public override int getValue(State state)
        {
            reset();
            this.currentState = state;
            relaxedPlan = ffHeuristic.getRelaxedPlan(state);
            unrelax();

            return planValue;
            //TODO            
        }

        public override string getDescription()
        {
            return "Red-Black Heuristic";
        }

        public RBHeuristic(Domain d)
        {
            init(d);
        }
    }

    class WeightedHeuristic : Heuristic
    {
        private Heuristic h;
        private int weight;

        public WeightedHeuristic(Heuristic h, int weight)
        {
            this.h = h;
            this.weight = weight;
        }

        public override int getValue(State state)
        {
            return weight * h.getValue(state);
        }

        public override string getDescription()
        {
            return "weighted " + h.getDescription() + ". Weight = " + weight;
        }
    }

}
