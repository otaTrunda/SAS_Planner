using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SASPlan
{
    struct StateInformation
    {
        public int gValue;
        public bool isClosed; 

        public StateInformation(int val = 0)
        {
            this.gValue = val;
            this.isClosed = false;
        }
    }

    class AStarSearch : HeuristicSearchEngine
    {
        protected IHeap<int, State> openNodes;
        protected Dictionary<State, StateInformation> gValues;
        protected Dictionary<State, State> predecessor;

        protected const long memoryLimit = 5000000;

        protected void addToClosedList(State state)
        {
            StateInformation f = gValues[state];
            f.isClosed = true;
            gValues[state] = f;
        }

        protected void addToOpenList(State s, int gValue, State pred, int hValue)
        {
            if (!gValues.ContainsKey(s))
            {
                gValues.Add(s, new StateInformation(gValue));
                predecessor.Add(s, pred);
                openNodes.insert(gValue + hValue, s);
                return;
            }
            if (gValues[s].gValue > gValue)
            {
                StateInformation f = gValues[s];
                f.gValue = gValue;
                gValues[s] = f;
                predecessor[s] = pred;
                openNodes.insert(gValue + hValue, s);
                return;
            }
        }

        public override int search(bool quiet = false)
        {
            predecessor = new Dictionary<State, State>();
            printMessage("search started. Algorithm: A*, problem: " + dom.problemName + ", " + h.ToString(), quiet);
            DateTime start = DateTime.Now;
            openNodes.insert(0, dom.initialState);
            gValues.Add(dom.initialState, new StateInformation());
            predecessor.Add(dom.initialState, null);
            int steps = -1;
            while (openNodes.size() > 0)
            {
                steps++;
                if (steps % 100000 == 0)
                {
                    printMessage("Expanded nodes: " + (gValues.Count - openNodes.size()) +
                        "\tOpen nodes: " + openNodes.size() + "\tVisited nodes: " + gValues.Count +
                        "\tHeuristic calls: " + h.heuristicCalls, quiet);
                    if (gValues.Count > memoryLimit)
                    {
                        printMessage("Search FAILED - memory limit exceeded.", quiet);
                        DateTime end = DateTime.Now;
                        printMessage("search ended in " + (end - start).TotalSeconds + " seconds", quiet);
                        break;
                    }
                }
                State currentState = openNodes.removeMin();
                if (gValues[currentState].isClosed)
                    continue;
                addToClosedList(currentState);
                if (dom.isGoalState(currentState))
                {
                    DateTime end = DateTime.Now;
                    int GVAL = gValues[currentState].gValue;
                    printMessage("search ended in " + (end - start).TotalSeconds + " seconds", quiet);
                    printMessage("Expanded nodes: " + (gValues.Count - openNodes.size()) + ", plan length " + GVAL , quiet);
                    this.solution = extractSolution(currentState);
                    return GVAL;
                }
                int currentGValue = gValues[currentState].gValue;
                Dictionary<Operator, State> successors = dom.getSuccessors(currentState);
                foreach (var item in successors.Keys)
                {
                    State state = successors[item];
                    int gVal = currentGValue + item.cost;
                    int hVal = h.getValue(state);
                    addToOpenList(state, gVal, currentState, hVal);

                }
            }
            printMessage("No solution exists.", quiet);
            return -1;
        }

        protected List<int> extractSolution(State state)
        {
            List<int> result = new List<int>();
            State current = state;
            while(current != null)
            {
                State pred = predecessor[current];
                if (pred == null)
                    break;
                var ss = dom.getSuccessors(pred);
                foreach (var item in ss.Keys)
                {
                    if (ss[item].Equals(current))
                    {
                        result.Insert(0, item.orderIndex);
                        break;
                    }
                }
                current = pred;
            }
            return result;
        }

        public AStarSearch(Domain d, Heuristic h)
        {
            this.dom = d;
            this.h = h;
            this.gValues = new Dictionary<State, StateInformation>();
            //this.openNodes = new LeftistHeap<State>();
            //this.openNodes = new RegularHeap<State>();
            //this.openNodes = new BinomialHeap<State>();
            //this.openNodes = new SingleBucket<State>(200*h.getValue(d.initialState));
            this.openNodes = new OrderedBagHeap<State>();
            this.openNodes = new OrderedMutliDictionaryHeap<State>();
        }   
    }

    class BeamStackSearch : AStarSearch
    {
        private int maxWidth;
        private LinkedList<State> bestSuccessors; 
        private LinkedList<int> best_hValues,
            bestOperatorsCosts; 

        private void selectBestSuccessors(Dictionary<Operator, State> successors)
        {
            bestSuccessors.Clear();
            best_hValues.Clear();
            bestOperatorsCosts.Clear();
            foreach (var item in successors.Keys)
            {
                State state = successors[item];
                if (bestSuccessors.Count >= maxWidth && bestOperatorsCosts.Last.Value + best_hValues.Last.Value < item.cost)
                    continue;
                
                int hVal = h.getValue(state);
                if (bestSuccessors.Count < maxWidth || bestOperatorsCosts.Last.Value + best_hValues.Last.Value > item.cost + hVal)
                    addSuccessorCandidate(state, item.cost, hVal);
            }
        }

        private void addSuccessorCandidate(State state, int cost, int hVal)
        {
            if (bestSuccessors.Count == 0)
            {
                bestSuccessors.AddFirst(state);
                best_hValues.AddFirst(hVal);
                bestOperatorsCosts.AddFirst(cost);
                return;
            }
            if (best_hValues.Last.Value + bestOperatorsCosts.Last.Value < cost + hVal)
            {
                bestSuccessors.AddLast(state);
                best_hValues.AddLast(hVal);
                bestOperatorsCosts.AddLast(cost);
                return;
            }
            
            LinkedListNode<int> hValIterator = best_hValues.Last;
            LinkedListNode<int> opIterator = bestOperatorsCosts.Last;
            LinkedListNode<State> stateIterator = bestSuccessors.Last;
            while (hValIterator.Previous != null && opIterator.Previous.Value + hValIterator.Previous.Value > cost + hVal)
            {
                hValIterator = hValIterator.Previous;
                opIterator = opIterator.Previous;
                stateIterator = stateIterator.Previous;
            }
            bestSuccessors.AddBefore(stateIterator, state);
            best_hValues.AddBefore(hValIterator, hVal);
            bestOperatorsCosts.AddBefore(opIterator, cost);
            if (bestSuccessors.Count > maxWidth)
            {
                bestSuccessors.RemoveLast();
                best_hValues.RemoveLast();
                bestOperatorsCosts.RemoveLast();
            }
        }

        private void addBestSuccessorsToOpenList(int parrentGValue, State predecessor)
        {
            LinkedListNode<int> hValIterator = best_hValues.First;
            LinkedListNode<int> opIterator = bestOperatorsCosts.First;
            LinkedListNode<State> stateIterator = bestSuccessors.First;
            while (hValIterator != null)
            {
                addToOpenList(stateIterator.Value, opIterator.Value + parrentGValue, predecessor, hValIterator.Value);
                hValIterator = hValIterator.Next;
                opIterator = opIterator.Next;
                stateIterator = stateIterator.Next;
            }
        }

        public override int search(bool quiet = false)
        {
            predecessor = new Dictionary<State, State>();
            printMessage("Search started. Algorithm: Beam search, width: " + maxWidth + " problem: " + dom.problemName + ", " + h.ToString(), quiet);
            DateTime start = DateTime.Now;
            openNodes.insert(0, dom.initialState);
            gValues.Add(dom.initialState, new StateInformation());
            predecessor.Add(dom.initialState, null);
            int steps = -1;
            while (openNodes.size() > 0)
            {
                steps++;
                if (steps % 100000 == 0)
                {
                    printMessage("Expanded nodes: " + (gValues.Count - openNodes.size()) +
                        "\tOpen nodes: " + openNodes.size() + "\tVisited nodes: " + gValues.Count +
                        "\tHeuristic calls: " + h.heuristicCalls, quiet);
                    if (gValues.Count > memoryLimit)
                    {
                        printMessage("Search FAILED - memory limit exceeded.", quiet);
                        DateTime end = DateTime.Now;
                        printMessage("search ended in " + (end - start).TotalSeconds + " seconds", quiet);
                        break;
                    }
                }
                State currentState = openNodes.removeMin();
                if (gValues[currentState].isClosed)
                    continue;
                addToClosedList(currentState);
                if (dom.isGoalState(currentState))
                {
                    DateTime end = DateTime.Now;
                    int GVAL = gValues[currentState].gValue;
                    printMessage("search ended in " + (end - start).TotalSeconds + " seconds", quiet);
                    printMessage("Expanded nodes: " + (gValues.Count - openNodes.size()) + ", plan length " + GVAL, quiet);
                    this.solution = extractSolution(currentState);
                    return GVAL;
                }
                int currentGValue = gValues[currentState].gValue;
                Dictionary<Operator, State> successors = dom.getSuccessors(currentState);
                selectBestSuccessors(successors);
                addBestSuccessorsToOpenList(currentGValue, currentState);
            }
            printMessage("No solution found.", quiet);
            return -1;
        }

        public BeamStackSearch(Domain d, Heuristic h) : base(d, h)
        {
            this.dom = d;
            this.h = h;
            this.gValues = new Dictionary<State, StateInformation>();
            //this.openNodes = new LeftistHeap<int[]>();
            this.openNodes = new RegularHeap<State>();
            //this.openNodes = new BinomialHeap<int[]>();
            //this.openNodes = new SingleBucket<State>();
            this.maxWidth = 2;
            this.best_hValues = new LinkedList<int>();
            this.bestOperatorsCosts = new LinkedList<int>();
            this.bestSuccessors = new LinkedList<State>();
        }
    }


}
