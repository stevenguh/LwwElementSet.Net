using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LwwElementSet.Net;
using Xunit;

namespace LwwElementSet.Tests
{
  // See https://stackoverflow.com/a/1262619/2563765
  public static class ListExtensions
  {
    public static void Shuffle<T>(this IList<T> list)
    {
      int n = list.Count;
      while (n > 1)
      {
        n--;
        int k = ThreadSafeRandom.RandomOfCurrentThread.Next(n + 1);
        T value = list[k];
        list[k] = list[n];
        list[n] = value;
      }
    }
  }
  public static class ThreadSafeRandom
  {
    [ThreadStatic] private static Random Local;

    public static Random RandomOfCurrentThread
    {
      get { return Local ?? (Local = new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId))); }
    }
  }
}