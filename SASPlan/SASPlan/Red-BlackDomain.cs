using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SASPlan
{
    public class Red_BlackDomain : Domain
    {
        protected HashSet<int> blackVariables;

        public override bool isAbstracted(int variable)
        {
            if (blackVariables.Contains(variable))
                return false;
            return true;
        }

        public void addAbstraction(int variableToAbstract)
        {
            blackVariables.Remove(variableToAbstract);
        }

        public void makeAllAbstracted()
        {
            blackVariables.Clear();
        }

        public void makeAllNonAbstracted()
        {
            for (int i = 0; i < variablesCount; i++)
            {
                blackVariables.Add(i);
            }
        }

        public static new Red_BlackDomain readFromFile(string file)
        {
            Red_BlackDomain d = new Red_BlackDomain();
            Domain.readData(file, d);
            for (int i = 0; i < d.variablesCount; i++)
            {
                d.blackVariables.Add(i);
            }
            d.initialState = new Red_BlackState(d.initialState, d);
            return d;
        }

        public Red_BlackDomain()
        {
            this.blackVariables = new HashSet<int>();
        }

    }
}
