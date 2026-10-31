namespace TentacleVania.Core.Progression
{
    public sealed class RespecDraft
    {
        private readonly int[] _values = new int[4];

        public int Total { get; private set; }
        public int Unassigned => Total - (_values[0] + _values[1] + _values[2] + _values[3]);
        public bool IsComplete => Unassigned == 0;

        public void Begin(int total)
        {
            Total = total;
            for (var i = 0; i < _values.Length; i++)
                _values[i] = StatRules.StartValue;
        }

        public int Get(StatKind kind) => _values[(int)kind];

        public bool Add(StatKind kind)
        {
            if (Unassigned <= 0 || _values[(int)kind] >= StatRules.MaxValue)
                return false;
            _values[(int)kind]++;
            return true;
        }

        public bool Remove(StatKind kind)
        {
            if (_values[(int)kind] <= StatRules.StartValue)
                return false;
            _values[(int)kind]--;
            return true;
        }

        public bool Apply() => IsComplete && Stats.Respec(_values[0], _values[1], _values[2], _values[3]);
    }
}
