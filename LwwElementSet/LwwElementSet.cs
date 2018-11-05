using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("LwwElementSet.Tests")]

namespace LwwElementSet.Net
{
  public enum Bias
  {
    Add = 0,
    Remove = 1,
  }

  public class LwwElementSet<T> : IEnumerable<T>
  {
    private readonly object _lock = new object();
    private Bias _bias;
    internal Dictionary<T, long> _adds;
    internal Dictionary<T, long> _removes;

    public LwwElementSet(Bias bias)
    {
      _bias = bias;
      _adds = new Dictionary<T, long>();
      _removes = new Dictionary<T, long>();
    }

    public void Add(T element, long timeStamp)
    {
      lock (_lock)
      {
        bool exists = _adds.TryGetValue(element, out long oldTimeStamp);
        if (!exists || oldTimeStamp < timeStamp)
        {
          _adds[element] = timeStamp;
        }
      }
    }

    public void Remove(T element, long timeStamp)
    {
      lock (_lock)
      {
        bool exists = _removes.TryGetValue(element, out long oldTimeStamp);
        if (!exists || oldTimeStamp < timeStamp)
        {
          _removes[element] = timeStamp;
        }
      }
    }

    public bool Exists(T element)
    {
      lock (_lock)
      {
        bool InAdds = _adds.ContainsKey(element);
        bool InRemoves = _removes.ContainsKey(element);

        if (InAdds && InRemoves)
        {
          long addTime = _adds[element];
          long removeTime = _removes[element];
          if (addTime == removeTime)
          {
            return _bias == Bias.Add;
          }
          else if (addTime > removeTime)
          {
            return true;
          }
          else
          {
            return false;
          }
        }
        else if (InAdds && !InRemoves)
        {
          return true;
        }
        else // !InAdds
        {
          return false;
        }
      }
    }

    public void Merge(LwwElementSet<T> set)
    {
      lock (_lock)
      {
        if (_bias != set._bias)
        {
          throw new InvalidOperationException("Two different biases");
        }

        foreach (var kv in set._adds)
        {
          this.Add(kv.Key, kv.Value);
        }

        foreach (var kv in set._removes)
        {
          this.Remove(kv.Key, kv.Value);
        }
      }
    }

    public List<T> ToList()
    {
      lock (_lock)
      {
        return _adds.Where(kv =>
        {
          T element = kv.Key;
          long timeStamp = kv.Value;

          if (_removes.TryGetValue(element, out long removeTimeStamp))
          {
            if (timeStamp > removeTimeStamp)
            {
              // timeStamp of add the greater, exists.
              return true;
            }
            else if (timeStamp == removeTimeStamp)
            {
              // timeStamp are equals, resolve with bias.
              return _bias == Bias.Add;
            }
            else
            {
              // timeStamps of remove is greater, not exist.
              return false;
            }
          }
          else
          {
            // Not in remove, element exists.
            return true;
          }
        }).Select(kv => kv.Key).ToList();
      }
    }

    public IEnumerator<T> GetEnumerator()
    {
      return this.ToList().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return this.GetEnumerator();
    }
  }
}
