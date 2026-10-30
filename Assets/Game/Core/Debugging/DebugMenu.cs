using System;
using System.Collections.Generic;
using UnityEngine;

namespace TentacleVania.Core.Debugging
{
    public enum DebugEntryKind
    {
        Button,
        Toggle,
        Slider,
        Value,
        Choice,
    }

    public sealed class DebugEntry
    {
        public DebugEntryKind Kind;
        public string Tab;
        public string Label;
        public Action OnClick;
        public Func<bool> GetBool;
        public Action<bool> SetBool;
        public Func<float> GetFloat;
        public Action<float> SetFloat;
        public float Min;
        public float Max;
        public Func<string> GetText;
        public string[] Choices;
        public Action<int> OnChoice;
    }

    public static class DebugMenu
    {
        public const string Game = "Game";
        public const string Creature = "Creature";
        public const string Combat = "Combat";
        public const string Enemies = "Enemies";
        public const string Camera = "Camera";

        private static readonly string[] PreferredOrder = { Game, Creature, Combat, Enemies, Camera };
        private static readonly List<string> TabList = new();
        private static readonly Dictionary<string, List<DebugEntry>> Entries = new();

        private static bool _open;

        public static event Action<bool> OpenChanged;

        public static int Version { get; private set; }
        public static IReadOnlyList<string> Tabs => TabList;

        public static bool IsOpen
        {
            get => _open;
            set
            {
                if (_open == value)
                    return;
                _open = value;
                OpenChanged?.Invoke(value);
            }
        }

        public static IReadOnlyList<DebugEntry> EntriesFor(string tab) =>
            Entries.TryGetValue(tab, out var list) ? list : Array.Empty<DebugEntry>();

        public static DebugEntry Button(string tab, string label, Action onClick) =>
            Add(new() { Kind = DebugEntryKind.Button, Tab = tab, Label = label, OnClick = onClick });

        public static DebugEntry Toggle(string tab, string label, Func<bool> get, Action<bool> set) =>
            Add(new() { Kind = DebugEntryKind.Toggle, Tab = tab, Label = label, GetBool = get, SetBool = set });

        public static DebugEntry Slider(string tab, string label, float min, float max, Func<float> get, Action<float> set) =>
            Add(new() { Kind = DebugEntryKind.Slider, Tab = tab, Label = label, Min = min, Max = max, GetFloat = get, SetFloat = set });

        public static DebugEntry Value(string tab, string label, Func<string> get) =>
            Add(new() { Kind = DebugEntryKind.Value, Tab = tab, Label = label, GetText = get });

        public static DebugEntry Choice(string tab, string label, string[] choices, Action<int> onChoice) =>
            Add(new() { Kind = DebugEntryKind.Choice, Tab = tab, Label = label, Choices = choices, OnChoice = onChoice });

        public static void Remove(DebugEntry entry)
        {
            if (entry != null && Entries.TryGetValue(entry.Tab, out var list) && list.Remove(entry))
                Version++;
        }

        private static DebugEntry Add(DebugEntry entry)
        {
            if (!Entries.TryGetValue(entry.Tab, out var list))
            {
                Entries[entry.Tab] = list = new();
                InsertTab(entry.Tab);
            }
            list.Add(entry);
            Version++;
            return entry;
        }

        private static void InsertTab(string tab)
        {
            var rank = Array.IndexOf(PreferredOrder, tab);
            if (rank < 0)
            {
                TabList.Add(tab);
                return;
            }
            var index = 0;
            while (index < TabList.Count)
            {
                var other = Array.IndexOf(PreferredOrder, TabList[index]);
                if (other < 0 || other > rank)
                    break;
                index++;
            }
            TabList.Insert(index, tab);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Clear()
        {
            TabList.Clear();
            Entries.Clear();
            _open = false;
            OpenChanged = null;
            Version++;
        }

        public static void ClearForTests() => Clear();
    }
}
