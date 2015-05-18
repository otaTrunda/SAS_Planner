using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SASPlan
{
    class KnowledgeExtraction
    {
        public static CausualGraph computeCausualGraph(Domain dom)
        {
            CausualGraph result = new CausualGraph();
            result.vertices = new List<int>(dom.variablesCount);
            result.isEdge = new bool[dom.variablesCount, dom.variablesCount];
            for (int i = 0; i < dom.variablesCount; i++)
            {
                result.vertices.Add(i);
            }

            foreach (var item in dom.operators)
            {
                foreach (var precondVar in item.preconditionVariables)
                {
                    foreach (var eff in item.effects)
                    {
                        if (eff.effectVariable != precondVar)
                            result.setEdge(precondVar, eff.effectVariable);
                    }
                }
                foreach (var eff in item.effects)
                {
                    foreach (var precondVar in eff.conditionVariables)
                    {
                        if (eff.effectVariable != precondVar)
                            result.setEdge(precondVar, eff.effectVariable);
                    }
                }
                foreach (var eff in item.effects)
                {
                    foreach (var eff2 in item.effects)
                    {
                        if (eff.effectVariable != eff2.effectVariable)
                            result.setEdge(eff.effectVariable, eff2.effectVariable);
                    }
                }
            }

            return result;
        }

        public static DomainTransitionGraph computeDTG(Domain dom, int variable)
        {
            DomainTransitionGraph result = new DomainTransitionGraph();
            result.variable = variable;
            result.vertices = new List<int>();
            result.edges = new List<GraphEdge>();

            for (int i = 0; i < dom.variablesDomainsRange[variable]; i++)
            {
                result.vertices.Add(i);
            }
            foreach (var item in dom.operators)
            {
                foreach (var eff in item.effects)
                {
                    if (eff.effectVariable == variable)
                    {
                        int targetValue = eff.effectValue,
                            originalValue = -1;
                        Effect outsideEffect = new Effect(),
                            outsideCondition = new Effect();
                        List<int> outsideConditionVariables = new List<int>(),
                            outsideConditionValues = new List<int>(),
                            outsideEffectVariables = new List<int>(),
                            outsideEffectValues = new List<int>();

                        foreach (var eff1 in item.effects)
                        {
                            if (eff1.effectVariable != eff.effectVariable)
                            {
                                outsideEffectVariables.Add(eff1.effectVariable);
                                outsideEffectValues.Add(eff1.effectValue);
                            }
                        }
                        outsideEffect.conditionVariables = outsideEffectVariables.ToArray();
                        outsideEffect.conditionValues = outsideEffectValues.ToArray();

                        for (int i = 0; i < eff.conditionVariables.Length; i++)
                        {
                            if (eff.conditionVariables[i] == variable)
                                originalValue = eff.conditionValues[i];
                            else
                            {
                                outsideConditionVariables.Add(eff.conditionVariables[i]);
                                outsideConditionValues.Add(eff.conditionValues[i]);
                            }
                        }
                        for (int i = 0; i < item.preconditionVariables.Length; i++)
                        {
                            if (item.preconditionVariables[i] == variable)
                                originalValue = item.preconditionValues[i];
                            else
                            {
                                outsideConditionVariables.Add(item.preconditionVariables[i]);
                                outsideConditionValues.Add(item.preconditionValues[i]);
                            }
                        }
                        outsideCondition.conditionVariables = outsideConditionVariables.ToArray();
                        outsideCondition.conditionValues = outsideConditionValues.ToArray();

                        if (originalValue != -1)
                        {
                            GraphEdge e = new GraphEdge();
                            e.from = originalValue;
                            e.to = targetValue;
                            e.outsideCondition = outsideCondition;
                            e.outsideEffect = outsideEffect;
                            e.op = item;
                            result.edges.Add(e);
                        }
                        else
                        {
                            foreach (var val in result.vertices)
                            {
                                if (val == targetValue)
                                    continue;
                                GraphEdge e = new GraphEdge();
                                e.from = val;
                                e.to = targetValue;
                                e.outsideCondition = outsideCondition;
                                e.outsideEffect = outsideEffect;
                                e.op = item;
                                result.edges.Add(e);
                            }
                        }
                    }
                }
            }
            result.computeRSE_Invertibility();
            return result;
        }    
    }

    public class KnowledgeHolder
    {
        public CausualGraph CG;
        public List<DomainTransitionGraph> DTGs;
        public HashSet<int> RSE_InvertibleVariables;

        public static KnowledgeHolder compute(Domain dom)
        {
            KnowledgeHolder result = new KnowledgeHolder();
            result.CG = KnowledgeExtraction.computeCausualGraph(dom);
            result.DTGs = new List<DomainTransitionGraph>();
            result.RSE_InvertibleVariables = new HashSet<int>();
            for (int i = 0; i < dom.variablesCount; i++)
            {
                result.DTGs.Add(KnowledgeExtraction.computeDTG(dom, i));
                if (result.DTGs[result.DTGs.Count - 1].isRSE_Invertible)
                    result.RSE_InvertibleVariables.Add(i);
            }
            return result;
        }

        public void show(int variable, System.Windows.Forms.Panel panel)
        {
            if (variable == 0)
            {
                CG.visualize(panel, RSE_InvertibleVariables);
                return;
            }
            if (variable <= CG.vertices.Count)
            {
                DTGs[variable - 1].visualize(true, panel);
                return;
            }
            DTGs[variable - CG.vertices.Count - 1].visualize(false, panel);
        }

        public void visualize()
        {
            KnowledgeVisualizerForm f = new KnowledgeVisualizerForm();
            f.listView1.Items.Add("Causual Graph");
            for (int i = 0; i < CG.vertices.Count; i++)
            {
                f.listView1.Items.Add("DTG var" + i.ToString());
            }
            for (int i = 0; i < CG.vertices.Count; i++)
            {
                f.listView1.Items.Add("DTG NoLabel var" + i.ToString());
            }
            f.h = this;
            System.Windows.Forms.Application.Run(f);
        }
    }

    public class CausualGraph
    {
        public List<int> vertices;
        public bool[,] isEdge;
        private bool hasSomeEdge = false;

        public void setEdge(int from, int to)
        {
            isEdge[from, to] = true;
            hasSomeEdge = true;
        }

        public void visualize(System.Windows.Forms.Panel panel = null, HashSet<int> invertibleVariables = null)
        {
            Microsoft.Glee.Drawing.Graph g = new Microsoft.Glee.Drawing.Graph("Causual Graph");
            foreach (var item in vertices)
            {
                g.AddNode(item.ToString());
                if (invertibleVariables != null && !invertibleVariables.Contains(item))
                {
                    ((Microsoft.Glee.Drawing.Node)g.NodeMap[item.ToString()]).Attr.Color = Microsoft.Glee.Drawing.Color.Red;
                    //g.SelectedNodeAttribute.Color = Microsoft.Glee.Drawing.Color.Red;
                }
            }
            for (int i = 0; i < isEdge.GetLength(0); i++)
                for (int j = 0; j < isEdge.GetLength(1); j++)
                    if (isEdge[i, j]) g.AddEdge(i.ToString(), j.ToString());                

            Microsoft.Glee.GraphViewerGdi.GViewer viewer = new Microsoft.Glee.GraphViewerGdi.GViewer();
            viewer.Graph = g;
            if (panel == null)
            {
                KnowledgeVisualizerForm form = new KnowledgeVisualizerForm();
                form.SuspendLayout();
                viewer.Dock = System.Windows.Forms.DockStyle.Fill;
                form.Controls.Add(viewer);
                form.ResumeLayout();
                System.Windows.Forms.Application.Run(form);
            }
            else
            {
                viewer.Dock = System.Windows.Forms.DockStyle.Fill;
                panel.Controls.Clear();
                panel.Controls.Add(viewer);
            }
        }

        public CausualGraph projection(HashSet<int> variables)
        {
            CausualGraph result = new CausualGraph();
            result.vertices = new List<int>();
            int max = 0;
            foreach (var item in this.vertices)
            {
                if (variables.Contains(item))
                {
                    result.vertices.Add(item);
                    if (max < item) 
                        max = item;
                }
            }
            result.isEdge = new bool[max, max];
            for (int i = 0; i < max; i++)
            {
                for (int j = 0; j < max; j++)
                {
                    if (this.isEdge[i, j]) 
                        result.setEdge(i, j);
                }
            }
            return result;
        }

        private class acyclicityChecker
        {
            // 0 = not visited, 1 = pending, 2 = closed
            Dictionary<int, int> visited = new Dictionary<int, int>(),
                enterTime = new Dictionary<int, int>(),
                exitTime = new Dictionary<int, int>();
            
            int time = 0;
            CausualGraph gr;
            bool hasCycle = false;

            private void doDFS(CausualGraph g)
            {
                hasCycle = false;
                visited.Clear();
                enterTime.Clear();
                exitTime.Clear();
                gr = g;
                foreach (var item in g.vertices)
                    visited.Add(item, 0);
                time = 0;
                foreach (var item in g.vertices)
                {
                    if (visited[item] == 0)
                        visit(item);
                }
            }

            public bool isAcyclic(CausualGraph g)
            {
                doDFS(g);
                return !hasCycle;
            }

            private void visit(int vertex)
            {
                visited[vertex] = 1;
                time++;
                enterTime[vertex] = time;
                foreach (var successor in gr.vertices)
                {
                    if (gr.isEdge[vertex, successor])
                    {
                        if (visited[successor] == 0)
                        {
                            visit(successor);
                        }
                        else
                        {
                            if (visited[successor] == 1)
                            {
                                hasCycle = true;
                            }
                        }

                    }
                }
                visited[vertex] = 3;
                time++;
                exitTime[vertex] = time;
            }
        }

    //    for i:=1 to n do barva[i]:=bílá;
    //čas:=0;
    //for i:=1 to n do if barva[i]=bílá then NAVŠTIV(i)

    //    NAVŠTIV(i) 
    //begin	barva[i]:=šedá; čas:=čas+1; d[i]:=čas;
    //for each j je soused i do 
    //if barva[j]=bílá 
    //    then 	begin	NAVŠTIV(j);
    //            označ (i,j) jako stromovou
    //        end
    //    else if barva[j]=šedá 
    //        then 	begin 	ohlas nalezení cyklu;
    //                označ (i,j) jako zpětnou
    //            end
    //        else if d[i] < d[j] 
    //            then označ (i,j) jako dopřednou
    //            else označ (i,j) jako příčnou
    //barva[i]:=černá; čas:=čas+1; f[i]:=čas
    //end;

        public bool isAcyclic()
        {
            acyclicityChecker checker = new acyclicityChecker();
            return checker.isAcyclic(this);
        }

        public List<int> topologicalOrder()
        {
            if (isEmpty())
            {
                return vertices;
            }
            else throw new Exception("Causual graph is not empty");
        }

        public bool isEmpty()
        {
            return !hasSomeEdge;
        }
    }

    public class DomainTransitionGraph
    {
        public int variable;
        public List<int> vertices;
        public List<GraphEdge> edges;
        
        private List<GraphEdge>[] edgesByVertices;
        private bool isTransformed = false;

        private void transformToSuccesorsLists()
        {
            this.edgesByVertices = new List<GraphEdge>[vertices.Count];
            foreach (var item in edges)
            {
                if (edgesByVertices[item.from] == null)
                    edgesByVertices[item.from] = new List<GraphEdge>();
                edgesByVertices[item.from].Add(item);
            }
            isTransformed = true;
        }
        
        public bool isRSE_Invertible = false;

        public void computeRSE_Invertibility()
        {
            isRSE_Invertible = true;
            foreach (var item in edges)
            {
                if (!isEdgeRSE_Invertible(item))
                {
                    isRSE_Invertible = false;
                    return;
                }
            }
        }

        private bool isEdgeRSE_Invertible(GraphEdge e)
        {
            if (e.isInvertibilityComputed)
                return e.isRSE_Invertible;
            e.isInvertibilityComputed =true;
            bool isJ_thConditionMet = false;
            foreach (var item in edges)
            {
                if (item.from != e.to || item.to != e.from)
                    continue;
                if (item.outsideCondition.conditionVariables.Length == 0)
                {
                    e.isRSE_Invertible = true;
                    return true;
                }
                for (int j = 0; j < item.outsideCondition.conditionVariables.Length; j++)
			    {
                    isJ_thConditionMet = false;
                    for (int i = 0; i < e.outsideCondition.conditionVariables.Length; i++)
                    {
                        if (e.outsideCondition.conditionVariables[i] == item.outsideCondition.conditionVariables[j] &&
                            e.outsideCondition.conditionValues[i] == item.outsideCondition.conditionValues[j])
                        {
                            isJ_thConditionMet = true;
                            break;
                        }
                    }
                    if (isJ_thConditionMet)
                        continue;
                    for (int i = 0; i < e.outsideEffect.conditionVariables.Length; i++)
                    {
                        if (e.outsideEffect.conditionVariables[i] == item.outsideCondition.conditionVariables[j] &&
                            e.outsideEffect.conditionValues[i] == item.outsideCondition.conditionValues[j])
                        {
                            isJ_thConditionMet = true;
                            break;
                        }
                    }
                    if (!isJ_thConditionMet)
                        break;
                }
                if (isJ_thConditionMet)
                {
                    e.isRSE_Invertible = true;
                    return true;
                }
            }
            e.isRSE_Invertible = false;
            return false;
        }

        public void visualize(bool isLabeled = true, System.Windows.Forms.Panel panel = null)
        {
            Microsoft.Glee.Drawing.Graph g = new Microsoft.Glee.Drawing.Graph("Domain Transition Graph of variable " + variable);
            foreach (var item in vertices)
            {
                g.AddNode(item.ToString());
            }
            if (isLabeled)
                foreach (var item in edges)
                {
                    g.AddEdge(item.from.ToString(), "Cond:" + item.outsideCondition.ToEdgeLabel() + "Eff:" + item.outsideEffect.ToEdgeLabel(), item.to.ToString());
                    if (!isEdgeRSE_Invertible(item))
                    {
                        g.Edges[g.EdgeCount - 1].Attr.Color = Microsoft.Glee.Drawing.Color.Red;
                    }
                }
            else
            {
                bool[,] isEdge = new bool[vertices.Count, vertices.Count];
                foreach (var item in edges)
                {
                    if (isEdge[item.from, item.to])
                        continue;
                    g.AddEdge(item.from.ToString(), item.to.ToString());
                    if (!isEdgeRSE_Invertible(item))
                    {
                        g.Edges[g.EdgeCount - 1].Attr.Color = Microsoft.Glee.Drawing.Color.Red;
                    }
                    isEdge[item.from, item.to] = true;
                }
            }

            Microsoft.Glee.GraphViewerGdi.GViewer viewer = new Microsoft.Glee.GraphViewerGdi.GViewer();
            viewer.Graph = g;
            if (panel == null)
            {
                KnowledgeVisualizerForm form = new KnowledgeVisualizerForm();
                form.SuspendLayout();
                viewer.Dock = System.Windows.Forms.DockStyle.Fill;
                form.Controls.Add(viewer);
                form.ResumeLayout();
                System.Windows.Forms.Application.Run(form);
            }
            else
            {
                viewer.Dock = System.Windows.Forms.DockStyle.Fill;
                panel.Controls.Clear();
                panel.Controls.Add(viewer);
            }
        }

        /// <summary>
        /// Finds a path in DTG from given value to another value. Should be called only for black variables, and no other black variables should occur
        /// in the outside conditions. The red variables may occur and an edge is accessible only if the outisde condition is met by given RedValues.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="redValues"></param>
        /// <returns></returns>
        public List<Operator> findPath(int from, int to, Dictionary<int, HashSet<int>> redValues)
        {
            if (!isTransformed)
                transformToSuccesorsLists();

            HashSet<int> visited = new HashSet<int>();
            IHeap<int, int> nodes = new BinomialHeap<int>();
            int[] lengths = new int[vertices.Count], previous = new int[vertices.Count];
            Operator[] previousOperator = new Operator[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
            {
                lengths[i] = int.MaxValue;
            }

            lengths[from] = 0;
            nodes.insert(0, from);
            visited.Add(from);
            while (nodes.size() > 0)
            {
                int current = nodes.removeMin();
                if (current == to)
                    break;
                foreach (var item in edgesByVertices[current])
                {
                    if (isOutsideConditionMet(item.outsideCondition, redValues))
                    {
                        int succesor = item.to;
                        int newLength = lengths[current] + item.op.cost;
                        if (newLength < lengths[succesor])
                        {
                            lengths[succesor] = newLength;
                            previous[succesor] = current;
                            previousOperator[succesor] = item.op;
                            if (visited.Contains(succesor))
                            {
                                nodes.insert(lengths[succesor], succesor);
                            }
                        }
                        if (!visited.Contains(succesor))
                        {
                            visited.Add(succesor);
                            nodes.insert(lengths[succesor], succesor);
                        }
                    }
                }
            }

            List<Operator> result = new List<Operator>();

            int currentVal = to;
            while (currentVal != from)
            {
                result.Insert(0, previousOperator[currentVal]);
                currentVal = previous[currentVal];
            }

            return result;
        }

        private bool isOutsideConditionMet(Effect condition, Dictionary<int, HashSet<int>> redValues)
        {
            for (int i = 0; i < condition.conditionVariables.Length; i++)
            {
                if (!redValues.ContainsKey(condition.conditionVariables[i]))
                {
                    throw new Exception("Outside condition contains a black variable.");
                }
                if (!redValues[condition.conditionVariables[i]].Contains(condition.conditionValues[i]))
                    return false;
            }
            return true;
        }
    
    }

    public class GraphEdge
    {
        //both outsideCondition and outside effect are in a form of two arrays - int he first are the variables and in the second are appropriate values. 
        //These are stored in Effect class in the 
        //atributes conditionVariables and conditionValues. (The Effect.effectVariable and Effect.effecValue are NOT used here, and shouldn't be accessed)
        public Effect outsideCondition, outsideEffect;
        public int from, to;
        public bool isRSE_Invertible, isInvertibilityComputed;
        public Operator op;
    }
}
