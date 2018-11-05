using System;
using System.Linq;
using LwwElementSet.Net;
using Xunit;

namespace LwwElementSet.Tests
{
  /// <summary>
  /// Test the CRDT properties
  /// | Original state | Operation   | Resulting state | Set   | Test Method Name          |
  /// |----------------|-------------|-----------------|-------|---------------------------|
  /// | A(), R()       | add(a,1)    | A(a,1) R()      | { a } | AddEmpty                  |
  /// | A(a,1) R()     | add(a,0)    | A(a,1) R()      | { a } | AddOld                    |
  /// | A(a,1) R()     | add(a,1)    | A(a,1) R()      | { a } | AddSame                   |
  /// | A(a,1) R()     | add(a,2)    | A(a,2) R()      | { a } | AddNew                    |
  /// | A() R(a,1)     | add(a,0)    | A(a,0) R(a,1)   | {   } | AddOldAfterNewRemove      |
  /// | A() R(a,1)     | add(a,1)    | A(a,1) R(a,1)   | bias  | AddSameAfterSameRemove    |
  /// | A() R(a,1)     | add(a,2)    | A(a,2) R(a,1)   | { a } | AddNewAfterOldRemove      |
  /// | A(), R()       | remove(a,1) | A() R(a,1)      | {   } | RemoveEmpty               |
  /// | A() R(a,1)     | remove(a,0) | A() R(a,1)      | {   } | RemoveOld                 |
  /// | A() R(a,1)     | remove(a,1) | A() R(a,1)      | {   } | RemoveSame                |
  /// | A() R(a,1)     | remove(a,2) | A() R(a,2)      | {   } | RemoveNew                 |
  /// | A(a,1) R()     | remove(a,0) | A(a,1) R(a,0)   | { a } | RemoveOldAfterNewAdd      |
  /// | A(a,1) R()     | remove(a,1) | A(a,1) R(a,1)   | bias  | RemoveSameAfterSameAdd    |
  /// | A(a,1) R()     | remove(a,2) | A(a,1) R(a,2)   | {   } | RemoveNewAfterOldAdd      |
  /// </summary>
  public class LwwElementSetCrdtTests
  {
    /// <summary>
    /// A(), R() + add(a, 1) = A(a, 1), R().
    /// </summary>
    [Fact]
    public void AddEmpty()
    {
      LwwElementSet<string> set = new LwwElementSet<string>(Bias.Add);
      Assert.Empty(set);

      string item1 = "Test";
      set.Add(item1, 1);

      Assert.Single(set._adds);
      Assert.Equal(1, set._adds[item1]);
      Assert.Empty(set._removes);

      Assert.Single(set);
      Assert.Equal(item1, set.First());
    }

    /// <summary>
    /// A(a, 1), R() + add(a, 0) = A(a, 1), R().
    /// </summary>
    [Fact]
    public void AddOld()
    {
      LwwElementSet<string> set = new LwwElementSet<string>(Bias.Add);
      string item1 = "Test";
      set.Add(item1, 1);
      set.Add(item1, 0);

      Assert.Single(set._adds);
      Assert.Equal(1, set._adds[item1]);
      Assert.Empty(set._removes);

      Assert.Single(set);
      Assert.Equal(item1, set.First());
    }

    /// <summary>
    /// A(a, 1), R() + add(a, 1) = A(a, 1), R().
    /// </summary>
    [Fact]
    public void AddSame()
    {
      LwwElementSet<string> set = new LwwElementSet<string>(Bias.Add);
      string item1 = "Test";
      set.Add(item1, 1);
      set.Add(item1, 1);

      Assert.Single(set._adds);
      Assert.Equal(1, set._adds[item1]);
      Assert.Empty(set._removes);

      Assert.Single(set);
      Assert.Equal(item1, set.First());
    }

    /// <summary>
    /// A(a, 1), R() + add(a, 2) = A(a, 2), R().
    /// </summary>
    [Fact]
    public void AddNew()
    {
      LwwElementSet<string> set = new LwwElementSet<string>(Bias.Add);
      string item1 = "Test";
      set.Add(item1, 1);
      set.Add(item1, 2);

      Assert.Single(set._adds);
      Assert.Equal(2, set._adds[item1]);
      Assert.Empty(set._removes);

      Assert.Single(set);
      Assert.Equal(item1, set.First());
    }

    /// <summary>
    /// A(), R(a, 1) + add(a, 0) = A(a, 0), R(a, 1)
    /// </summary>
    [Fact]
    public void AddOldAfterNewRemove()
    {
      LwwElementSet<string> set = new LwwElementSet<string>(Bias.Add);
      string item1 = "Test";
      set.Remove(item1, 1);
      set.Add(item1, 0);

      Assert.Single(set._adds);
      Assert.Single(set._removes);
      Assert.Equal(0, set._adds[item1]);
      Assert.Equal(1, set._removes[item1]);

      Assert.Empty(set);
    }

    /// <summary>
    /// A(), R(a, 1) + add(a, 1) = A(a, 1), R(a, 1)
    /// </summary>
    [Fact]
    public void AddSameAfterSameRemove()
    {
      LwwElementSet<string> set = new LwwElementSet<string>(Bias.Add);
      string item1 = "Test";
      set.Remove(item1, 1);
      set.Add(item1, 1);

      Assert.Single(set._adds);
      Assert.Single(set._removes);
      Assert.Equal(1, set._adds[item1]);
      Assert.Equal(1, set._removes[item1]);

      // Bias toward add
      Assert.Single(set);
      Assert.Equal(item1, set.First());

      // Test the other bias
      set = new LwwElementSet<string>(Bias.Remove);
      set.Remove(item1, 1);
      set.Add(item1, 1);
      Assert.Empty(set);
    }

    /// <summary>
    /// A(), R(a, 1) + add(a, 2) = A(a, 2), R(a, 1)
    /// </summary>
    [Fact]
    public void AddNewAfterOldRemove()
    {
      LwwElementSet<string> set = new LwwElementSet<string>(Bias.Add);
      string item1 = "Test";
      set.Remove(item1, 1);
      set.Add(item1, 2);

      Assert.Single(set._adds);
      Assert.Single(set._removes);
      Assert.Equal(2, set._adds[item1]);
      Assert.Equal(1, set._removes[item1]);

      Assert.Single(set);
      Assert.Equal(item1, set.First());
    }

    /// <summary>
    /// A(), R() + remove(a, 1) = A(), R(a, 1).
    /// </summary>
    [Fact]
    public void RemoveEmpty()
    {
      LwwElementSet<string> set = new LwwElementSet<string>(Bias.Add);
      Assert.Empty(set);

      string item1 = "Test";
      set.Remove(item1, 1);

      Assert.Single(set._removes);
      Assert.Equal(1, set._removes[item1]);
      Assert.Empty(set._adds);

      Assert.Empty(set);
    }

    /// <summary>
    /// A() R(a,1) + remove(a,0) = A() R(a,1)
    /// </summary>
    [Fact]
    public void RemoveOld()
    {
      LwwElementSet<string> set = new LwwElementSet<string>(Bias.Add);
      string item1 = "Test";
      set.Remove(item1, 1);
      set.Remove(item1, 0);

      Assert.Single(set._removes);
      Assert.Equal(1, set._removes[item1]);
      Assert.Empty(set._adds);

      Assert.Empty(set);
    }

    /// <summary>
    /// A(), R(a, 1) + remove(a, 1) = A(), R(a, 1).
    /// </summary>
    [Fact]
    public void RemoveSame()
    {
      LwwElementSet<string> set = new LwwElementSet<string>(Bias.Add);
      string item1 = "Test";
      set.Remove(item1, 1);
      set.Remove(item1, 1);

      Assert.Single(set._removes);
      Assert.Equal(1, set._removes[item1]);
      Assert.Empty(set._adds);

      Assert.Empty(set);
    }

    /// <summary>
    /// A(), R(a, 1) + remove(a, 2) = A(), R(a, 2).
    /// </summary>
    [Fact]
    public void RemoveNew()
    {
      LwwElementSet<string> set = new LwwElementSet<string>(Bias.Add);
      string item1 = "Test";
      set.Remove(item1, 1);
      set.Remove(item1, 2);

      Assert.Single(set._removes);
      Assert.Equal(2, set._removes[item1]);
      Assert.Empty(set._adds);

      Assert.Empty(set);
    }

    /// <summary>
    /// A(a, 1), R() + remove(a, 0) = A(a, 1), R(a, 0)
    /// </summary>
    [Fact]
    public void RemoveOldAfterNewAdd()
    {
      LwwElementSet<string> set = new LwwElementSet<string>(Bias.Add);
      string item1 = "Test";
      set.Add(item1, 1);
      set.Remove(item1, 0);

      Assert.Single(set._adds);
      Assert.Single(set._removes);
      Assert.Equal(1, set._adds[item1]);
      Assert.Equal(0, set._removes[item1]);

      Assert.Single(set);
      Assert.Equal(item1, set.First());
    }

    /// <summary>
    /// A(a, 1), R() + remove(a, 1) = A(a, 1), R(a, 1)
    /// </summary>
    [Fact]
    public void RemoveSameAfterSameAdd()
    {
      LwwElementSet<string> set = new LwwElementSet<string>(Bias.Add);
      string item1 = "Test";
      set.Add(item1, 1);
      set.Remove(item1, 1);

      Assert.Single(set._adds);
      Assert.Single(set._removes);
      Assert.Equal(1, set._adds[item1]);
      Assert.Equal(1, set._removes[item1]);

      // Bias toward add
      Assert.Single(set);
      Assert.Equal(item1, set.First());

      // Test the other bias
      set = new LwwElementSet<string>(Bias.Remove);
      set.Add(item1, 1);
      set.Remove(item1, 1);
      Assert.Empty(set);
    }

    /// <summary>
    /// A(a, 1), R() + remove(a, 2) = A(a, 1), R(a, 2)
    /// </summary>
    [Fact]
    public void RemoveNewAfterOldAdd()
    {
      LwwElementSet<string> set = new LwwElementSet<string>(Bias.Add);
      string item1 = "Test";
      set.Add(item1, 1);
      set.Remove(item1, 2);

      Assert.Single(set._adds);
      Assert.Single(set._removes);
      Assert.Equal(1, set._adds[item1]);
      Assert.Equal(2, set._removes[item1]);

      Assert.Empty(set);
    }
  }
}
