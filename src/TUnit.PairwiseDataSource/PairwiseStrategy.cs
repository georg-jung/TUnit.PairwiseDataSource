// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the Ms-PL license. See LICENSE file in the project root for full license information.

// ***********************************************************************
// Copyright (c) 2008 Charlie Poole
// Copyright (c) 2015 Andrew Arnott (modified from Charlie's original)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

namespace TUnit.PairwiseDataSource;

/// <summary>
/// PairwiseStrategy creates test cases by combining the parameter
/// data so that all possible pairs of data items are used.
/// </summary>
/// <remarks>
/// <para>
/// The number of test cases that cover all possible pairs of test function
/// parameters values is significantly less than the number of test cases
/// that cover all possible combination of test function parameters values.
/// And because different studies show that most of software failures are
/// caused by combination of no more than two parameters, pairwise testing
/// can be an effective ways to test the system when it's impossible to test
/// all combinations of parameters.
/// </para>
/// <para>
/// The PairwiseStrategy code is based on "jenny" tool by Bob Jenkins:
/// <see href="http://burtleburtle.net/bob/math/jenny.html"/>.
/// </para>
/// <para>
/// This implementation is derived from the Xunit.Combinatorial library by Andrew Arnott,
/// which was itself based on Charlie Poole's NUnit implementation.
/// </para>
/// </remarks>
internal static class PairwiseStrategy
{
    /// <summary>
    /// Creates a set of test cases for specified dimensions.
    /// </summary>
    /// <param name="dimensions">
    /// An array which contains information about dimensions. Each element of
    /// this array represents a number of features in the specific dimension.
    /// </param>
    /// <returns>
    /// A set of test cases.
    /// </returns>
    public static int[][] GetTestCases(int[] dimensions)
    {
        return [.. from testCase in new PairwiseTestCaseGenerator().GetTestCases(dimensions)
                   select testCase.Features];
    }

    private static bool IsTupleCovered(this TestCaseInfo testCaseInfo, FeatureTuple tuple)
    {
        for (int i = 0; i < tuple.Length; i++)
        {
            if (testCaseInfo.Features[tuple[i].Dimension] != tuple[i].Feature)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// FleaRand is a pseudo-random number generator developed by Bob Jenkins:
    /// <see href="http://burtleburtle.net/bob/rand/talksmall.html#flea" />.
    /// </summary>
    private sealed class FleaRand
    {
        private uint _b;
        private uint _c;
        private uint _d;
        private uint _z;
        private readonly uint[] _m;
        private readonly uint[] _r;
        private uint _q;

        public FleaRand(uint seed)
        {
            _b = seed;
            _c = seed;
            _d = seed;
            _z = seed;
            _m = new uint[256];
            _r = new uint[256];

            for (int i = 0; i < _m.Length; i++)
            {
                _m[i] = seed;
            }

            for (int i = 0; i < 10; i++)
            {
                Batch();
            }

            _q = 0;
        }

        public uint Next()
        {
            if (_q == 0)
            {
                Batch();
                _q = (uint)_r.Length - 1;
            }
            else
            {
                _q--;
            }

            return _r[_q];
        }

        private void Batch()
        {
            uint a;
            uint b = _b;
            uint c = _c + (++_z);
            uint d = _d;

            for (int i = 0; i < _r.Length; i++)
            {
                a = _m[b % _m.Length];
                _m[b % _m.Length] = d;
                d = (c << 19) + (c >> 13) + b;
                c = b ^ _m[i];
                b = a + d;
                _r[i] = c;
            }

            _b = b;
            _c = c;
            _d = d;
        }
    }

    /// <summary>
    /// FeatureInfo represents coverage of a single value of test function
    /// parameter, represented as a pair of indices, Dimension and Feature. In
    /// terms of unit testing, Dimension is the index of the test parameter and
    /// Feature is the index of the supplied value in that parameter's list of
    /// sources.
    /// </summary>
    private sealed class FeatureInfo(int dimension, int feature)
    {
        public int Dimension { get; } = dimension;

        public int Feature { get; } = feature;
    }

    /// <summary>
    /// A FeatureTuple represents a combination of features, one per test
    /// parameter, which should be covered by a test case. In the
    /// PairwiseStrategy, we are only trying to cover pairs of features, so the
    /// tuples actually may contain only single feature or pair of features, but
    /// the algorithm itself works with triplets, quadruples and so on.
    /// </summary>
    private sealed class FeatureTuple
    {
        private readonly FeatureInfo[] _features;

        public FeatureTuple(FeatureInfo feature1)
        {
            _features = [feature1];
        }

        public FeatureTuple(FeatureInfo feature1, FeatureInfo feature2)
        {
            _features = [feature1, feature2];
        }

        public int Length => _features.Length;

        public FeatureInfo this[int index] => _features[index];
    }

    /// <summary>
    /// TestCase represents a single test case covering a list of features.
    /// </summary>
    private sealed class TestCaseInfo(int length)
    {
        public int[] Features { get; } = new int[length];
    }

    /// <summary>
    /// PairwiseTestCaseGenerator class implements an algorithm which generates
    /// a set of test cases which covers all pairs of possible values of test
    /// function.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The algorithm starts with creating a set of all feature tuples which we
    /// will try to cover (see <see
    /// cref="PairwiseTestCaseGenerator.CreateAllTuples" /> method). This set
    /// includes every single feature and all possible pairs of features. We
    /// store feature tuples in the 3-D collection (where axes are "dimension",
    /// "feature", and "all combinations which includes this feature"), and for
    /// every two feature (e.g. "A" and "B") we generate both ("A", "B") and
    /// ("B", "A") pairs. This data structure extremely reduces the amount of
    /// time needed to calculate coverage for a single test case (this
    /// calculation is the most time-consuming part of the algorithm).
    /// </para>
    /// <para>
    /// Then the algorithm picks one tuple from the uncovered tuple, creates a
    /// test case that covers this tuple, and then removes this tuple and all
    /// other tuples covered by this test case from the collection of uncovered
    /// tuples.
    /// </para>
    /// <para>
    /// Picking a tuple to cover.
    /// </para>
    /// <para>
    /// There are no any special rules defined for picking tuples to cover. We
    /// just pick them one by one, in the order they were generated.
    /// </para>
    /// <para>
    /// Test generation.
    /// </para>
    /// <para>
    /// Test generation starts from creating a completely random test case which
    /// covers, nevertheless, previously selected tuple. Then the algorithm
    /// tries to maximize number of tuples which this test covers.
    /// </para>
    /// <para>
    /// Test generation and maximization process repeats seven times for every
    /// selected tuple and then the algorithm picks the best test case ("seven"
    /// is a magic number which provides good results in acceptable time).
    /// </para>
    /// <para>Maximizing test coverage.</para>
    /// <para>
    /// To maximize tests coverage, the algorithm walks thru the list of mutable
    /// dimensions (mutable dimension is a dimension that are not included in
    /// the previously selected tuple). Then for every dimension, the algorithm
    /// walks thru the list of features and checks if this feature provides
    /// better coverage than randomly selected feature, and if yes keeps this
    /// feature.
    /// </para>
    /// <para>
    /// This process repeats while it shows progress. If the last iteration
    /// doesn't improve coverage, the process ends.
    /// </para>
    /// <para>
    /// In addition, for better results, before start every iteration, the
    /// algorithm "scrambles" dimensions - so for every iteration dimension
    /// probes in a different order.
    /// </para>
    /// </remarks>
    private sealed class PairwiseTestCaseGenerator
    {
        private FleaRand? _prng;
        private int[]? _dimensions;
        private List<FeatureTuple>[][]? _uncoveredTuples;

        public List<TestCaseInfo> GetTestCases(int[] dimensions)
        {
            _prng = new FleaRand(15485863);
            _dimensions = dimensions;

            CreateAllTuples();

            List<TestCaseInfo> testCases = [];

            while (true)
            {
                FeatureTuple? tuple = GetNextTuple();

                if (tuple is null)
                {
                    break;
                }

                TestCaseInfo testCase = CreateTestCase(tuple);

                RemoveTuplesCoveredByTest(testCase);

                testCases.Add(testCase);
            }

            return testCases;
        }

        private int GetNextRandomNumber()
        {
            return (int)(_prng!.Next() >> 1);
        }

        private void CreateAllTuples()
        {
            _uncoveredTuples = new List<FeatureTuple>[_dimensions!.Length][];

            for (int d = 0; d < _dimensions.Length; d++)
            {
                _uncoveredTuples[d] = new List<FeatureTuple>[_dimensions[d]];

                for (int f = 0; f < _dimensions[d]; f++)
                {
                    _uncoveredTuples[d][f] = CreateTuples(d, f);
                }
            }
        }

        private List<FeatureTuple> CreateTuples(int dimension, int feature)
        {
            List<FeatureTuple> result = [];

            result.Add(new FeatureTuple(new FeatureInfo(dimension, feature)));

            for (int d = 0; d < _dimensions!.Length; d++)
            {
                if (d != dimension)
                {
                    for (int f = 0; f < _dimensions[d]; f++)
                    {
                        result.Add(new FeatureTuple(new FeatureInfo(dimension, feature), new FeatureInfo(d, f)));
                    }
                }
            }

            return result;
        }

        private FeatureTuple? GetNextTuple()
        {
            for (int d = 0; d < _uncoveredTuples!.Length; d++)
            {
                for (int f = 0; f < _uncoveredTuples[d].Length; f++)
                {
                    List<FeatureTuple> tuples = _uncoveredTuples[d][f];

                    if (tuples.Count > 0)
                    {
                        FeatureTuple tuple = tuples[0];
                        tuples.RemoveAt(0);
                        return tuple;
                    }
                }
            }

            return null;
        }

        private TestCaseInfo CreateTestCase(FeatureTuple tuple)
        {
            TestCaseInfo? bestTestCase = null;
            int bestCoverage = -1;

            for (int i = 0; i < 7; i++)
            {
                TestCaseInfo testCase = CreateRandomTestCase(tuple);

                int coverage = MaximizeCoverage(testCase, tuple);

                if (coverage > bestCoverage)
                {
                    bestTestCase = testCase;
                    bestCoverage = coverage;
                }
            }

            return bestTestCase!;
        }

        private TestCaseInfo CreateRandomTestCase(FeatureTuple tuple)
        {
            TestCaseInfo result = new(_dimensions!.Length);

            for (int d = 0; d < _dimensions.Length; d++)
            {
                result.Features[d] = GetNextRandomNumber() % _dimensions[d];
            }

            for (int i = 0; i < tuple.Length; i++)
            {
                result.Features[tuple[i].Dimension] = tuple[i].Feature;
            }

            return result;
        }

        private int MaximizeCoverage(TestCaseInfo testCase, FeatureTuple tuple)
        {
            int totalCoverage = 1;
            int[] mutableDimensions = GetMutableDimensions(tuple);

            while (true)
            {
                bool progress = false;

                ScrambleDimensions(mutableDimensions);

                for (int i = 0; i < mutableDimensions.Length; i++)
                {
                    int d = mutableDimensions[i];

                    int bestCoverage = CountTuplesCoveredByTest(testCase, d, testCase.Features[d]);

                    int newCoverage = MaximizeCoverageForDimension(testCase, d, bestCoverage);

                    totalCoverage += newCoverage;

                    if (newCoverage > bestCoverage)
                    {
                        progress = true;
                    }
                }

                if (!progress)
                {
                    return totalCoverage;
                }
            }
        }

        private int[] GetMutableDimensions(FeatureTuple tuple)
        {
            bool[] immutableDimensions = new bool[_dimensions!.Length];

            for (int i = 0; i < tuple.Length; i++)
            {
                immutableDimensions[tuple[i].Dimension] = true;
            }

            List<int> result = [];

            for (int d = 0; d < _dimensions.Length; d++)
            {
                if (!immutableDimensions[d])
                {
                    result.Add(d);
                }
            }

            return [.. result];
        }

        private void ScrambleDimensions(int[] dimensions)
        {
            for (int i = 0; i < dimensions.Length; i++)
            {
                int j = GetNextRandomNumber() % dimensions.Length;
                (dimensions[j], dimensions[i]) = (dimensions[i], dimensions[j]);
            }
        }

        private int MaximizeCoverageForDimension(TestCaseInfo testCase, int dimension, int bestCoverage)
        {
            List<int> bestFeatures = new(_dimensions![dimension]);

            for (int f = 0; f < _dimensions[dimension]; f++)
            {
                testCase.Features[dimension] = f;

                int coverage = CountTuplesCoveredByTest(testCase, dimension, f);

                if (coverage >= bestCoverage)
                {
                    if (coverage > bestCoverage)
                    {
                        bestCoverage = coverage;
                        bestFeatures.Clear();
                    }

                    bestFeatures.Add(f);
                }
            }

            testCase.Features[dimension] = bestFeatures[GetNextRandomNumber() % bestFeatures.Count];

            return bestCoverage;
        }

        private int CountTuplesCoveredByTest(TestCaseInfo testCase, int dimension, int feature)
        {
            int result = 0;

            List<FeatureTuple> tuples = _uncoveredTuples![dimension][feature];

            for (int i = 0; i < tuples.Count; i++)
            {
                if (testCase.IsTupleCovered(tuples[i]))
                {
                    result++;
                }
            }

            return result;
        }

        private void RemoveTuplesCoveredByTest(TestCaseInfo testCase)
        {
            for (int d = 0; d < _uncoveredTuples!.Length; d++)
            {
                for (int f = 0; f < _uncoveredTuples[d].Length; f++)
                {
                    List<FeatureTuple> tuples = _uncoveredTuples[d][f];

                    for (int i = tuples.Count - 1; i >= 0; i--)
                    {
                        if (testCase.IsTupleCovered(tuples[i]))
                        {
                            tuples.RemoveAt(i);
                        }
                    }
                }
            }
        }
    }
}
