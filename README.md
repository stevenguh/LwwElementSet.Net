# LWW-Element-Set (Last-Write-Wins-Element-Set)
This is an implementation of the LWW element set in C#. 

At a high level, it provides the following APIs:
- `Add(T element, long timeStamp)`
- `Remove(T element, long timeStamp)`
- `Exists(T element)`
- `Merge(LwwElementSet<T> set)`
- `ToList()`

The choice of timestamp is curial to the correctness of the data structure because it is used to provide the ordering of the events. Using real time clock maybe fine in a single computer. For example, vanilla `DateTime.Now.Ticks` may be used as timestamp for when events are at around 30ms apart. The exact timing varies on different computer.

The following code is a simple example that counts the ticks up when the `DateTime.Now.Ticks` returns the same ticks since last accessed. It could be used when one wants to provide ordered timestamp when the events are microsecond apart. Noted that the `OrderedTime` has not yet been tested.

    public static class OrderedTime
    {
        private static long _ticks = 0;
        private static object _lock = new object();
        public static long Ticks
        {
            get
            {
                lock (_lock)
                {
                    long currentTicks = DateTime.Now.Ticks;
                    return _ticks = (currentTicks > _ticks) ? currentTicks : _ticks + 1;
                }
            }
        }
    }

Below are all possible combinations of add and remove operations. A(elements,...) is the state of the add set. R(elements...) is the state of the remove set. An element is a tuple with (value, timestamp). add(element) and remove(element) are the operations.

| Original state | Operation   | Resulting state |
|----------------|-------------|-----------------|
| A(a,1) R()     | add(a,0)    | A(a,1) R()      |
| A(a,1) R()     | add(a,1)    | A(a,1) R()      |
| A(a,1) R()     | add(a,2)    | A(a,2) R()      |
| A() R(a,1)     | add(a,0)    | A(a,0) R(a,1)   |
| A() R(a,1)     | add(a,1)    | A(a,1) R(a,1)   |
| A() R(a,1)     | add(a,2)    | A(a,2) R(a,1)   |
| A() R(a,1)     | remove(a,0) | A() R(a,1)      |
| A() R(a,1)     | remove(a,1) | A() R(a,1)      |
| A() R(a,1)     | remove(a,2) | A() R(a,2)      |
| A(a,1) R()     | remove(a,0) | A(a,1) R(a,0)   |
| A(a,1) R()     | remove(a,1) | A(a,1) R(a,1)   |
| A(a,1) R()     | remove(a,2) | A(a,1) R(a,2)   |

The table above is different than the one in [Roshi](https://github.com/soundcloud/roshi).

## Example
    LwwElementSet<string> set = new LwwElementSet<string>(Bias.Add);
    set.Add("a", 1);
    set.Add("a", 2);
    set.Remove("a", 0);

    var list = set.ToList(); // ["a"]


## Reference
- [LWW-Element-Set](https://en.wikipedia.org/wiki/Conflict-free_replicated_data_type#LWW-Element-Set_(Last-Write-Wins-Element-Set)) section of the Conflict-free replicated data type Wikipedia page
- [CRDT notes by Paul Frazee](https://github.com/pfrazee/crdt_notes)
- [lww-element-set](https://github.com/junjizhi/lww-element-set), an Python implementation of Last-Writer-Wins Element Set)
- [Roshi](https://github.com/soundcloud/roshi) from SoundCloud
- [go-lww-element-set](https://github.com/cedricblondeau/go-lww-element-set), a Last-Writer-Wins (LWW) CRDT implementation with Redis support in Go
- [Clock Synchronization](http://www.krzyzanowski.org/rutgers/notes/pdf/06-clocks.pdf) by Paul Krzyzanowski
- [Precision and accuracy of DateTime](https://blogs.msdn.microsoft.com/ericlippert/2010/04/08/precision-and-accuracy-of-datetime/)