using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SASPlan
{
    class Program
    {
        public static Random r = new Random();

        [STAThread]
        static void Main(string[] args)
        {
            /*
            //Red_BlackDomain d = Red_BlackDomain.readFromFile(@"..\tests\floortile-seq-p01-001.sas");
            //Red_BlackDomain d = Red_BlackDomain.readFromFile(@"..\tests\nomystery-p01.sas");
            Red_BlackDomain d = Red_BlackDomain.readFromFile(@"..\tests\elevators-p01.sas");
            //Red_BlackDomain d = Red_BlackDomain.readFromFile(@"..\tests\parcprinter-p01.sas");
            //Red_BlackDomain d = Red_BlackDomain.readFromFile(@"..\tests\pegsol-p01.sas");
            //Red_BlackDomain d = Red_BlackDomain.readFromFile(@"..\tests\test.sas");

            d.makeAllAbstracted();
            //AStarSearch ast = new AStarSearch(d, new BlindHeuristic());
            //AStarSearch ast = new AStarSearch(d, new NotAccomplishedGoalCount(d));
            AStarSearch ast = new AStarSearch(d, new AbstractStateSizeHeuristic(d));
            */

            //runHeapTests(); return;


            //runPatternsTest(@"..\tests\nomystery-p01.sas");
            //runPatternsTest(@"..\tests\test.sas");
            //return;

            List<string> domainFiles = new List<string>();
            //domainFiles.Add(@"..\tests\test.sas");
            domainFiles.Add(@"..\tests\nomystery-p01.sas");
            //domainFiles.Add(@"..\tests\nomystery-p02.sas");
            //domainFiles.Add(@"..\tests\nomystery-p03.sas");
            domainFiles.Add(@"..\tests\elevators-p01.sas");
            domainFiles.Add(@"..\tests\floortile-seq-p01-001.sas");
            domainFiles.Add(@"..\tests\parcprinter-p01.sas");
            domainFiles.Add(@"..\tests\pegsol-p01.sas");
            //domainFiles.Add(@"..\tests\sokoban-p01.sas");
            domainFiles.Add(@"..\tests\visitall-problem14.sas");
            domainFiles.Add(@"..\tests\scanalyzer-p03.sas");

            //runTests(domainFiles); return;
            

            //Domain d = Domain.readFromFile(@"..\tests\floortile-seq-p01-001.sas");
            Domain d = Domain.readFromFile(@"..\tests\nomystery-p01.sas");
            //Domain d = Domain.readFromFile(@"..\..\..\..\TSP\TSP\bin\Release\tempTSP2SAS.sas");
            //Domain d = Domain.readFromFile(@"..\tests\nomystery-p02.sas");
            //Domain d = Domain.readFromFile(@"..\tests\nomystery-p03.sas");
            //Domain d = Domain.readFromFile(@"..\tests\elevators-p01.sas");
            //Domain d = Domain.readFromFile(@"..\tests\parcprinter-p01.sas");
            //Domain d = Domain.readFromFile(@"..\tests\pegsol-p01.sas");
            //Domain d = Domain.readFromFile(@"..\tests\sokoban-p01.sas");
            //Domain d = Domain.readFromFile(@"..\tests\visitall-problem14.sas");
            //Domain d = Domain.readFromFile(@"..\tests\scanalyzer-p03.sas");
            //Domain d = Domain.readFromFile(@"..\tests\test.sas");
            
            
            //AStarSearch ast = new AStarSearch(d, new BlindHeuristic());
            //AStarSearch ast = new AStarSearch(d, new NotAccomplishedGoalCount(d));
                
            //AStarSearch ast = new AStarSearch(d, new PDBHeuristic(d));

            //HeuristicSearchEngine ast = new HillClimbingSearch(d, new FFHeuristic(d));

            //HeuristicSearchEngine ast = new AStarSearch(d, new DeleteRelaxationHeuristic_Perfect(d));
            //HeuristicSearchEngine ast = new AStarSearch(d, new PlannigGraphLayersHeuristic(d));
            HeuristicSearchEngine ast = new AStarSearch(d, new FFHeuristic(d));
            //HeuristicSearchEngine ast = new AStarSearch(d, new WeightedHeuristic(new FFHeuristic(d), 200));
            //HeuristicSearchEngine ast = new MCTSSolver(d, new FFHeuristic(d));
            //Heuristic h = new PDBHeuristic(d);

            //PlanningGraphComputation pgc = new PlanningGraphComputation(d);
            //pgc.computePlanningGraph(d.initialState);

            //KnowledgeHolder holder = KnowledgeHolder.compute(d);
            //holder.visualize();
            //return;

            //TreeVisualizerForm f = new TreeVisualizerForm(d, new FFHeuristic(d));
            //System.Windows.Forms.Application.Run(f);
            //return;

            ast.search();
        }
        static void runPatternsTest(string domainFile)
        {
            Domain d = Domain.readFromFile(domainFile);
            //AStarSearch ast;
            PDBHeuristic h = new PDBHeuristic(d);

            bool[] isSeleceted = new bool[d.variablesCount];
            //List<TestResult> res = new List<TestResult>(); 
            if (System.IO.File.Exists("idStart.txt"))
            {
                using (var reader = new System.IO.StreamReader("idStart.txt"))
                {
                    TestResult.IDStart = int.Parse(reader.ReadLine());
                }
            }
            else
            {
                using (var writer = new System.IO.StreamWriter("results.txt", true))
                { 
                    writer.WriteLine("ID\tSize\tCreate\tSearch\tNodes");
                }
            }

            using (var writer = new System.IO.StreamWriter("results.txt", true))
            {
                writer.AutoFlush = true;
                for (int i = 0; i <= d.variablesCount; i++)
                {
                    testPatterns(isSeleceted, 0, 0, i, d, writer);
                }
                /*
                //Writing the results

                Console.WriteLine("<---- \tResults\t ---->");
                Console.WriteLine();
                Console.WriteLine("ID\tSize\tCreation\tSearch\tNodes");
                for (int i = 0; i < res.Count; i++)
                {
                    Console.WriteLine(i + "\t" + res[i].pattern.Count + "\t" + res[i].creation + "\t" + res[i].search + "\t" + res[i].nodes);
                }
                 */
            }
        }

        static void testPatterns(bool[] isSelected, int selectedCount, int position, int limit, Domain d, System.IO.StreamWriter writer)
        {
            if (selectedCount == limit)
            {
                if (TestResult.currentID < TestResult.IDStart)
                {
                    TestResult.currentID++;
                    return;
                }

                HashSet<int> pattern = new HashSet<int>();
                bool intersectsWithGoal = false;
                for (int i = 0; i < isSelected.Length; i++)
                {
                    if (isSelected[i])
                    {
                        pattern.Add(i);
                        if (d.goalConditions.Keys.Contains(i))
                            intersectsWithGoal = true;
                    }
                }
                if (!intersectsWithGoal)
                {
                    TestResult.currentID++;
                    return;
                }

                PDBHeuristic h = new PDBHeuristic(d);
                DateTime buildingStarted = DateTime.Now;
                h.initializePatterns(pattern);
                DateTime buildingEnded = DateTime.Now;
                AStarSearch ast = new AStarSearch(d, h);
                DateTime searchStarted = DateTime.Now;
                ast.search();
                DateTime searchEnded = DateTime.Now;
                writer.WriteLine(TestResult.currentID + "\t" + pattern.Count + "\t" + String.Format("{0:0.##}", (buildingEnded - buildingStarted).TotalSeconds) +
                    "\t" + String.Format("{0:0.##}", (searchEnded - searchStarted).TotalSeconds) + "\t" + h.heuristicCalls);
                //res.Add(new TestResult(d, pattern, (buildingEnded - buildingStarted).TotalSeconds, (searchEnded - searchStarted).TotalSeconds, nodes));
                TestResult.currentID++;

                return;
            }
            if (selectedCount < limit - (isSelected.Length - position))
                return;

            if (position >= isSelected.Length)
                return;

            isSelected[position] = true;
            testPatterns(isSelected, selectedCount + 1, position + 1, limit, d, writer);
            isSelected[position] = false;
            testPatterns(isSelected, selectedCount, position + 1, limit, d, writer);
        }

        static void runTests(List<string> domainFiles)
        {
            Domain d;
            //AStarSearch ast;
            HillClimbingSearch ast;
            foreach (var item in domainFiles)
            {
                /*
                d = Domain.readFromFile(item);
                ast = new AStarSearch(d, new BlindHeuristic());
                ast.search();
                Console.WriteLine();
                 
                d = Domain.readFromFile(item);
                ast = new AStarSearch(d, new NotAccomplishedGoalCount(d));
                ast.search();
                Console.WriteLine();
                
                d = Domain.readFromFile(item);
                ast = new AStarSearch(d, new PDBHeuristic(d));
                ast.search();
                Console.WriteLine();
                 */
                d = Domain.readFromFile(item);
                //ast = new AStarSearch(d, new FFHeuristic(d));
                ast = new HillClimbingSearch(d, new FFHeuristic(d));
                ast.search();
                Console.WriteLine();
                Console.WriteLine(" ----- new domain ----- ");
            }
        }

        #region Heap test methods
        private static void runHeapTests()
        {
            Console.WriteLine("\nTest number 0");
            heapTests(2000, 10);
            Console.WriteLine("\nTest number 1");
            heapTests(int.MaxValue / 20000, 2);
            Console.WriteLine("\nTest number 2");
            heapTests(int.MaxValue / 2000, 2);
            Console.WriteLine("\nTest number 3");
            heapTests(int.MaxValue / 2000, 10);
            Console.WriteLine("\nTest number 4");
            heapTests(int.MaxValue / 2000, 50);
            Console.WriteLine("\nTest number 5");
            heapTests(int.MaxValue / 200, 2);
            Console.WriteLine("\nTest number 6");
            heapTests(int.MaxValue / 200, 10);
            Console.WriteLine("\nTest number 7");
            heapTests(int.MaxValue / 200, 50);
            Console.WriteLine("\nTest number 8");
            heapTests(int.MaxValue / 100, 10);
            Console.WriteLine("\nTest number 9");
            heapTests(int.MaxValue / 100, 20);
            Console.WriteLine("\nTest number 10");
            heapTests(int.MaxValue / 100, 50);
            Console.WriteLine("\nTest number 11");
            heapTests(int.MaxValue / 100, 100);
        }

        private static void heapTests(int size, int removeInterval, int maxValue = -1)
        {
            if (maxValue < 0) maxValue = size / 4;
            List<IHeap<int, int>> testSubjects = new List<IHeap<int, int>>();
            List<string> names = new List<string>();
            testSubjects.Add(new RegularHeap<int>());
            names.Add("Regular Heap");
            testSubjects.Add(new LeftistHeap<int>());
            names.Add("Leftist Heap");
            testSubjects.Add(new BinomialHeap<int>());
            names.Add("Binomial Heap");
            //testSubjects.Add(new SortedListHeap<int>());
            //names.Add("SortedList Heap");
            testSubjects.Add(new SingleBucket<int>(maxValue));
            names.Add("Single Bucket Heap");
            testSubjects.Add(new RadixHeap<int>(maxValue));
            names.Add("Radix Heap");

            List<List<int>> removedValues = new List<List<int>>();
            List<TimeSpan> results = new List<TimeSpan>();
            List<int> input = generateNonDecreasingTestInput(size, maxValue);

            for (int j = 0; j < testSubjects.Count; j++)
            {
                Console.WriteLine("testing the " + names[j]);
                IHeap<int, int> heap = testSubjects[j];
                //removedValues.Add(new List<int>());
                //int index = removedValues.Count -1;
                DateTime start = DateTime.Now;

                for (int i = 0; i < size; i++)
                {
                    heap.insert(input[i], input[i]);
                    if (i % removeInterval == 0)
                    {
                        heap.removeMin();
                        if ((DateTime.Now - start).TotalSeconds > 120)
                        {
                            Console.WriteLine(names[j] + " time limit exceeded.");
                            break;
                        }
                    }
                }
                DateTime end = DateTime.Now;
                Console.WriteLine("Test finished.");
                results.Add(end - start);
                testSubjects[j] = null;
                GC.Collect();
            }

            /*
            for (int i = 0; i < removedValues[0].Count; i++)
            {
                for (int j = 0; j < removedValues.Count; j++)
                {
                    if (removedValues[j][i] != removedValues[0][i])
                    {
                        Console.WriteLine("chyba");
                    }
                }
            }
             */
            for (int i = 0; i < testSubjects.Count; i++)
            {
                Console.WriteLine(names[i] + " " + results[i].TotalSeconds + " seconds.");
            }

        }

        private static List<int> generateTestInput(int size, int maxValue)
        {
            HashSet<int> a = new HashSet<int>();
            Console.WriteLine("Generating test input");
            Random r = new Random();
            List<int> result = new List<int>();
            for (int i = 0; i < size; i++)
            {
                int item = r.Next(maxValue);
                /*while (a.Contains(item))
                    item = r.Next(maxValue);*/
                result.Add(item);
                a.Add(item);
            }
            Console.WriteLine("Done");
            return result;
        }
        private static List<int> generateNonDecreasingTestInput(int size, int maxValue)
        {
            Console.WriteLine("Generating test input");
            Random r = new Random();
            List<int> result = new List<int>();
            result.Add(0);
            for (int i = 1; i < size; i++)
            {
                int item = result[i-1] + r.Next(5);
                /*while (a.Contains(item))
                    item = r.Next(maxValue);*/
                result.Add(item);
            }
            Console.WriteLine("Done");
            return result;
        }
        

        #endregion

        #region HashMaps test methods
        private static void runHashSetTests()
        {
            testHashSet(int.MaxValue / 20000);
            testHashSet(int.MaxValue / 2000);
            testHashSet(int.MaxValue / 200);
            testHashSet(int.MaxValue / 150);
            testHashSet(int.MaxValue / 150);
        }

        private static void testHashSet(int size)
        {
            TimeSpan totalDict, totalHash;
            Console.WriteLine("\n -------- Running a new test --------");
            GC.Collect();
            List<int> inputToAdd = generateTestInput(size, size / 10),
                inputToQuerry = generateTestInput(size, size / 10);
            Dictionary<int, int> dictionary = new Dictionary<int, int>();
            HashSet<int> hashSet = new HashSet<int>();

            Console.WriteLine("Testing dictionary");
            Console.WriteLine("Adding items");
            DateTime start = DateTime.Now;
            for (int i = 0; i < size; i++)
            {
                dictionary.Add(inputToAdd[i], inputToAdd[i]);
            }
            DateTime end = DateTime.Now;
            Console.WriteLine("Items added in " + (end-start).TotalSeconds + " seconds.");
            Console.WriteLine("Querrying items");
            int found = 0;
            DateTime start2 = DateTime.Now;
            for (int i = 0; i < size; i++)
            {
                if (dictionary.ContainsKey(inputToQuerry[i]))
                    found++;
            }
            DateTime end2 = DateTime.Now;
            Console.WriteLine("Querrying finished in " + (end2 - start2).TotalSeconds + "seconds.");
            totalDict = ((end2 - start2) + (end - start));
            Console.WriteLine("Total time " + totalDict.TotalSeconds + "seconds.");
            dictionary.Clear();
            GC.Collect();

            Console.WriteLine("\nTesting HashSet");
            Console.WriteLine("Adding items");
            start = DateTime.Now;
            for (int i = 0; i < size; i++)
            {
                hashSet.Add(inputToAdd[i]);
            }
            end = DateTime.Now;
            Console.WriteLine("Items added in " + (end - start).TotalSeconds + " seconds.");
            Console.WriteLine("Querrying items");
            found = 0;
            start2 = DateTime.Now;
            for (int i = 0; i < size; i++)
            {
                if (hashSet.Contains(inputToQuerry[i]))
                    found++;
            }
            end2 = DateTime.Now;
            Console.WriteLine("Querrying finished in " + (end2 - start2).TotalSeconds + "seconds.");
            totalHash = ((end2 - start2) + (end - start));
            Console.WriteLine("Total time " + totalHash.TotalSeconds + "seconds.");
            Console.WriteLine("Results: \nDictionary: " + totalDict.TotalSeconds + "\nHashSet: " + totalHash.TotalSeconds);
        }

        #endregion

        #region EqualityComparer test methods

        private static void testArrayEquality()
        {
            HashSet<int[]> hash = new HashSet<int[]>(new ArrayEqualityComparer());
            int[] a = { 0, 0 }, b = { 0, 0 };
            Console.WriteLine(a.GetHashCode());
            Console.WriteLine(b.GetHashCode());
            if (a.Equals(b))
                Console.WriteLine("equal");
            else Console.WriteLine("not equal");
            hash.Add(a);
            if (hash.Contains(a))
            {
                Console.WriteLine("t");
            }
            else Console.WriteLine("f");
            if (hash.Contains(b))
            {
                Console.WriteLine("t");
            }
            else Console.WriteLine("f");
        }

        private static List<int[]> generateInputs(int size, int arraySize, int[] valuesRange)
        {
            Console.WriteLine("Generating inputs");
            List<int[]> result = new List<int[]>();
            for (int i = 0; i < size; i++)
            {
                int[] item = generateInput(arraySize, valuesRange);
                result.Add(item);
            }
            Console.WriteLine("Done");
            return result;
        }

        private static int[] generateInput(int arraySize, int[] valuesRange)
        {

            int[] result = new int[arraySize];
            for (int i = 0; i < arraySize; i++)
            {
                result[i] = r.Next(valuesRange[i]);
            }

            return result;
        }

        private static TimeSpan testComparer(List<int[]> inputsToAdd, List<int[]> inputsToTest, IEqualityComparer<int[]> testSubject, string name)
        {
            GC.Collect();
            Console.WriteLine("\nTesting " + name);
            Console.WriteLine("Adding items");
            HashSet<int[]> h = new HashSet<int[]>();
            DateTime startAdding = DateTime.Now;
            for (int i = 0; i < inputsToAdd.Count; i++)
            {
                if (!h.Contains(inputsToAdd[i]))
                    h.Add(inputsToAdd[i]);
            }
            DateTime endAdding = DateTime.Now;
            Console.WriteLine("Done");

            Console.WriteLine("Finding items");
            DateTime startFinding = DateTime.Now;
            for (int i = 0; i < inputsToTest.Count; i++)
            {
                if (h.Contains(inputsToTest[i]))
                    h.Remove(inputsToTest[i]);
            }
            DateTime endFinding = DateTime.Now;
            Console.WriteLine("Done");
            Console.WriteLine("Time to add: " + (endAdding - startAdding).TotalSeconds + " seconds");
            Console.WriteLine("Time to find: " + (endFinding - startFinding).TotalSeconds + " seconds");
            Console.WriteLine("Total time: " + ((endAdding - startAdding) + (endFinding - startFinding)).TotalSeconds + " seconds");
            return (endAdding - startAdding) + (endFinding - startFinding);
        }

        private static void runTestOnComparers(int size, int arraySize, bool standard)
        {
            Console.WriteLine("\nNew test started ------------");

            int[] valuesRange = new int[arraySize];
            if (standard)
                for (int i = 0; i < arraySize; i++)
                    valuesRange[i] = i;
            else
            {
                for (int i = 0; i < arraySize / 3; i++)
                {
                    valuesRange[i] = 2;
                }
                for (int i = arraySize / 3; i < 2 * arraySize / 3; i++)
                {
                    valuesRange[i] = r.Next(i / 2);
                }
                for (int i = 2 * arraySize / 3; i < arraySize; i++)
                {
                    valuesRange[i] = 50;
                }
            }

            List<int[]> toAdd = generateInputs(size, arraySize, valuesRange),
                toFind = generateInputs(0, arraySize, valuesRange);
            TimeSpan t0 = testComparer(toAdd, toFind, new ArrayEqualityComparer(), "comparer 0");
            TimeSpan t1 = testComparer(toAdd, toFind, new ArrayEqualityComparer1(), "comparer 1");
            TimeSpan t2 = testComparer(toAdd, toFind, new ArrayEqualityComparer2(), "comparer 2");
            Console.WriteLine("Results: ");
            Console.WriteLine("comparer 0: " + t0.TotalSeconds + " seconds");
            Console.WriteLine("comparer 1: " + t1.TotalSeconds + " seconds");
            Console.WriteLine("comparer 2: " + t2.TotalSeconds + " seconds");
        }

        private static void testComparers()
        {
            runTestOnComparers(1000000, 20, true);
            runTestOnComparers(1000000, 30, true);
            runTestOnComparers(1000000, 40, true);
            runTestOnComparers(1000000, 50, true);
            runTestOnComparers(1000000, 60, true);
            runTestOnComparers(1000000, 70, true);

            runTestOnComparers(1000000, 20, false);
            runTestOnComparers(1000000, 30, false);
            runTestOnComparers(1000000, 40, false);
            runTestOnComparers(1000000, 50, false);
            runTestOnComparers(1000000, 60, false);
            runTestOnComparers(1000000, 70, false);
        }

        #endregion
    }
}
