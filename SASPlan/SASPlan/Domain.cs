using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SASPlan
{
    public class Domain
    {
        public string problemName;
        public int variablesCount, operatorsCount;
        public bool isMetricUsed;
        public State initialState;
        public Dictionary<int, int> goalConditions;
        public List<Operator> operators;
        public int[] variablesDomainsRange;
        public OperatorDecisionTree ODT;

        public virtual bool isAbstracted(int variable)
        {
            return false;
        }

        #region read methods

        private static int lineNumber;

        private static string getNextLine(StreamReader reader)
        {
            lineNumber++;
            return reader.ReadLine();
        }

        public static Domain readFromFile(string file)
        {
            Domain d = new Domain();
            readData(file, d);
            return d;
        }

        protected static void readData(string file, Domain d)
        {
            d.problemName = file;
            using (var reader = new System.IO.StreamReader(file))
            {
                lineNumber = 0;
                readSASVersion(reader, d);
                readMetric(reader, d);
                readVariables(reader, d);
                readMutexes(reader, d);
                readInitialState(reader, d);
                readGoalState(reader, d);
                readOperators(reader, d);
                readAxioms(reader, d);
            }
        }

        private static void checkCorrectness(string fileLine, string expectedContent)
        {
            if (fileLine != expectedContent)
                throw new FormatException("\"" + expectedContent +"\" expected but \"" + fileLine + "\" found at line number " + lineNumber);
        }

        private static void readSASVersion(StreamReader reader, Domain d)
        {
            string line = getNextLine(reader);
            checkCorrectness(line, "begin_version");
            line = getNextLine(reader);
            checkCorrectness(line, "3");
            int version = int.Parse(line);
            line = getNextLine(reader);
            checkCorrectness(line, "end_version");
        }

        private static void readMetric(StreamReader reader, Domain d)
        {
            string line = getNextLine(reader);
            checkCorrectness(line, "begin_metric");
            line = getNextLine(reader);
            int metric = int.Parse(line);
            line = getNextLine(reader);
            checkCorrectness(line, "end_metric");
            if (metric == 0) d.isMetricUsed = false;
            else
            {
                if (metric == 1) d.isMetricUsed = true;
                else throw new FormatException();
            }
        }

        private static void readVariables(StreamReader reader, Domain d)
        {
            string line = getNextLine(reader);
            int variablesCount = int.Parse(line);
            d.variablesCount = variablesCount;
            d.variablesDomainsRange = new int[variablesCount];

            for (int i = 0; i < variablesCount; i++)
            {
                line = getNextLine(reader);
                checkCorrectness(line, "begin_variable");
                string name = getNextLine(reader);
                int axiomLayer = int.Parse(getNextLine(reader));
                int domainRange = int.Parse(getNextLine(reader));
                d.variablesDomainsRange[i] = domainRange;
                for (int j = 0; j < domainRange; j++)
                {
                    //reading value meanings - only skipes the lines
                    line = getNextLine(reader);
                }
                line = getNextLine(reader);
                checkCorrectness(line, "end_variable");
            }
        }

        private static void readMutexes(StreamReader reader, Domain d)
        {
            string line = getNextLine(reader);
            int mutexesCount = int.Parse(line);

            for (int i = 0; i < mutexesCount; i++)
            {
                line = getNextLine(reader);
                checkCorrectness(line, "begin_mutex_group");
                int mutexSize = int.Parse(getNextLine(reader));
                for (int j = 0; j < mutexSize; j++)
                {
                    //reading mutex values - only skipes the lines
                    line = getNextLine(reader);
                }
                line = getNextLine(reader);
                checkCorrectness(line, "end_mutex_group");
            }
        }

        private static void readInitialState(StreamReader reader, Domain d)
        {
            string line = getNextLine(reader);
            checkCorrectness(line, "begin_state");
            d.initialState = new SasState(d);
            for (int i = 0; i < d.variablesCount; i++)
            {
                line = getNextLine(reader);
                int value = int.Parse(line);
                d.initialState.setValue(i, value);
            }
            line = getNextLine(reader);
            checkCorrectness(line, "end_state");
        }

        private static void readGoalState(StreamReader reader, Domain d)
        {
            string line = getNextLine(reader);
            checkCorrectness(line, "begin_goal");
            int goalConditions = int.Parse(getNextLine(reader));

            d.goalConditions = new Dictionary<int, int>();
            for (int i = 0; i < goalConditions; i++)
            {
                line = getNextLine(reader);
                int variable = int.Parse(line.Split(' ')[0]);
                int value = int.Parse(line.Split(' ')[1]);
                d.goalConditions.Add(variable, value);
            }
            line = getNextLine(reader);
            checkCorrectness(line, "end_goal");
        }

        private static void readOperators(StreamReader reader, Domain d)
        {
            string line = getNextLine(reader);
            int operatorsCount = int.Parse(line);
            d.operatorsCount = operatorsCount;
            d.operators = new List<Operator>();

            for (int i = 0; i < operatorsCount; i++)
            {
                Operator op = new Operator();
                line = getNextLine(reader);
                checkCorrectness(line, "begin_operator");
                string name = getNextLine(reader);
                op.name = name;
                int prevailConditionsCount = int.Parse(getNextLine(reader));
                List<int> preconditionVariables = new List<int>(),
                    preconditionValues = new List<int>();

                for (int j = 0; j < prevailConditionsCount; j++)
                {
                    line = getNextLine(reader);
                    int variable = int.Parse(line.Split(' ')[0]);
                    int value = int.Parse(line.Split(' ')[1]);
                    preconditionVariables.Add(variable);
                    preconditionValues.Add(value);
                }
                int effectsCount = int.Parse(getNextLine(reader));
                List<Effect> effects = new List<Effect>();
                for (int j = 0; j < effectsCount; j++)
                {
                    line = getNextLine(reader);
                    effects.Add(Effect.readFromString(line));
                    string[] splitted = line.Split(' ');
                    int variable = int.Parse(splitted[splitted.Length - 3]);
                    int value = int.Parse(splitted[splitted.Length - 2]);
                    if (preconditionVariables.Contains(variable))
                    {
                        throw new FormatException("Conflict in operator definition (line " + lineNumber + "). Variable " + 
                            variable + " defined in both prevailing conditions an effects");
                    }
                    preconditionVariables.Add(variable);
                    preconditionValues.Add(value);
                }
                op.effects = effects.ToArray();
                op.preconditionValues = preconditionValues.ToArray();
                op.preconditionVariables = preconditionVariables.ToArray();
                int opCost = int.Parse(getNextLine(reader));
                if (d.isMetricUsed == false || opCost == 0)
                    opCost = 1;
                op.cost = opCost;

                line = getNextLine(reader);
                checkCorrectness(line, "end_operator");
                op.orderIndex = d.operators.Count;  
                d.operators.Add(op);
            }
        }
        
        private static void readAxioms(StreamReader reader, Domain d)
        {
            string line = getNextLine(reader);
            int axiomsCount = int.Parse(line);
            for (int i = 0; i < axiomsCount; i++)
            {
                line = getNextLine(reader);
                checkCorrectness(line, "begin_rule");
                int conditionsCount = int.Parse(getNextLine(reader));
                for (int j = 0; j < conditionsCount; j++)
                {
                    line = getNextLine(reader);
                    int variable = int.Parse(line.Split(' ')[0]);
                    int value = int.Parse(line.Split(' ')[1]);
                    // conditions are read but not used (so far)
                }

                line = getNextLine(reader);
                int var = int.Parse(line.Split(' ')[0]);
                int oldValue = int.Parse(line.Split(' ')[1]);
                int newValue = int.Parse(line.Split(' ')[2]);
                //effects are read but not used (so far)

                line = getNextLine(reader);
                checkCorrectness(line, "end_rule");
            }
        }

        #endregion

        public Dictionary<Operator, State> getSuccessors(State state)
        {
            if (ODT == null)
            {
                ODT = new OperatorDecisionTree();
                ODT.buildTree(this);
            }
            
            //var res = ODT.getSuccessors(state)
            return ODT.getSuccessors(state);
            
            /*
            Dictionary<Operator, State> result = new Dictionary<Operator, State>();
            foreach (var item in operators)
            {
                if (Operator.isApplicable(item, state))
                {
                    result.Add(item, Operator.apply(item, state));
                }
            }
            /*
            if (res.Keys.Count != result.Keys.Count)
            {
                throw new Exception();
            }
            
            return result;
            */
        }

        public Dictionary<Operator, State> getPredecessors(State state)
        {
            //TODO vyzkouset jestli to nebude rychlejsi pri pouziti HashSet

            Dictionary<Operator, State> result = new Dictionary<Operator, State>();

            foreach (var item in operators)
            {
                if (Operator.canBePredecessor(item, state))
                {
                    State s = Operator.applyBackwards(item, state);
                    if (!result.ContainsValue(s))
                        result.Add(item, s);
                }
            }

            return result;
        }

        /// <summary>
        /// Returns set of all applicable operators that has some effect which is not yet achived in the current state. Especially usefull for delete relaxation
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public List<Operator> getApplicableRelevantOps(State state)
        {   
            if (ODT == null)
            {
                ODT = new OperatorDecisionTree();
                ODT.buildTree(this);
            }
            //int ODTCount = ODT.getRelevantOps(state).Count;
            return ODT.getRelevantOps(state);
            /*
            
            List<Operator> result = new List<Operator>();
            foreach (var item in operators)
                if (Operator.isApplicable(item, state) && Operator.isRelevant(item, state))
                    result.Add(item);
            /*
            if (result.Count != ODTCount)
            {
                throw new Exception("getApplicableRelevantOps has a bug!!");
            }
            
            return result;
            */
        }

        public Operator getApplicableRelevantOp(State state)
        {
            foreach (var item in operators)
                if (Operator.isApplicable(item, state) && Operator.isRelevant(item, state))
                    return item;
            return null;
        }

        public bool isGoalState(State state)
        {
            foreach (var item in goalConditions.Keys)
            {
                if (!state.hasValue(item, goalConditions[item]))
                    return false;
            }
            return true;
        }

        protected Domain()
        {
        }
    }

    public class OperatorDecisionTree
    {
        private TreeNode root;
        private Domain d;

        private TreeNode buildTree(List<Operator> allOperators, List<int> remainingVariables)
        {
            if (allOperators.Count == 0)
                return TreeNodeLeaf.createEmpty();
            if (remainingVariables.Count == 0)
                return TreeNodeLeaf.create(allOperators);
            int max = remainingVariables.Max(i => d.variablesDomainsRange[i]);
            int decisionVariable = remainingVariables.First(i => d.variablesDomainsRange[i] == max);
            List<Operator>[] opsByPreconditions = new List<Operator>[d.variablesDomainsRange[decisionVariable]];
            List<Operator> remaining = new List<Operator>();
            for (int i = 0; i < opsByPreconditions.Length; i++)
                opsByPreconditions[i] = new List<Operator>();
            
            foreach (var item in allOperators)
            {
                if (item.preconditionVariables.Contains(decisionVariable))
                {
                    int precondValue = item.preconditionValues[Array.IndexOf<int>(item.preconditionVariables, decisionVariable)];
                    if (precondValue != -1)
                        opsByPreconditions[precondValue].Add(item);
                    else
                        remaining.Add(item);
                }
                else
                    remaining.Add(item);
            }
            if (remaining.Count == allOperators.Count)
            {
                remainingVariables.Remove(decisionVariable);
                var res = buildTree(allOperators, remainingVariables);
                remainingVariables.Add(decisionVariable);
                return res;
            }
            remainingVariables.Remove(decisionVariable);
            TreeNode node = new TreeNode();
            node.variableIndex = decisionVariable;
            node.successorsByValues = new TreeNode[d.variablesDomainsRange[decisionVariable]];
            for (int i = 0; i < d.variablesDomainsRange[decisionVariable]; i++)
            {
                node.successorsByValues[i] = buildTree(opsByPreconditions[i], remainingVariables);
            }
            node.dontCareSuccessor = buildTree(remaining, remainingVariables);
            remainingVariables.Add(decisionVariable);
            return node;
        }

        public void buildTree(Domain d)
        {
            this.d = d;
            List<int> remainingVars = new List<int>();
            for (int i = 0; i < d.variablesCount; i++) 
                remainingVars.Add(i);
            root = buildTree(d.operators, remainingVars);
        }

        public Dictionary<Operator, State> getSuccessors(State state)
        {
            Dictionary<Operator, State> result = new Dictionary<Operator, State>();
            root.getApplicableOperators(state, result);
            return result;
        }

        public List<Operator> getRelevantOps(State state)
        {
            List<Operator> result = new List<Operator>();
            root.getRelevantOperators(state, result);
            return result;
        }

        private class TreeNode
        {
            public int variableIndex;
            public TreeNode[] successorsByValues;
            public TreeNode dontCareSuccessor;

            public virtual void getApplicableOperators(State s, Dictionary<Operator, State> result)
            {
                successorsByValues[s.getValue(variableIndex)].getApplicableOperators(s, result);
                dontCareSuccessor.getApplicableOperators(s, result);
            }

            public virtual void getRelevantOperators(State s, List<Operator> result)
            {
                foreach (var item in s.getAllValues(variableIndex))
                {
                    successorsByValues[item].getRelevantOperators(s, result);
                }
                dontCareSuccessor.getRelevantOperators(s, result);
            }
        }

        private class TreeNodeLeaf : TreeNode
        {
            List<Operator> operators;
            public override void getApplicableOperators(State s, Dictionary<Operator, State> result)
            {
                foreach (var item in operators)
                {
                    result.Add(item, Operator.apply(item, s));
                }
            }

            public override void getRelevantOperators(State s, List<Operator> result)
            {
                foreach (var item in operators)
                {
                    if (Operator.isRelevant(item, s))
                        result.Add(item);
                }
            }

            public static TreeNodeLeaf createEmpty()
            {
                TreeNodeLeaf result = new TreeNodeLeaf();
                result.operators = new List<Operator>();
                return result;
            }

            public static TreeNodeLeaf create(List<Operator> ops)
            {
                TreeNodeLeaf result = new TreeNodeLeaf();
                result.operators = new List<Operator>();
                foreach (var item in ops)
                {
                    result.operators.Add(item);
                }
                return result;
            }
        }
    }
}
