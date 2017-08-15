using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Code
{
    class Program
    {
static string input = 
@"9 10
.........
.........
.ferries.
.l.....t.
.o..an.a.
.e...e.f.
.short.f.
.......e.
..called.";

static string square5 = 
@"5 6
brawl
radii
ozone
weber
needs"
;

        static void Main(string[] args)
        {
            var mat = InputToMatrix(square5);
            var dic = LoaDictionary("./dict.txt");

            if(!BoardIsValid(mat,dic))
                throw new Exception("Invalid Initial Board, Aborting");

            var steps = ComputeSteps(mat, dic).Reverse();
            foreach(var s in steps){
                Console.WriteLine("Played :" + s.Item1 );
                //PrintBoard(s.Item2);
            }
        }

//-------------------------Helpers
            static void PrintBoard(Tuple<int,int,char[,]> mat){
            var lines = Enumerable.Range(0,mat.Item1)
                        .Select(i =>
                             Enumerable.Range(0, mat.Item2)
                             .Select(j => mat.Item3[i,j])
                             .Aggregate(string.Empty, (s,c) => s+c)
                        );
            foreach (var str in lines)
                Console.WriteLine(str);
            Console.WriteLine();
        }

        static void PrintSubsets(IEnumerable<List<char>> sets){
            int i=0;
            foreach(var s in sets){
                string elems = s.Aggregate("", (all, c) => all = all +","+ c);
                Console.WriteLine("Set #" + i + " : " + elems);
                i++;
            }
        }

        static void PrintWords(IEnumerable<IEnumerable<Tuple<int,int,char>>> words){
            foreach (var w in words.Select(ExtractWord))
                Console.WriteLine(w);
        }

        static void PrintInvalids(IEnumerable<string> invalids){
            foreach (var w in invalids)
                Console.WriteLine(w +" is invalid");
        }

        public static Tuple<int , int , char[,]> InputToMatrix(string str){
            var lines = str.Split('\n');

            var header = lines[0].Split(' ');
            int rows = Convert.ToInt32(header[1]) -1;
            int cols = Convert.ToInt32(header[0]);

            if (rows % 2 == 0 || cols% 2 == 0)
                throw new Exception("Invalid board dimensions. Board dimension need to be odd");

            var mat = new char[rows,cols];
            for(int i =0; i< rows ; i++)
                for(int j = 0 ; j < cols; j++)
                    mat[i,j] = lines[i+1][j];
            
            return Tuple.Create(rows, cols, mat);
        }

        static HashSet<string> LoaDictionary(string path){
            var dic = new HashSet<string>(); 
            foreach (var l in File.ReadAllLines(path))
                dic.Add(l);
            return dic;
        }
        
        static Tuple<int,int,char[,]> CopyMat(Tuple<int,int,char[,]> mat){
            var result = new char[mat.Item1,mat.Item2];
            Array.Copy(mat.Item3,result, mat.Item1*mat.Item2);
            return Tuple.Create(mat.Item1, mat.Item2, result);
        }

//-------------------------Payload
        static IEnumerable<Tuple<string,Tuple<int,int,char[,]>, Tuple<int,int,char[,]>>> ComputeSteps(Tuple<int,int,char[,]> mat, HashSet<string> dic){
            var mat2 = mat;
            while(CharCount(mat2) > 1)
            {
                var words = Words(GetColumns(mat2).Concat(GetRows(mat2)));
                var next = AlternativeBoards(mat2, words)
                            .Where(b => BoardIsValid(b.Item3,dic))
                            .Aggregate((i,n) =>  n.Item2.Count(c => c=='.') < i.Item2.Count(c => c=='.') ? n : i);

                PrintBoard(next.Item3);

                var result = Tuple.Create(next.Item1, mat2, next.Item3);
                yield return result;
                mat2 = next.Item3;
            }
        }

        static int CharCount(Tuple<int,int,char[,]> mat){
            return Enumerable.Range(0,mat.Item1)
            .SelectMany(i => Enumerable.Range(0, mat.Item2)
                            .Select(j => mat.Item3[i,j])
                        ).Count(c => c != '.');
        }

        static IEnumerable<Tuple<string,string, Tuple<int,int,char[,]>>> AlternativeBoards(Tuple<int,int,char[,]> mat, IEnumerable<IEnumerable<Tuple<int,int,char>>> words){
            var plays = words.Select(w => Tuple.Create(w, ExtractWord(w)))
            .Select(t =>  Tuple.Create(t.Item1, PossiblePlays(t.Item2)));

            return plays.SelectMany(play => play.Item2
                                    .Select( p => Tuple.Create(ExtractWord(play.Item1), p, ApplyPlay(mat, play.Item1, p)))
                                    );
        }

        static Tuple<int,int,char[,]> ApplyPlay(Tuple<int,int,char[,]> mat, IEnumerable<Tuple<int,int,char>> word, string play){
            var result = CopyMat(mat);

            var replacements = Enumerable.Zip(word,play, (w,p) => Tuple.Create(w.Item1,w.Item2,p));
            foreach(var r in replacements) 
                 result.Item3[r.Item1,r.Item2] = r.Item3;
            
            return result;

        }

        static IEnumerable<string> PossiblePlays(string word){
            return Enumerable.Range(0, (1 << word.Length)-1)
                .Select(i => Mask(word, i));
        }

        static string Mask(string str, int mask){
            return Enumerable.Range(0, str.Length)
                .Select(i => (mask & (1 << i)) > 0 ? str[i]:'.')
                .Aggregate("", (s,c)=> s+=c);
        }

        static bool BoardIsValid(Tuple<int , int , char[,]> mat, HashSet<string> dic){
             bool valid = !InvalidWords(mat, dic).Any()
                 && ConnectedSubsets(mat).Count() == 1
                 && CenterLetterPresent(mat);
            return valid;
        }

        static bool CenterLetterPresent(Tuple<int , int , char[,]> mat){
            return mat.Item3[(mat.Item1 - 1)/2,(mat.Item2-1)/2] != '.';
        }

        static IEnumerable<string> InvalidWords(Tuple<int , int , char[,]> mat, HashSet<string> dic){        
            var lines = GetRows(mat)
                .Concat(GetColumns(mat));

            var words = Words(lines).Select(ExtractWord);

            return words.Where(w => !dic.Contains(w));
        }

        static IEnumerable<List<char>> ConnectedSubsets(Tuple<int , int , char[,]> mat){
            
            int currentId = 0;
            var groupAtPos = new Dictionary<Tuple<int,int>,int>();
            var groups = new Dictionary<int, List<Tuple<int,int>>>();

            void Absorb(int id, int into){
                foreach (var t in groups[id]){
                    groupAtPos[t] = into;
                    groups[into].Add(t);
                }
                groups.Remove(id);

            }

            void Add(int id, Tuple<int,int> item){
                if(!groups.ContainsKey(id))
                    groups.Add(id, new List<Tuple<int,int>>());
                groups[id].Add(item);
                groupAtPos.Add(item, id);
            }

            for(int i=0; i < mat.Item1 ; i++ ){
                currentId ++;
                for (int j=0; j < mat.Item2 ; j++){
                    var current = Tuple.Create(i,j);
                    var above = Tuple.Create(i-1, j);
                  
                    if(mat.Item3[i,j] == '.')
                        currentId ++;
                    else{
                        Add(currentId, current);    

                        if(groupAtPos.ContainsKey(above)){
                            int otherId = groupAtPos[above];
                            if(otherId != currentId){
                                Absorb(otherId,currentId);
                            }              
                        }
                    }
                }
            }
            return groups.Values.Select(
                l => l.Select(t =>mat.Item3[t.Item1,t.Item2]).ToList()); 
        }

        static string ExtractWord(IEnumerable<Tuple<int,int,char>> word){
            return word.Aggregate(string.Empty, (s,c) => s+=c.Item3);
        }

        static IEnumerable<IEnumerable<Tuple<int,int, char>>> Words(IEnumerable<IEnumerable<Tuple<int,int,char>>> lines){
            foreach(var l in lines){
                var word = new List<Tuple<int,int,char>>();    
                foreach(var c in l){
                    if(c.Item3 == '.'){
                        if(word.Count >= 2)
                            yield return word;
                        word.Clear();
                    }
                    else
                        word.Add(c);
                }
                if(word.Count >= 2)
                    yield return word;
            }
        }

        static IEnumerable<IEnumerable<Tuple<int,int,char>>> GetRows(Tuple<int , int , char[,]> mat){
            return Enumerable.Range(0,mat.Item1)
            .Select(i => Enumerable.Range(0,mat.Item2)
                        .Select(j => Tuple.Create(i,j,mat.Item3[i,j]))
                    );
        }      
        
        static IEnumerable<IEnumerable<Tuple<int,int,char>>> GetColumns(Tuple<int , int , char[,]> mat){
            return Enumerable.Range(0,mat.Item2)
            .Select(j => Enumerable.Range(0,mat.Item1)
                        .Select(i => Tuple.Create(i,j,mat.Item3[i,j]))
                    );
        }     
    }
}
