using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wintellect;

namespace SASPlan
{
    public interface IHeap<Key, Value> where Key : IComparable
    {
        void insert(Key k, Value v);
        Value getMin();
        int getMinKey();
        Value removeMin();
        bool remove(Value v);
        bool change(Value v, Key newKey);
        int size();
    }

    public class RegularHeap<Value> : IHeap<int, Value>
    {
        private class TreeNode<Key, TheValue> where Key : IComparable
        {
            public TheValue val { get; set; }
            public Key key { get; set; }
            public int index { get; set; }

            public TreeNode(TheValue value, Key k, int index)
            {
                this.val = value;
                this.key = k;
                this.index = index;
            }

            public override string ToString()
            {
                return "Key :" + key + " index:" + index;
            }
        }

        private IList<TreeNode<int, Value>> tree;

        private bool isRoot(TreeNode<int, Value> t)
        {
            return t.index == 0;
        }

        private bool isLeaf(TreeNode<int, Value> t)
        {
            return getLeftSuccesor(t) == null;
        }

        private TreeNode<int, Value> getPredecessor(TreeNode<int, Value> t)
        {
            return t.index == 0 ? null : tree[(t.index - 1) / 2];
        }

        private TreeNode<int, Value> getLeftSuccesor(TreeNode<int, Value> t)
        {
            int index = t.index * 2 + 1;
            return tree.Count > index ? tree[index] : null;
        }

        private TreeNode<int, Value> getRightSuccesor(TreeNode<int, Value> t)
        {
            int index = t.index * 2 + 2;
            return tree.Count > index ? tree[index] : null;
        }

        public bool isEmpty()
        {
            return tree.Count == 0;
        }

        private void checkUp(TreeNode<int, Value> node)
        {
            TreeNode<int, Value> current = node,
                predecessor = getPredecessor(current);
            while (!isRoot(current) && current.key < predecessor.key)
            {
                swap(current, predecessor);
                predecessor = getPredecessor(current);
            }
        }

        private void swap(TreeNode<int, Value> current, TreeNode<int, Value> predecessor)
        {
            TreeNode<int, Value> stored = tree[current.index];

            tree[current.index] = tree[predecessor.index];
            tree[predecessor.index] = stored;

            int storedIndex = current.index;
            current.index = predecessor.index;
            predecessor.index = storedIndex;
        }

        private void checkDown(TreeNode<int, Value> node)
        {
            TreeNode<int, Value> current = node,
                succesor = null,
                succesorLeft = getLeftSuccesor(current),
                succesorRight = getRightSuccesor(current);

            if (succesorLeft != null)
            {
                if (succesorRight == null)
                    succesor = succesorLeft;
                else
                    succesor = (succesorLeft.key < succesorRight.key ? succesorLeft : succesorRight);

                while (succesor.key < current.key && !isLeaf(current))
                {
                    swap(current, succesor);

                    succesorLeft = getLeftSuccesor(current);
                    succesorRight = getRightSuccesor(current);
                    if (succesorLeft != null)
                    {
                        if (succesorRight == null)
                            succesor = succesorLeft;
                        else
                            succesor = (succesorLeft.key < succesorRight.key ? succesorLeft : succesorRight);
                    }
                }
            }
        } 

        public RegularHeap()
        {
            this.tree = new List<TreeNode<int, Value>>();
        }

        #region Heap<int,Value> Members

        public void insert(int k, Value v)
        {
            TreeNode<int, Value> newNode = new TreeNode<int, Value>(v, k, tree.Count);
            tree.Add(newNode);
            checkUp(newNode);
        }

        public Value getMin()
        {
            return (tree.Count > 0 ? tree[0].val : default(Value));
        }

        public Value removeMin()
        {
            Value result = tree[0].val;
            swap(tree[0], tree[tree.Count - 1]);
            tree.RemoveAt(tree.Count - 1);
            if (!isEmpty())
                checkDown(tree[0]);
            return result;
        }

        public bool remove(Value v)
        {
            throw new NotImplementedException();
        }

        public bool change(Value v, int newKey)
        {
            throw new NotImplementedException();
        }

        public int size()
        {
            return tree.Count;
        }

        public int getMinKey()
        {
            return (tree.Count > 0 ? tree[0].key : -1);
        }

        #endregion
    }

    public class LeftistHeap<Value> : IHeap<int, Value>
    {
        private class TreeNode<Key, TheValue> where Key : IComparable
        {
            public TheValue val { get; set; }
            public Key key { get; set; }
            public int npl { get; set; }
            public TreeNode<Key, TheValue> ancestor,
                leftSuccesor,
                rightSuccesor;

            public bool isRoot()
            {
                return this.ancestor == null;
            }

            public bool isLeaf()
            {
                return leftSuccesor == null;
            }

            public TreeNode(TheValue value, Key k, int npl)
            {
                this.val = value;
                this.key = k;
                this.npl = npl;
            }

            public override string ToString()
            {
                return "Key: " + key + " npl: " + npl;
            }
        }

        private TreeNode<int, Value> root;
        private int count;

        private int getNpl(TreeNode<int, Value> node)
        {
            if (node == null)
                return -1;
            return node.npl;
        }

        private TreeNode<int, Value> merge(TreeNode<int, Value> first, TreeNode<int, Value> second)
        {
            if (first == null)
                return second;
            if (second == null)
                return first;
            if (first.key > second.key)
                return merge(second, first);

            TreeNode<int, Value> newRight = merge(first.rightSuccesor, second);
            first.rightSuccesor = newRight;
            newRight.ancestor = first;
            if (getNpl(first.rightSuccesor) > getNpl(first.leftSuccesor))
            {
                TreeNode<int, Value> stored = first.leftSuccesor;
                first.leftSuccesor = first.rightSuccesor;
                first.rightSuccesor = stored;
            }
            first.npl = getNpl(first.rightSuccesor) + 1;

            return first;
        }

        #region IHeap<int,Value> Members

        public void insert(int k, Value v)
        {
            TreeNode<int, Value> newNode = new TreeNode<int, Value>(v, k, 0);
            root = merge(root, newNode);
            count++;
        }

        public Value getMin()
        {
            return root.val;
        }

        public Value removeMin()
        {
            Value result = root.val;
            root = merge(root.leftSuccesor, root.rightSuccesor);
            count--;
            return result;
        }

        public bool remove(Value v)
        {
            throw new NotImplementedException();
        }

        public bool change(Value v, int newKey)
        {
            throw new NotImplementedException();
        }

        public int size()
        {
            return count;
        }

        public int getMinKey()
        {
            return root.key;
        }

        #endregion

        #region Constructors

        public LeftistHeap()
        {
            this.root = null;
            this.count = 0;
        }

        private LeftistHeap(TreeNode<int, Value> root, int size)
        {
            this.root = root;
            this.count = size;
        }

        #endregion Constructors

    }

    public class BinomialHeap<Value> : IHeap<int, Value>
    {
        private class TreeNode<Key, TheValue> where Key : IComparable
        {
            public TheValue val { get; set; }
            public Key key { get; set; }
            public int rank;
            public TreeNode<Key, TheValue> ancestor;
            public List<TreeNode<Key, TheValue>> succesors;

            public bool isRoot()
            {
                return this.ancestor == null;
            }

            public bool isLeaf()
            {
                return succesors.Count == 0;
            }

            public TreeNode(TheValue value, Key k, int rank)
            {
                this.val = value;
                this.key = k;
                this.rank = rank;
                this.succesors = new List<BinomialHeap<Value>.TreeNode<Key, TheValue>>();
            }

            public override string ToString()
            {
                return "Key: " + key;
            }
        }

        private int count = 0;

        private TreeNode<int, Value> join(TreeNode<int, Value> first, TreeNode<int, Value> second)
        {
            if (first.key > second.key)
                return join(second, first);
            second.ancestor = first;
            first.succesors.Add(second);
            first.rank += 1;
            return first;
        }

        private LinkedList<TreeNode<int, Value>> trees;

        private LinkedList<TreeNode<int, Value>> merge(LinkedList<TreeNode<int, Value>> first,
            LinkedList<TreeNode<int, Value>> second)
        {
            first.AddLast(second.First);
            return first;
        }

        private LinkedList<TreeNode<int, Value>> repair(List<TreeNode<int, Value>>[] list)
        {
            LinkedList<TreeNode<int, Value>> result = new LinkedList<BinomialHeap<Value>.TreeNode<int, Value>>();
            for (int i = 0; i < list.Length; i++)
            {
                while(list[i].Count > 1)
                {
                    TreeNode<int, Value> first = list[i][0];
                    TreeNode<int, Value> second = list[i][1];
                    list[i].RemoveAt(1);
                    list[i].RemoveAt(0);
                    list[i + 1].Add(join(first, second));
                }
                if (list[i].Count > 0)
                    result.AddLast(new LinkedListNode<TreeNode<int,Value>>(list[i][0]));
            }
            return result;
        }

        #region Constructors

        public BinomialHeap()
        {
            this.count = 0;
            this.trees = new LinkedList<BinomialHeap<Value>.TreeNode<int, Value>>();
        }

        #endregion Constructors

        #region IHeap<int,Value> Members

        public void insert(int k, Value v)
        {
            LinkedListNode<TreeNode<int, Value>> newnode = 
                new LinkedListNode<TreeNode<int,Value>>(new TreeNode<int, Value>(v, k, 0));
            trees.AddFirst(newnode);
            count++;
        }

        public Value getMin()
        {
            if (trees.First == null)
                return default(Value);
            TreeNode<int, Value> min = trees.First.Value;
            List<TreeNode<int, Value>>[] byRank =
                new List<BinomialHeap<Value>.TreeNode<int, Value>>[(int)Math.Log(count, 2) + 1];
            byRank.Initialize();
            foreach (TreeNode<int, Value> item in trees)
            {
                if (item.key < min.key)
                    min = item;
                byRank[item.rank].Add(item);
            }
            trees = repair(byRank);

            return min.val;
        }

        public Value removeMin()
        {
            if (trees.First == null)
                return default(Value);
            TreeNode<int, Value> min = trees.First.Value;
            List<TreeNode<int, Value>>[] byRank =
                new List<BinomialHeap<Value>.TreeNode<int, Value>>[(int)Math.Log(count, 2) + 1];
            for (int i = 0; i < byRank.Length; i++)
            {
                byRank[i] = new List<TreeNode<int, Value>>();
            }
            foreach (TreeNode<int, Value> item in trees)
            {
                if (item.key < min.key)
                    min = item;
                byRank[item.rank].Add(item);
            }
            byRank[min.rank].Remove(min);
            foreach (TreeNode<int, Value> item in min.succesors)
            {
                byRank[item.rank].Add(item);
            }
            trees = repair(byRank);
            count--;
            return min.val;
        }

        public bool remove(Value v)
        {
            throw new NotImplementedException();
        }

        public bool change(Value v, int newKey)
        {
            throw new NotImplementedException();
        }

        public int size()
        {
            return count;
        }

        public int getMinKey()
        {
            return trees.Min(a => a.key);
        }

        #endregion

    }

    public class SortedListHeap<Value> : IHeap<int, Value>
    {
        private SortedList<int, Value> items;

        #region IHeap<int,Value> Members

        public void insert(int k, Value v)
        {
            items.Add(k, v);
        }

        public Value getMin()
        {
            foreach (var item in items.Keys)
            {
                return items[item];
            }
            return default(Value);
        }

        public Value removeMin()
        {
            foreach (var item in items.Keys)
            {
                Value result = items[item];
                items.Remove(item);
                return result;
            }
            return default(Value);
        }

        public bool remove(Value v)
        {
            if (items.ContainsValue(v))
            {
                items.RemoveAt(items.IndexOfValue(v));
                return true;
            }
            return false;
        }

        public bool change(Value v, int newKey)
        {
            if (remove(v))
            {
                insert(newKey, v);
                return true;
            }
            insert(newKey, v);
            return false;
        }

        public int size()
        {
            return items.Count;
        }

        public int getMinKey()
        {
            foreach (var item in items.Keys)
            {
                return item;
            }
            return -1;
        }

        #endregion

        public SortedListHeap()
        {
            items = new SortedList<int, Value>();
        }

    }

    public class SingleBucket<Value> : IHeap<int, Value>
    {
        protected class TreeNode<Key, TheValue> where Key : IComparable
        {
            public TheValue val { get; set; }
            public Key key { get; set; }

            public TreeNode(TheValue value, Key k)
            {
                this.val = value;
                this.key = k;
            }

            public override string ToString()
            {
                return "Key :" + key + " value:" + val.ToString();
            }
        }

        protected List<TreeNode<int,Value>>[] buckets;
        
        /// <summary>
        /// Size of the hash array. Range of the elements. MUST BE GREATER THAN DIFFERENCE BETWEEN THE HIGHEST POSSIBLE KEY AND THE LOWEST POSSIBLE KEY !
        /// </summary>
        protected int C;

        /// <summary>
        /// The minimum key stored in the structure 
        /// </summary>
        protected int minKey;

        /// <summary>
        /// Position of the minimum element
        /// </summary>
        protected int minPos;

        /// <summary>
        /// Number of elements in the structure
        /// </summary>
        protected int n;

        protected void insertTo(int bucket, int key, Value val)
        {
            /*
            if (buckets[bucket] == null)
                buckets[bucket] = new List<TreeNode<int, Value>>();
             */
            buckets[bucket].Add(new TreeNode<int, Value>(val, key));
        }

        public void insert(int k, Value v)
        {
            /*
            if (n > 0 && k > minKey + C)
            {
                throw new Exception();
            }
            */

            this.n++;
            int pos = k % (C+1);
            if (k < minKey)
            {
                minPos = pos;
                minKey = k;
            }
            insertTo(pos, k, v);
        }

        public Value getMin()
        {
            foreach (var item in buckets[minPos])
            {
                if (item.key == minKey)
                    return item.val;
            }
            return default(Value);
        }

        public int getMinKey()
        {
            return minKey;
        }

        public Value removeMin()
        {
            /*
            int minKeyTest = getMinKey_TotalSearch();
            if (minKey != minKeyTest)
            {
                throw new Exception();
            }
            */

            Value x = default(Value);
            for (int i = 0; i < buckets[minPos].Count; i++)
            {
                if (buckets[minPos][i].key == minKey)
                {
                    x = buckets[minPos][i].val;
                    buckets[minPos].RemoveAt(i);
                    break;
                }
            }
            n = n - 1;
            if (n > 0)
            {
                while(/*buckets[minPos] = null ||*/ buckets[minPos].Count == 0)
                    minPos = (minPos + 1) % (C + 1);
                minKey = buckets[minPos].Min(a => a.key);
            }
            else minKey = int.MaxValue;
            return x;
        }

        public bool remove(Value v)
        {
            throw new NotImplementedException();
        }

        public bool change(Value v, int newKey)
        {
            throw new NotImplementedException();
        }

        public int size()
        {
            return n;
        }
        public SingleBucket(int C)
        {
            this.n = 0;
            this.C = C;
            this.minPos = -1;
            this.minKey = int.MaxValue;
            this.buckets = new List<TreeNode<int, Value>>[C + 1];
            for (int i = 0; i < C+1; i++)
            {
                this.buckets[i] = new List<TreeNode<int, Value>>();
            }
        }
    
        /// <summary>
        /// Returns minimal key stored in the structure, using total search - very ineffective, used obly for testing correctness
        /// </summary>
        /// <returns></returns>
        protected int getMinKey_TotalSearch()
        {
            int min = int.MaxValue;
            for (int i = 0; i < buckets.Length; i++)
            {
                foreach (var item in buckets[i])
                {
                    if (item.key < min)
                        min = item.key;
                }
            }
            return min;
        }
    
    }

    public class RadixHeap<Value> : IHeap<int, Value>
    {
        protected class TreeNode<Key, TheValue> where Key : IComparable
        {
            public TheValue val { get; set; }
            public Key key { get; set; }

            public TreeNode(TheValue value, Key k)
            {
                this.val = value;
                this.key = k;
            }

            public override string ToString()
            {
                return "Key :" + key + " value:" + val.ToString();
            }
        }

        protected List<TreeNode<int, Value>>[] buckets;

        protected int[] bounds;

        /// <summary>
        /// Range of the elements. MUST BE GREATER THAN DIFFERENCE BETWEEN THE HIGHEST POSSIBLE KEY AND THE LOWEST POSSIBLE KEY !
        /// </summary>
        protected int C;

        /// <summary>
        /// Size of the hash array. Depends on C
        /// </summary>
        protected int B;

        /// <summary>
        /// Number of elements in the structure
        /// </summary>
        protected int n;

        void IHeap<int, Value>.insert(int k, Value v)
        {
            int i = B - 1;
            while (bounds[i] > k) i--;
            buckets[i].Add(new TreeNode<int, Value>(v, k));
            n++;
        }

        Value IHeap<int, Value>.getMin()
        {
            throw new NotImplementedException();
        }

        int IHeap<int, Value>.getMinKey()
        {
            throw new NotImplementedException();
        }

        Value IHeap<int, Value>.removeMin()
        {
            int i = 0, j = 0;
            while (buckets[i].Count == 0)
                i++;
            int minkey = buckets[i][0].key, minIndex = 0;
            for (j = 1; j < buckets[i].Count; j++)
            {
                if (buckets[i][j].key < minkey)
                {
                    minkey = buckets[i][j].key;
                    minIndex = j;
                }
            }
            Value minValue = buckets[i][minIndex].val;
            buckets[i].RemoveAt(minIndex);
            n--;
            if (n == 0) return minValue;
            while (buckets[i].Count == 0)
                i++;
            if (i > 0)
            {
                int k = buckets[i].Min(a => a.key);
                bounds[0] = k;
                bounds[1] = k + 1;
                for (j = 2; j < i + 1; j++)
                    bounds[j] = min(bounds[j - 1] + 1 << (j - 2), bounds[i + 1]);
                while (buckets[i].Count > 0)
                {
                    j = 0;
                    var el = buckets[i][buckets[i].Count-1];
                    buckets[i].RemoveAt(buckets[i].Count - 1);
                    while (el.key > bounds[j + 1])
                        j++;
                    buckets[j].Add(el);
                }
            }
            return minValue;
        }

        private int min(int a, int b)
        {
            return a < b ? a : b;
        }

        bool IHeap<int, Value>.remove(Value v)
        {
            throw new NotImplementedException();
        }

        bool IHeap<int, Value>.change(Value v, int newKey)
        {
            throw new NotImplementedException();
        }

        int IHeap<int, Value>.size()
        {
            return n;
        }

        public RadixHeap(int range)
        {
            this.C = range;
            this.B = (int)Math.Ceiling(Math.Log(C + 1, 2) + 2);
            this.n = 0;
            this.buckets = new List<TreeNode<int, Value>>[B];
            this.bounds = new int[B];
            for (int i = 0; i < B; i++)
                buckets[i] = new List<TreeNode<int, Value>>();
            bounds[0] = 0;
            bounds[1] = 1;
            int exp = 1;
            for (int i = 2; i < B; i++)
            {
                bounds[i] = bounds[i - 1] + exp;
                exp *= 2;
            }
            
        }

    }

    public class OrderedBagHeap< Value> : IHeap<int, Value>
    {
        Wintellect.PowerCollections.OrderedBag<Wintellect.PowerCollections.Pair<int, Value>> structure;

        public void insert(int k, Value v)
        {
            structure.Add(new Wintellect.PowerCollections.Pair<int,Value>(k, v));
        }

        public Value getMin()
        {
            throw new NotImplementedException();
        }

        public int getMinKey()
        {
            throw new NotImplementedException();
        }

        public Value removeMin()
        {
            return structure.RemoveFirst().Second;
        }

        public bool remove(Value v)
        {
            throw new NotImplementedException();
        }

        public bool change(Value v, int newKey)
        {
            throw new NotImplementedException();
        }

        public int size()
        {
            return structure.Count;
        }

        public OrderedBagHeap()
        {
            this.structure = new Wintellect.PowerCollections.OrderedBag<Wintellect.PowerCollections.Pair<int, Value>>((a,b) => a.First - b.First);
        }
    }

    public class OrderedMutliDictionaryHeap<Value> : IHeap<int, Value>
    {
        Wintellect.PowerCollections.OrderedMultiDictionary<int, Value> structure;

        public void insert(int k, Value v)
        {
            structure.Add(k, v);
        }

        public Value getMin()
        {
            throw new NotImplementedException();
        }

        public int getMinKey()
        {
            throw new NotImplementedException();
        }

        public Value removeMin()
        {
            //TODO
            var minKey = structure.RangeFrom(0, true).First();
            structure.Remove(minKey);
            return minKey.Value;
            //return structure.RangeFrom(0, true)[0].First();
        }

        public bool remove(Value v)
        {
            throw new NotImplementedException();
        }

        public bool change(Value v, int newKey)
        {
            throw new NotImplementedException();
        }

        public int size()
        {
            return structure.Count;
        }

        public OrderedMutliDictionaryHeap()
        {
            this.structure = new Wintellect.PowerCollections.OrderedMultiDictionary<int, Value>(true, (a, b) => a - b, (a, b) => 0);
        }
    }

}
