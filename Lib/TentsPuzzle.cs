using System;
using System.Collections.Generic;
using System.Linq;

namespace Tents
{
    public class TentsPuzzle
    {
        public TentsPuzzle((bool col, int ix, int clue)[] reqClues, Coord[] trees, Coord[] tents)
        {
            Trees = trees;
            Tents = tents;

            Clues = new int?[12];
            foreach (var (col, ix, clue) in reqClues)
                Clues[col ? ix : ix + 6] = clue;
        }

        public int?[] Clues { get; private set; }
        public Coord[] Trees { get; private set; }
        public Coord[] Tents { get; private set; }

        public static TentsPuzzle GenerateTentsPuzzle()
        {
            int w = 6;
            int h = 6;
            int numTrees = w * h / 5;
            startOver:

            var trees = Enumerable.Range(0, w * h).ToList().Shuffle().Take(numTrees).Select(cell => new Coord(w, h, cell)).ToArray();
            trees = trees.OrderBy(tr => tr.OrthogonalNeighbors.Count(neigh => !trees.Contains(neigh))).ToArray();
            IEnumerable<Coord[]> findTentPlacement(Coord[] sofar)
            {
                if (sofar.Length == trees.Length)
                {
                    yield return sofar;
                    yield break;
                }

                foreach (var potentialTent in trees[sofar.Length].OrthogonalNeighbors)
                    if (!trees.Contains(potentialTent) && !sofar.Any(s => s == potentialTent || s.Neighbors.Contains(potentialTent)))
                        foreach (var tents in findTentPlacement(sofar.Insert(sofar.Length, potentialTent)))
                            yield return tents;
            }

            var tents = findTentPlacement(new Coord[0]).FirstOrDefault();
            if (tents == null)
                goto startOver;

            var colClues = Enumerable.Range(0, w).Select(x => (int?)tents.Count(t => t.X == x)).ToArray();
            var rowClues = Enumerable.Range(0, h).Select(y => (int?)tents.Count(t => t.Y == y)).ToArray();

            IEnumerable<Coord[]> solveTents(Coord[] sofar, int?[] cols, int?[] rows)
            {
                if (sofar.Length == trees.Length)
                {
                    yield return sofar;
                    yield break;
                }

                foreach (var potentialTent in trees[sofar.Length].OrthogonalNeighbors)
                    if (!trees.Contains(potentialTent) &&
                            !sofar.Any(s => s == potentialTent || s.Neighbors.Contains(potentialTent)) &&
                            (cols[potentialTent.X] == null || sofar.Count(tent => tent.X == potentialTent.X) < cols[potentialTent.X]) &&
                            (rows[potentialTent.Y] == null || sofar.Count(tent => tent.Y == potentialTent.Y) < rows[potentialTent.Y]))
                        foreach (var t in solveTents(sofar.Insert(sofar.Length, potentialTent), cols, rows))
                            yield return tents;
            }

            (bool col, int ix, int clue)[] reqClues;

            var cluelessSolutions = solveTents(new Coord[0], new int?[w], new int?[h]).Take(2).ToArray();
            if (cluelessSolutions.Length > 1)
            {
                var fullClueSolutions = solveTents(new Coord[0], colClues, rowClues).Take(2).ToArray();
                if (fullClueSolutions.Length > 1)
                    goto startOver;
                var allClues = colClues.Select((c, ix) => (col: true, ix, clue: c.Value)).Concat(rowClues.Select((r, ix) => (col: false, ix, clue: r.Value))).ToArray();
                reqClues = Ut.ReduceRequiredSet(allClues, skipConsistencyTest: false, test: state =>
                    {
                        var colClues = new int?[w];
                        var rowClues = new int?[h];
                        foreach (var (col, ix, clue) in state.SetToTest)
                            (col ? colClues : rowClues)[ix] = clue;
                        return !solveTents(new Coord[0], colClues, rowClues).Skip(1).Any();
                    }).ToArray();
            }
            else
                reqClues = new (bool col, int ix, int clue)[0];
            return new TentsPuzzle(reqClues, trees, tents);
        }
    }
}