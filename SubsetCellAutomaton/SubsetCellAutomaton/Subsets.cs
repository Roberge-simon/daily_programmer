using System.Linq;
using System.Collections.Generic;
using System;
using System.IO;
using System.Drawing;
using System.Diagnostics;

public class SubsetsAutomaton{

    private static Color[] Palette(int range)
    {
        var d = new Color[range];
        int colorGap = (256 / range);
        foreach (var i in Enumerable.Range(0, range))
            d[i] = Color.FromArgb(i * colorGap , i * colorGap, i * colorGap);
        return d;
    }

    private static IEnumerable<Tuple<int,int>> Offsets(){
        var offsets = new List<int>(){-1,0,1};
        return Cartesian(offsets, offsets)
        .Where(t => !t.Equals(Tuple.Create(0, 0)));
    }

    private static IEnumerable<Tuple<int,int>[]> Subsets(){
        var offsets = Offsets().ToList();
        return Enumerable.Range(0, (1 << 8)-1)
            .Select(i => Pick(offsets, i, 8).ToArray());
    }

    private static IEnumerable<Tuple<int,int>> Pick(IEnumerable<Tuple<int,int>> elems, int mask, int count){
                
        return Enumerable.Range(0, count)
            .Where(i => (mask & (1 << i)) > 0)
            .SelectMany(i => AsList(elems.ElementAt(i)));
    }

    //using good old for loops because this is the performance bottleneck.
    private static bool TestSum2(Tuple<int, int>[][] subsets, int[,] board, int x, int y, int target)
    {
        for (int i =0; i < subsets.Length; i++)
        {
            int sum = 0;
            for (int j = 0; j < subsets[i].Length; j++)
            {
                sum += board[x + subsets[i][j].Item1, y + subsets[i][j].Item2];
            }
            if (sum == target)
                return true;
        }
        return false;
    }

    private static void Initialize(Tuple<int,int> dimensions, int range, ref int[,] board){
        var r = new Random();
        foreach (var p in CrossRange(dimensions,1))
            board[p.Item1, p.Item2] = r.Next() % range;
    }

    private static void Next(Tuple<int,int>[][] subsets, int[,] board, Tuple<int,int> dimensions, int range, Tuple<int,int,int> rules, ref int[,] nextGen){
        foreach(var p in CrossRange(dimensions,1))
        {
            bool satisfies = TestSum2(subsets, board, p.Item1, p.Item2, rules.Item1);
            var nextValue = board[p.Item1,p.Item2] + (satisfies ? rules.Item2 : -rules.Item3);

            var clamped =   nextValue > range-1  ? range - 1
                            : nextValue < 0       ? 0
                                                    : nextValue;
            nextGen[p.Item1, p.Item2] = clamped;
        }
    }


    private static IEnumerable<int[,]> Automaton(Tuple<int,int> dimensions,int range, Tuple<int,int,int> rules){
        var subsets = Subsets().ToArray();
    
        var board = new int[dimensions.Item1 + 2, dimensions.Item2 + 2];
        var nextBoard = new int[dimensions.Item1 + 2, dimensions.Item2 + 2];

        Initialize(dimensions, range, ref board);

        var fromStart = new Stopwatch();
        var sinceLastGen = new Stopwatch();

        fromStart.Start();
        int generation = 0;
        
        while (true)
        {
            sinceLastGen.Start();
            yield return board;
            Next(subsets, board, dimensions, range, rules, ref nextBoard);
            Swap(ref board, ref nextBoard);
            
            Console.WriteLine("Generation " + generation + " Computed in " + sinceLastGen.ElapsedMilliseconds + " Total Run Time " + fromStart.ElapsedMilliseconds);
            generation++;
            sinceLastGen.Reset();
        }             
    }

    public static void Run(Tuple<int,int> dimensions, int frames, int range, int scale, Tuple<int,int,int> rules)
    {
        var automaton = Automaton(dimensions, range, rules);
        CreateGif(automaton.Take(frames), b => CreateImage(b, dimensions, scale, Palette(range)));
    }

    private static void CreateGif(IEnumerable<int[,]> boards, Func<int[,],Image> BoardToImage){
     
        var stream = new FileStream("./test.gif", FileMode.OpenOrCreate);
        var gifWriter = new GifWriter(stream);

        foreach(var b in boards)
            gifWriter.WriteFrame(BoardToImage(b), 100);

        gifWriter.Dispose();
        stream.Dispose();
    }

    private static Image CreateImage(int[,] board, Tuple<int,int> dimension, int scale, Color[] palette){
        var img = new Bitmap(dimension.Item1 * scale ,dimension.Item2 * scale);
        foreach (var p in CrossRange(dimension,1))
            foreach(var offset in CrossRange(Tuple.Create(scale,scale),0))
                img.SetPixel((p.Item1-1)*scale + offset.Item1, (p.Item2-1)*scale + offset.Item2, palette[board[p.Item1,p.Item2]]);
        return img;
    }

    private static IEnumerable<Tuple<T1,T2>> Cartesian<T1, T2>(IEnumerable<T1> first, IEnumerable<T2> second)
    {
        return first.SelectMany(t1 => second.Select(t2 => Tuple.Create(t1, t2)));
    }

    private static IEnumerable<Tuple<int,int>> CrossRange(Tuple<int,int> dimensions, int startingIndex)
    {
        return Cartesian(Enumerable.Range(startingIndex, dimensions.Item1), Enumerable.Range(startingIndex, dimensions.Item2));
    }

    private static List<T> AsList<T> (T item) { return new List<T> { item }; }
    private static void Swap<T>(ref T t1, ref T t2)
    {
        T _t = t1;
        t1 = t2;
        t2 = t1;
    }

}

