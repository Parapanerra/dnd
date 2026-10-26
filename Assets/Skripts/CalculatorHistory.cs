using System;
using System.Collections.Generic;
using UnityEngine;

// Session-only log: scene changes keep it, closing the application clears it.
public static class CalculatorHistory
{
    public readonly struct Entry
    {
        public readonly string Text;
        public readonly Color Color;

        public Entry(string text, Color color)
        {
            Text = text;
            Color = color;
        }
    }

    private static readonly List<Entry> entries = new List<Entry>();
    public static IReadOnlyList<Entry> Entries => entries;
    public static event Action<Entry> Added;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void BeginSession()
    {
        entries.Clear();
        Added = null;
    }

    public static void Add(string text, Color color)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var entry = new Entry(text.Trim(), color);
        entries.Add(entry);
        Added?.Invoke(entry);
    }
}
