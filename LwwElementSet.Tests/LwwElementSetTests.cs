using System;
using System.Collections.Generic;
using System.Linq;
using LwwElementSet.Net;
using Xunit;

namespace LwwElementSet.Tests
{
  public class LwwElementSetTests
  {
    [Theory]
    [InlineData(100)]
    /// <summary>
    /// A(a,0 b,3 c,4 d,8 d,9 d,10 e,13) R(a,0 b,1 b,2 c,5 d,9) => S(a b d e)
    /// A(a,0 b,3 c,4 d,8 d,9 d,10 e,13) R(a,0 b,1 b,2 c,5 d,9) => S(b d e)
    /// </summary>
    /// <param name="fuzzIteration">Number of iteration to fuzz.</param>
    public void FuzzEventuallyConsistent(int fuzzIteration)
    {
      const int a = 0;
      const int r = 1;
      List<(int, string, long)> actions = new List<(int, string, long)>()
      {
        (a, "a", 0L), (a, "b", 3L), (a, "c", 4L), (a, "d", 8L), (a, "d", 9L), (a, "d", 10L), (a, "e", 13L),
        (r, "a", 0L), (r, "b", 1L), (r, "b", 2L), (r, "c", 5), (r, "d", 9), (r, "f", 13),
      };
      HashSet<string> eventualSetAdd = new HashSet<string>() { "a", "b", "d", "e" };
      HashSet<string> eventualSetRemove = new HashSet<string>() { "b", "d", "e" };
      for (int i = 0; i < fuzzIteration; i++)
      {
        Bias b = (Bias)ThreadSafeRandom.RandomOfCurrentThread.Next(1, 2);
        LwwElementSet<string> set = new LwwElementSet<string>(b);
        actions.Shuffle();
        foreach (var tup in actions)
        {
          if (tup.Item1 == a)
          {
            set.Add(tup.Item2, tup.Item3);
          }
          else
          {
            set.Remove(tup.Item2, tup.Item3);
          }
        }

        var eventualSet = b == Bias.Add ? eventualSetAdd : eventualSetRemove;
        Assert.Equal(eventualSet.Count, set.Count());
        Assert.True(set.All(item => eventualSet.Contains(item)));
      }
    }

    /// <summary>
    /// 1) A(a,0 b,3 c,4 d,8 d,9 d,10 e,13) R(a,0 b,1 b,2 c,5 d,9)
    /// 2) A(a,1 b,4 c,3 d,10 e,11) R(a,0 b,0 c,6)
    /// =  A(a,1 b,4 c,4 d,10 e,13) R(a,0 b,2 c,6 d,9) => S(a b d e)
    /// </summary>
    [Fact]
    public void CanMerge()
    {
      Func<(LwwElementSet<string>, LwwElementSet<string>)> createSets = () =>
      {
        LwwElementSet<string> s1 = new LwwElementSet<string>(Bias.Add);
        s1.Add("a", 0);
        s1.Add("b", 3);
        s1.Add("c", 4);
        s1.Add("d", 8);
        s1.Add("d", 9);
        s1.Add("d", 10);
        s1.Add("e", 13);
        s1.Remove("a", 0);
        s1.Remove("b", 1);
        s1.Remove("b", 2);
        s1.Remove("c", 5);
        s1.Remove("d", 9);

        LwwElementSet<string> s2 = new LwwElementSet<string>(Bias.Add);
        s2.Add("a", 1);
        s2.Add("b", 4);
        s2.Add("c", 3);
        s2.Add("d", 10);
        s2.Add("e", 11);
        s2.Remove("a", 0);
        s2.Remove("b", 0);
        s2.Remove("c", 6);

        return (s1, s2);
      };
      HashSet<string> eventualSet = new HashSet<string>() {
        "a", "b", "d", "e"
      };
      Dictionary<string, long> expectedAdds = new Dictionary<string, long>
      {
        ["a"] = 1,
        ["b"] = 4,
        ["c"] = 4,
        ["d"] = 10,
        ["e"] = 13,
      };
      Dictionary<string, long> expectedRemoves = new Dictionary<string, long>
      {
        ["a"] = 0,
        ["b"] = 2,
        ["c"] = 6,
        ["d"] = 9,
      };

      var (set1, set2) = createSets();
      set1.Merge(set2);
      Assert.Equal(expectedAdds.Count, set1._adds.Count);
      Assert.True(set1._adds.All(kv => expectedAdds[kv.Key] == kv.Value));
      Assert.Equal(expectedRemoves.Count, set1._removes.Count);
      Assert.True(set1._removes.All(kv => expectedRemoves[kv.Key] == kv.Value));
      Assert.True(set1.All(item => eventualSet.Contains(item)));

      (set1, set2) = createSets();
      set2.Merge(set1);
      Assert.Equal(expectedAdds.Count, set2._adds.Count);
      Assert.True(set2._adds.All(kv => expectedAdds[kv.Key] == kv.Value));
      Assert.Equal(expectedRemoves.Count, set2._removes.Count);
      Assert.True(set2._removes.All(kv => expectedRemoves[kv.Key] == kv.Value));
      Assert.True(set2.All(item => eventualSet.Contains(item)));
    }

    [Fact]
    public void CanMergeThrowIfDifferentBias()
    {
      LwwElementSet<string> set1 = new LwwElementSet<string>(Bias.Add);
      LwwElementSet<string> set2 = new LwwElementSet<string>(Bias.Remove);
      Assert.Throws<InvalidOperationException>(() => set1.Merge(set2));
    }

    [Fact]
    public void ExistsBiasAdd()
    {
      LwwElementSet<string> set = new LwwElementSet<string>(Bias.Add);
      set.Add("a", 0);
      set.Add("b", 3);
      set.Add("c", 4);
      set.Add("d", 8);
      set.Add("d", 9);
      set.Add("d", 10);
      set.Add("e", 13);
      set.Remove("a", 0);
      set.Remove("b", 1);
      set.Remove("b", 2);
      set.Remove("c", 5);
      set.Remove("d", 9);
      set.Remove("f", 13);

      Assert.True(set.Exists("a"));
      Assert.True(set.Exists("b"));
      Assert.False(set.Exists("c"));
      Assert.True(set.Exists("d"));
      Assert.True(set.Exists("e"));
      Assert.False(set.Exists("f"));
      Assert.False(set.Exists("-1"));
    }

    [Fact]
    public void ExistsBiasRemove()
    {
      LwwElementSet<string> set = new LwwElementSet<string>(Bias.Remove);
      set.Add("a", 0);
      set.Add("b", 3);
      set.Add("c", 4);
      set.Add("d", 8);
      set.Add("d", 9);
      set.Add("d", 10);
      set.Add("e", 13);
      set.Remove("a", 0);
      set.Remove("b", 1);
      set.Remove("b", 2);
      set.Remove("c", 5);
      set.Remove("d", 9);
      set.Remove("f", 13);

      Assert.False(set.Exists("a"));
      Assert.True(set.Exists("b"));
      Assert.False(set.Exists("c"));
      Assert.True(set.Exists("d"));
      Assert.True(set.Exists("e"));
      Assert.False(set.Exists("f"));
      Assert.False(set.Exists("-1"));
    }
  }
}