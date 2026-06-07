using System;

namespace Moonbreak
{
    public static class FuzzySearch
    {
        // Higher = better match. Returns -1 if no match.
        public static int Score(string query, string candidate)
        {
            if (string.IsNullOrEmpty(query)) { return 0; }

            string queryToLower = query.ToLowerInvariant();
            string candidateToLower = candidate.ToLowerInvariant();

            // Substring match — higher score
            // Example: query "kil" in "killallenemies" → subIndex = 0 → score = 1000
            int subIndex = candidateToLower.IndexOf(queryToLower, StringComparison.Ordinal);
            if (subIndex >= 0)
            {
                return 1000 - subIndex;
            }

            // Fuzzy non-contiguous match — lower score
            // Walk the candidate left to right. Each time a candidate char matches
            // the current query char, advance the query pointer and add a point.
            //
            // Example: query "kle", candidate "killallenemies"
            //   ci=0  c[ci]='k'  q[qi]='k'  → match, qi=1, score=1
            //   ci=1  c[ci]='i'  q[qi]='l'  → no match
            //   ci=2  c[ci]='l'  q[qi]='l'  → match, qi=2, score=2
            //   ci=3  c[ci]='l'  q[qi]='e'  → no match
            //   ...
            //   ci=9  c[ci]='e'  q[qi]='e'  → match, qi=3, score=3
            //   qi == q.Length → done, return 3
            int queryIndex = 0;
            int candidateIndex = 0;
            int score = 0;

            while (candidateIndex < candidateToLower.Length && queryIndex < queryToLower.Length)
            {
                if (candidateToLower[candidateIndex] == queryToLower[queryIndex])
                {
                    score++;
                    queryIndex++;   // advance query pointer on a hit
                }

                candidateIndex++;       // always advance candidate pointer
            }

            // If qi never reached the end, not all query chars were found → no match
            if (queryIndex < queryToLower.Length) { return -1; }
            return score;
        }
    }
}
