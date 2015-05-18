using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SASPlan
{
    public class Operator
    {
        public string name;
        public int[] preconditionVariables, preconditionValues;
        public Effect[] effects;
        public int cost;
        /// <summary>
        /// Index of this operator in the list of all operators (Domain.operators)
        /// </summary>
        public int orderIndex;

        public static bool isRelevant(Operator o, State state)
        {
            foreach (var item in o.effects)
            {
                if (!state.hasValue(item.effectVariable, item.effectValue))
                    return true;
            }
            return false;
        }

        public static bool isApplicable(Operator o, State state)
        {
            for (int i = 0; i < o.preconditionVariables.Length; i++)
            {
                if (!state.hasValue(o.preconditionVariables[i], o.preconditionValues[i]))
                    return false;
            }
            return true;
        }

        public static bool canBePredecessor(Operator o, State s)
        {
            return o.effects.Any(e => s.hasValue(e.effectVariable, e.effectValue));
            /*
            for (int i = 0; i < o.effects.Length; i++)
            {
                Effect e = o.effects[i];
            }
            */
        }

        /// <summary>
        /// This should only be called when s can be result of application of o (i. e. if o is backward applicable). Backward applicability is not checked here
        /// </summary>
        /// <param name="o"></param>
        /// <param name="s"></param>
        /// <param name="notAbstractedVariables"></param>
        /// <returns></returns>
        public static State applyBackwards(Operator o, State state)
        {
            State result = state.clone();

            //TODO only works if operators doesn't have effect conditions
            for (int i = 0; i < o.preconditionVariables.Length; i++)
            {
                result.setValue(o.preconditionVariables[i], o.preconditionValues[i]);
            }
            return result;
        }

        /// <summary>
        /// Apply can only be called when the operator is applicable! Applicability is NOT checked here.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public static State apply(Operator o, State state)
        {
            State result = state.clone();
            foreach (var item in o.effects)
            {
                applyEffect(result, item);
            }
            return result;
        }

        private static bool isApplicable(State state, Effect e)
        {
            for (int i = 0; i < e.conditionVariables.Length; i++)
            {
                if (!state.hasValue(e.conditionVariables[i], e.conditionValues[i]))
                    return false;
            }
            return true;
        }

        private static void applyEffect(State state, Effect e)
        {
            if (isApplicable(state, e))
            {
                state.setValue(e.effectVariable, e.effectValue);
            }
        }

    }

    public class Effect
    {
        public int[] conditionVariables, conditionValues;
        public int effectVariable, effectValue;

        public static Effect readFromString(string s)
        {
            Effect result = new Effect();
            string[] splitted = s.Split(' ');
            int numberOfConditions = int.Parse(splitted[0]);
            result.conditionValues= new int[numberOfConditions];
            result.conditionVariables= new int[numberOfConditions];
            
            int i = 0;
            for (i = 0; i < numberOfConditions; i++)
			{
                int variable = int.Parse(splitted[2 * i + 1]);
                int value = int.Parse(splitted[2 * i + 2]);
                result.conditionVariables[i] = variable;
                result.conditionValues[i] = value;
			}

            int effectVar = int.Parse(splitted[2 * i + 1]),
                effectVal = int.Parse(splitted[2 * i + 3]);
            result.effectVariable = effectVar;
            result.effectValue = effectVal;

            return result;
        }

        public override string ToString()
        {
            string result = "";
            for (int i = 0; i < conditionVariables.Length; i++)
            {
                result += conditionVariables[i] + "=" + conditionValues[i] + ",";
            }
            return result += " -> " + effectVariable + ":=" + effectValue;
        }

        /// <summary>
        /// Only called for the Domain Transition Graph edge labels
        /// </summary>
        /// <returns></returns>
        public string ToEdgeLabel()
        {
            string result = "";
            for (int i = 0; i < conditionVariables.Length; i++)
            {
                result += conditionVariables[i] + "=" + conditionValues[i] + ",";
            }
            return result;
        }
    }
}
