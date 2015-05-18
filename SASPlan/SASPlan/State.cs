using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SASPlan
{
    public abstract class State
    {
        protected Domain dom;
        public abstract bool hasValue(int variable, int value);
        public abstract void setValue(int variable, int value);
        public abstract int getValue(int variable);
        public abstract List<int> getAllValues(int variable);
        public abstract int[]  getAllValues();
        internal abstract State clone();
    }

    public class SasState : State
    {
        protected int[] stateValues;
        public override bool hasValue(int variable, int value)
        {
            return value == -1 || stateValues[variable] == value;
        }

        public override void setValue(int variable, int value)
        {
            stateValues[variable] = value;
        }

        public override int getValue(int variable)
        {
            return stateValues[variable];
        }

        public SasState(Domain dom)
        {
            this.dom = dom;
            this.stateValues = new int[dom.variablesCount];
        }

        internal override State clone()
        {
            SasState result = new SasState(this.dom);
            for (int i = 0; i < this.stateValues.Length; i++)
            {
                result.stateValues[i] = this.stateValues[i];
            }
            return result;
        }

        public override int GetHashCode()
        {
            return ArrayEqualityComparer.comparer.GetHashCode(stateValues);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is SasState))
                return false;
            SasState s = (SasState)obj;
            return ArrayEqualityComparer.comparer.Equals(this.stateValues, s.stateValues);
        }

        public override string ToString()
        {
            string result = "";
            result += "[";
            for (int i = 0; i < stateValues.Length; i++)
                result += (stateValues[i] + " ");
            result += "]";
            return result;
        }

        public SasState()
        {

        }


        public override int[] getAllValues()
        {
            return stateValues;
        }

        public override List<int> getAllValues(int variable)
        {
            return new List<int> { stateValues[variable] };
        }
    }

    public class Red_BlackState : State
    {
        private List<int>[] stateValues;

        public int size()
        {
            int result = 0;
            foreach (var item in stateValues)
            {
                result += item.Count;
            }
            return result;
        }

        public override bool hasValue(int variable, int value)
        {
            return value == -1 || stateValues[variable].Contains(value);
        }

        public override void setValue(int variable, int value)
        {
            if (dom.isAbstracted(variable))
            {
                if (!stateValues[variable].Contains(value))
                    stateValues[variable].Add(value);
            }
            else stateValues[variable][0] = value;
        }

        public override int getValue(int variable)
        {
            return stateValues[variable][0];
        }

        public Red_BlackState(Domain dom)
        {
            this.dom = dom;
            this.stateValues = new List<int>[dom.variablesCount];
        }

        public Red_BlackState(State s, Domain dom)
        {
            this.dom = dom;
            this.stateValues = new List<int>[dom.variablesCount];
            for (int i = 0; i < dom.variablesCount; i++)
            {
                stateValues[i] = new List<int>();
                stateValues[i].Add(s.getValue(i));
            }
        }

        internal override State clone()
        {
            Red_BlackState result = new Red_BlackState(this.dom);
            for (int i = 0; i < this.stateValues.Length; i++)
                result.stateValues[i] = new List<int>(this.stateValues[i]);
            return result;
        }

        public override int GetHashCode()
        {
            return ListArrayEqualityComparer.comparer.GetHashCode(stateValues);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Red_BlackState))
                return false;
            Red_BlackState s = (Red_BlackState)obj;
            return ListArrayEqualityComparer.comparer.Equals(this.stateValues, s.stateValues);
        }

        public override string ToString()
        {
            string result = "";
            result += "[";
            for (int i = 0; i < stateValues.Length; i++)
            {
                result += "{";
                for (int j = 0; j < stateValues[i].Count; j++)
                {
                    result += (stateValues[i][j] + " ");
                }
                result += "}, ";
            }
            result += "]";
            return result;

        }

        public override int[] getAllValues()
        {
            return null;
        }

        public override List<int> getAllValues(int variable)
        {
            return stateValues[variable];
        }
    }

    /// <summary>
    /// Only allows for one abstraction to be worked with at any time. I.e. there cannot be two instances of AbstractState that would have different sets of not abstracted variables.
    /// </summary>
    public class AbstractState : SasState
    {
        public static Dictionary<int, int> notAbstractedVariablesIndices;
        public static void setNotAbstractedVariables(HashSet<int> variables)
        {
            AbstractState.notAbstractedVariablesIndices = new Dictionary<int, int>();
            int i = 0;
            foreach (var item in variables)
            {
                AbstractState.notAbstractedVariablesIndices.Add(item, i);
                i++;
            }
        }

        private bool isAbstracted(int variable)
        {
            return !AbstractState.notAbstractedVariablesIndices.ContainsKey(variable);
        }

        public override bool hasValue(int variable, int value)
        {
            if (isAbstracted(variable))
                return true;
            return base.hasValue(AbstractState.notAbstractedVariablesIndices[variable], value);
        }

        public override void setValue(int variable, int value)
        {
            if (isAbstracted(variable))
                return;
            base.setValue(AbstractState.notAbstractedVariablesIndices[variable], value);
        }

        public override int getValue(int variable)
        {
            if (isAbstracted(variable))
                return -1;
            return stateValues[notAbstractedVariablesIndices[variable]];
        }

        public override bool Equals(object obj)
        {
            if (!(obj is AbstractState))
                return false;
            AbstractState s = (AbstractState)obj;
            return ArrayEqualityComparer.comparer.Equals(this.stateValues, s.stateValues);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public AbstractState(Domain dom)
        {
            this.dom = dom;
            this.stateValues = new int[AbstractState.notAbstractedVariablesIndices.Keys.Count];
        }

        internal override State clone()
        {
            AbstractState result = new AbstractState(this.dom);
            result.stateValues = new int[this.stateValues.Length];
            Array.Copy(this.stateValues, result.stateValues, this.stateValues.Length);

            return result;
        }
    
    }

}
