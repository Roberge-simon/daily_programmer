using System;

namespace SubsetCellAutomaton
{
    class Program
    {
        static void Main(string[] args)
        {
            var rules = Tuple.Create(27, 5, 3);
            var dimensions = Tuple.Create(128, 128);
            SubsetsAutomaton.Run(dimensions, 1000,64, 2, rules);
        }
    }
}
