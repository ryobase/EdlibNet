using System;
using System.Runtime.CompilerServices;
using System.Text;
using EdlibNet.Structs;

namespace EdlibNet
{
    /// <summary>
    /// .NET implementation of Edlib, a lightweight, super fast sequence comparison library.
    ///
    /// MIT license
    /// </summary>
    /// <remarks>
    /// This library may not be as fast or lightweight as the original implementation due to heavily reliance
    /// on array as a direct substitute for pointers.
    /// </remarks>
    public static class Edlib
    {
        /// <summary>
        /// Size of ulong in bits
        /// </summary>
        private static int WordSize { get; } = sizeof(ulong) * 8;
        
        /// <summary>
        /// One in unsigned 64-bit integer.
        /// </summary>
        private const ulong Word1 = 1;
        
        /// <summary>
        /// 100..00
        /// </summary>
        private static ulong HighBitMask { get; } = Word1 << (WordSize - 1);
        
        /// <summary>
        /// Maximum value of byte
        /// </summary>
        private const int MaxUnsignedChar = 255;
        
        /// <summary>
        /// Main Edlib method.
        /// </summary>
        /// <param name="originalQuery">String a</param>
        /// <param name="originalTarget">String b</param>
        /// <param name="config">Configuration</param>
        /// <returns>Result of the alignment</returns>
        public static AlignmentResult Align(string originalQuery, string originalTarget, AlignmentConfig config)
        {
            // NOTE: Is other mode useful? If so, why?
            if (config.Mode == AlignmentMode.Hw || config.Mode == AlignmentMode.Shw)
                throw new NotImplementedException();

            AlignmentResult result = new AlignmentResult
            {
                Status = (int) AlignmentStatus.Ok,
                EditDistance = -1,
                EndLocations = null,
                StartingLocations = null,
                NumberOfLocations = 0,
                Alignment = null,
                AlignmentLength = 0,
                AlphabetLength = 0,
            };
            
            // Transform sequences and recognize alphabet
            byte[] query, target;
            string alphabet = TransformSequences(originalQuery, originalTarget, out query, out target);
            result.AlphabetLength = alphabet.Length;
            
            // Initialization
            int queryLength = originalQuery.Length;
            int targetLength = originalTarget.Length;
            int maxBlocks = CeilDivision(queryLength, WordSize);
            int w = maxBlocks * WordSize - queryLength;
            EqualityDefinition equalityDefinition = new EqualityDefinition(alphabet, config.AdditionalEqualities,
                config.AdditionalEqualitiesLength);
            ulong[] peq = BuildPeq(alphabet.Length, query, queryLength, ref equalityDefinition);
            
            // Main Calculation
            int positionNw = -1;
            AlignmentData alignmentData = new AlignmentData();
            bool dynamicK = false;
            int k = config.K;
            if (k < 0) // If valid k is not given, auto-adjust k until solution is found.
            {
                dynamicK = true;
                k = WordSize; // Gives better results than smaller k.
            }

            do
            {
                MyersEditDistanceNw(peq, w, maxBlocks, queryLength, target, targetLength, k, false, -1,
                    ref result.EditDistance, ref positionNw, ref alignmentData);
                k *= 2;
            } while (dynamicK && result.EditDistance == -1);

            // NOTE: Do we need this block code? We only care about editing distance
            // Since we only care about the editing distance score, this is where we stop
            if (result.EditDistance >= 0)
            {
                // If NW mode, set end location explicitly.
                if (config.Mode == AlignmentMode.Nw)
                {
                    result.EndLocations = new int[sizeof(int) * 1];
                    result.EndLocations[0] = targetLength - 1;
                    result.NumberOfLocations = 1;
                }

//                if (!(config.Task == AlignmentTask.TaskLocate || config.Task == AlignmentTask.TaskPath))
//                {
//                    for (int i = 0; i < result.NumberOfLocations; i++) {
//                        result.StartingLocations[i] = 0;
//                    }
//                }
            }
            
            return result;
        }

        /// <summary>
        /// Takes char query and char target, recognizes alphabet and transforms them into unsigned char sequences
        /// where elements in sequences are not any more letters of alphabet, but their index in alphabet.
        /// Most of internal edlib functions expect such transformed sequences.
        /// This function will allocate queryTransformed and targetTransformed, so make sure to free them when done.
        /// Example:
        ///   Original sequences: "ACT" and "CGT".
        ///   Alphabet would be recognized as "ACTG". Alphabet length = 4.
        ///  Transformed sequences: [0, 1, 2] and [1, 3, 2].
        /// </summary>
        /// <param name="oq">Original base string.</param>
        /// <param name="ot">Original target string.</param>
        /// <param name="query">It will contain values in range [0, alphabet length - 1].</param>
        /// <param name="target">It will contain values in range [0, alphabet length - 1].</param>
        /// <returns>
        /// Alphabet as a string of unique characters, where index of each character is its value in transformed 
        /// sequences.
        /// </returns>
        private static string TransformSequences(string oq, string ot, out byte[] query, out byte[] target)
        {
            query = new byte[sizeof(byte) * oq.Length];
            target = new byte[sizeof(byte) * ot.Length];
            
            byte[] letterIndice = new byte[MaxUnsignedChar + 1];
            bool[] inAlphabet = new bool[MaxUnsignedChar + 1];
            StringBuilder alphabet = new StringBuilder();

            for (int i = 0; i < oq.Length; i++)
            {
                char c = oq[i];
                if (!inAlphabet[c])
                {
                    inAlphabet[c] = true;
                    letterIndice[c] = (byte)alphabet.Length;
                    alphabet.Append(oq[i]);
                }

                query[i] = letterIndice[c];
            }
            for (int i = 0; i < ot.Length; i++)
            {
                char c = ot[i];
                if (!inAlphabet[c])
                {
                    inAlphabet[c] = true;
                    letterIndice[c] = (byte)alphabet.Length;
                    alphabet.Append(ot[i]);
                }

                target[i] = letterIndice[c];
            }

            return alphabet.ToString();
        }

        /// <summary>
        /// Build Peq table for given query and alphabet.
        /// Peq is table of dimensions alphabetLength+1 x maxNumBlocks.
        /// Bit i of Peq[s * maxNumBlocks + b] is 1 if i-th symbol from block b of query equals symbol s, otherwise it is 0.
        /// </summary>
        /// <param name="alphabetLength"></param>
        /// <param name="query"></param>
        /// <param name="queryLength"></param>
        /// <param name="equalityDefinition"></param>
        /// <returns>Table of dimensions alphabetLength+1 x maxNumBlocks.</returns>
        private static ulong[] BuildPeq(int alphabetLength, byte[] query, int queryLength, ref EqualityDefinition equalityDefinition)
        {
            int maxBlocks = CeilDivision(queryLength, WordSize);
            // Table of dimensions alphabetLength+1 x maxNumBlocks. Last symbol is wildcard.
            ulong[] peq = new ulong[(alphabetLength + 1) * maxBlocks];

            // Build Peq (1 is match, 0 is mismatch). NOTE: last column is wildcard(symbol that matches anything) with just 1s
            for (byte symbol = 0; symbol <= alphabetLength; symbol++)
            {
                for (int b = 0; b < maxBlocks; b++)
                {
                    if (symbol < alphabetLength)
                    {
                        peq[symbol * maxBlocks + b] = 0;
                        for (int r = (b + 1) * WordSize - 1; r >= b * WordSize; r--)
                        {
                            peq[symbol * maxBlocks + b] <<= 1;
                            // NOTE: We pretend like query is padded at the end with W wildcard symbols
                            if (r >= queryLength || equalityDefinition.IsEqual(query[r], symbol))
                                peq[symbol * maxBlocks + b] += 1;
                        }
                    }
                    else
                    {
                        peq[symbol * maxBlocks + b] = UInt64.MaxValue;
                    }
                }
            }

            return peq;
        }

        /// <summary>
        /// Uses Myers' bit-vector algorithm to find edit distance for global(NW) alignment method.
        /// </summary>
        /// <param name="peq"></param>
        /// <param name="w"></param>
        /// <param name="maxBlocks"></param>
        /// <param name="queryLength"></param>
        /// <param name="target"></param>
        /// <param name="targetLength"></param>
        /// <param name="k"></param>
        /// <param name="findAlignment"></param>
        /// <param name="targetStopPosition"></param>
        /// <param name="bestScore"></param>
        /// <param name="position"></param>
        /// <param name="alignmentData"></param>
        /// <returns>Status code.</returns>
        private static int MyersEditDistanceNw(ulong[] peq, int w, int maxBlocks, int queryLength, byte[] target,
            int targetLength, int k, bool findAlignment, int targetStopPosition, ref int bestScore,
            ref int position, ref AlignmentData alignmentData)
        {
            if (targetStopPosition > -1 && findAlignment)
                return (int) AlignmentStatus.Error;
            
            // Each STRONG_REDUCE_NUM column is reduced in more expensive way.
            const int strongReduceNum = 2048;

            if (k < Math.Abs(targetLength - queryLength))
            {
                bestScore = position = -1;
                return (int) AlignmentStatus.Ok;
            }

            k = Math.Min(k, Math.Max(queryLength, targetLength));

            int firstBlock = 0;
            int lastBlock = Math.Min(maxBlocks,
                                CeilDivision(Math.Min(k, (k + queryLength - targetLength) / 2) + 1,
                                    WordSize)) - 1;
            Block[] bl; // Current block
            Block[] blocks = new Block[maxBlocks];
            for (int i = 0; i < maxBlocks; i++)
            {
                blocks[i] = new Block();
            }
            
            // Initialize p, m, and score
            bl = blocks;
            for (int b = 0; b <= lastBlock; b++)
            {
                bl[b].Score = (b + 1) * WordSize;
                bl[b].P = UInt64.MaxValue;
                bl[b].M = 0;
            }

            if (findAlignment)
            {
                alignmentData = new AlignmentData(maxBlocks, targetLength);
            }
            else if (targetStopPosition > -1)
            {
                alignmentData = new AlignmentData(maxBlocks, 1);
            }

            for (int c = 0; c < targetLength; c++)
                {
                    ulong[] peqC = peq;
                    // Get offset for the character peq
                    // peq + *targetChar * maxBlocks
                    int startPeqC = target[c] * maxBlocks;

                    // Calculate column
                    int hout = 1;
                    // Get first block offset
                    for (int b = firstBlock; b <= lastBlock; b++)
                    {
                        ulong pout, mout;
                        hout = CalculateBlock(bl[b].P, bl[b].M, peqC[startPeqC + b], hout, out pout, out mout);
                        bl[b].P = pout;
                        bl[b].M = mout;
                        bl[b].Score += hout;
                    }

                    k = Math.Min(k,
                        bl[lastBlock].Score +
                        Math.Max(targetLength - c - 1, queryLength - ((1 + lastBlock) * WordSize - 1) - 1) +
                        (lastBlock == maxBlocks - 1 ? w : 0));

                    // Adjust number of blocks using Ukkonen algorithm
                    if (lastBlock + 1 < maxBlocks
                        && !
                            ( //score[lastBlock] >= k + Constants.WORDSIZE ||  // NOTICE: this condition could be satisfied if above block also!
                                ((lastBlock + 1) * WordSize - 1
                                 > k - bl[lastBlock].Score + 2 * WordSize - 2 - targetLength + c +
                                 queryLength)))
                    {
                        lastBlock++;
                        bl[lastBlock].P = UInt64.MaxValue;
                        bl[lastBlock].M = 0;
                        ulong pout, mout;
                        int newHout = CalculateBlock(bl[lastBlock].P,
                            bl[lastBlock].M, peqC[startPeqC + lastBlock], hout, out pout, out mout);

                        bl[lastBlock].Score =
                            bl[lastBlock - 1].Score - hout + WordSize + newHout;
                        bl[lastBlock].P = pout;
                        bl[lastBlock].M = mout;
                        hout = newHout;
                    }

                    // While block is out of band, move one block up.
                    // NOTE: Condition used here is more loose than the one from the article, since I simplified the max() part of it.
                    // I could consider adding that max part, for optimal performance.
                    while (lastBlock >= firstBlock
                           && (bl[lastBlock].Score >= k + WordSize
                               || ((lastBlock + 1) * WordSize - 1 >
                                   // TODO: Does not work if do not put +1! Why???
                                   k - bl[lastBlock].Score + 2 * WordSize - 2 - targetLength + c +
                                   queryLength + 1)))
                    {
                        lastBlock--;
                    }

                    // While outside of band, advance block
                    while (firstBlock <= lastBlock
                           && (bl[firstBlock].Score >= k + WordSize
                               || ((firstBlock + 1) * WordSize - 1 <
                                   bl[firstBlock].Score - k - targetLength + queryLength + c)))
                    {
                        firstBlock++;
                    }

                    if (c % strongReduceNum == 0)
                    {
                        while (lastBlock >= firstBlock)
                        {
                            int[] scores = GetBlockCellValues(bl[lastBlock]);
                            int numCells = lastBlock == maxBlocks - 1 ? WordSize - w : WordSize;
                            int r = lastBlock * WordSize + numCells - 1;
                            bool reduce = true;
                            for (int i = WordSize - numCells; i < WordSize; i++)
                            {
                                // TODO: Does not work if do not put +1! Why???
                                if (scores[i] <= k && r <= k - scores[i] - targetLength + c + queryLength + 1)
                                {
                                    reduce = false;
                                    break;
                                }

                                r--;
                            }

                            if (!reduce)
                                break;
                            
                            lastBlock--;
                        }

                        while (firstBlock <= lastBlock)
                        {
                            int[] scores = GetBlockCellValues(bl[firstBlock]);
                            int numCells = firstBlock == maxBlocks - 1 ? WordSize - w : WordSize;
                            int r = firstBlock * WordSize + numCells - 1;
                            bool reduce = true;
                            for (int i = WordSize - numCells; i < WordSize; i++)
                            {
                                if (scores[i] <= k && r >= scores[i] - k - targetLength + c + queryLength)
                                {
                                    reduce = false;
                                    break;
                                }

                                r--;
                            }

                            if (!reduce)
                                break;

                            firstBlock++;
                        }
                    }

                    if (lastBlock < firstBlock)
                    {
                        bestScore = position = -1;
                        return (int) AlignmentStatus.Ok;
                    }

                    if (findAlignment && c < targetLength)
                    {
                        for (int b = firstBlock; b <= lastBlock; b++)
                        {
                            alignmentData.Ps[maxBlocks * c + b] = bl[b].P;
                            alignmentData.Ms[maxBlocks * c + b] = bl[b].M;
                            alignmentData.Scores[maxBlocks * c + b] = bl[b].Score;
                            alignmentData.FirstBlocks[c] = firstBlock;
                            alignmentData.LastBlocks[c] = lastBlock;
                        }
                    }

                    if (c == targetStopPosition)
                    {
                        for (int b = firstBlock; b <= lastBlock; b++)
                        {
                            alignmentData.Ps[b] = blocks[b].P;
                            alignmentData.Ms[b] = blocks[b].M;
                            alignmentData.Scores[b] = blocks[b].Score;
                            alignmentData.FirstBlocks[0] = firstBlock;
                            alignmentData.LastBlocks[0] = lastBlock;
                        }

                        bestScore = -1;
                        position = targetStopPosition;
                        return (int) AlignmentStatus.Ok;
                    }
                }

            if (lastBlock == maxBlocks - 1)
            {
                int bs = GetBlockCellValues(bl[lastBlock])[w];
                if (bs <= k)
                {
                    bestScore = bs;
                    position = targetLength - 1;
                    return (int) AlignmentStatus.Ok;
                }
            }

            bestScore = position = -1;
            return (int) AlignmentStatus.Ok;
        }

        // TODO: Implement Semi-Global alignment
/*        public static void MyersEditDistanceSemiGlobal(ulong[] peq, int w, int maxBlocks, int queryLength,
            byte[] target,
            int targetLength, int k, AlignmentMode mode, ref int bestScore, ref int[] positions, ref int numPositions)
        {
            throw new NotImplementedException();
        }*/

        /// <summary>
        /// Corresponds to Advance_Block function from Myers. Calculates one word(block), which is part of a column.
        /// Highest bit of word (one most to the left) is most bottom cell of block from column.
        /// Pv[i] and Mv[i] define vin of cell[i]: vin = cell[i] - cell[i-1].
        /// </summary>
        /// <param name="pv"></param>
        /// <param name="mv"></param>
        /// <param name="eq"></param>
        /// <param name="hin">+1, 0 or -1.</param>
        /// <param name="pvOut"></param>
        /// <param name="mvOut"></param>
        /// <returns>+1, 0 or -1.</returns>
        private static int CalculateBlock(ulong pv, ulong mv, ulong eq, int hin, out ulong pvOut, out ulong mvOut)
        {
            // hin can be 1, -1 or 0.
            // 1  -> 00...01
            // 0  -> 00...00
            // -1 -> 11...11 (2-complement)
            
            ulong hinIsNegative = (ulong) (hin >> 2) & Word1; // 00...001 if hin is -1, 00...000 if 0 or 1
            ulong xv = eq | mv;
            eq |= hinIsNegative;
            ulong xh = (((eq & pv) + pv) ^ pv) | eq;

            ulong ph = mv | ~(xh | pv);
            ulong mh = pv & xh;

            // NOTE: (Ph & HIGH_BIT_MASK) >> (WORD_SIZE - 1) doesn't work. Why?!
            int hout = (ph & HighBitMask) != 0 ? 1 : 0;
            // NOTE: (Mh & HIGH_BIT_MASK) >> (WORD_SIZE - 1) doesn't work. Why?!
            hout = (mh & HighBitMask) != 0 ? -1 : hout;

            ph <<= 1;
            mh <<= 1;

            // This is instruction below written using 'if': if (hin < 0) Mh |= (Word)1;
            mh |= hinIsNegative;
            // This is instruction below written using 'if': if (hin > 0) Ph |= (Word)1;
            ph |= (ulong) ((hin + 1) >> 1);

            pvOut = mh | ~(xv | ph);
            mvOut = ph & xv;
            
            return hout;
        }

        /// <summary>
        /// Values of cells in block, starting with bottom cell in block.
        /// </summary>
        /// <param name="block">Current block</param>
        /// <returns>Array of scores of size 64</returns>
        private static int[] GetBlockCellValues(Block block)
        {
            int[] scores = new int[WordSize];
            int score = block.Score;
            ulong mask = HighBitMask;
            
            for (int i = 0; i < WordSize - 1; i++)
            {
                scores[i] = score;
                if ((block.P & mask) != 0)
                    score--;
                if ((block.M & mask) != 0)
                    score++;
                mask >>= 1;
            }
            
            scores[WordSize - 1] = score;
            return scores;
        }

        /// <summary>
        /// Writes values of cells in block into given array, starting with first/top cell.
        /// </summary>
        /// <param name="block">Current block.</param>
        /// <param name="dest">Reference to an array of destination.</param>
        public static void ReadBlock(Block block, ref int[] dest)
        {
            int score = block.Score;
            ulong mask = HighBitMask;
            for (int i = 0; i < WordSize - 1; i++)
            {
                dest[WordSize - 1 - i] = score;
                if ((block.P & mask) != 0) 
                    score--;
                if ((block.M & mask) != 0) 
                    score++;
                mask >>= 1;
            }
            
            dest[0] = score;
        }

        /// <summary>
        /// Writes values of cells in block into given array, starting with last/bottom cell.
        /// </summary>
        /// <param name="block">Current block.</param>
        /// <param name="dest">Reference to an array of destination.</param>
        public static void ReadBlockReverse(Block block, ref int[] dest)
        {
            int score = block.Score;
            ulong mask = HighBitMask;
            for (int i = 0; i < WordSize - 1; i++)
            {
                dest[i] = score;
                if ((block.P & mask) != 0) 
                    score--;
                if ((block.M & mask) != 0) 
                    score++;
                mask >>= 1;
            }

            dest[WordSize - 1] = score;
        }

        /// <summary>
        /// Check whether any cell is smaller than k.
        /// </summary>
        /// <param name="block">Current block</param>
        /// <param name="k">K value.</param>
        /// <returns>True if all value in every cells is larger than k. Return false if otherwise.</returns>
        public static bool AllBlockCellsLarger(Block block, int k)
        {
            int[] scores = GetBlockCellValues(block);
            for (int i = 0; i < WordSize; i++)
            {
                if (scores[i] <= k)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Get a new sequence that is reverse of given sequence.
        /// </summary>
        /// <param name="sequence">Original sequence.</param>
        /// <param name="length">length of the sequence.</param>
        /// <returns>Reversed sequence.</returns>
        public static byte[] CreateReverseCopy(byte[] sequence, int length)
        {
            byte[] reverseSequence = new byte[length];
            for (int i = 0; i < length; i++)
            {
                reverseSequence[i] = sequence[length - i - 1];
            }

            return reverseSequence;
        }

        /// <summary>
        /// Get default configuration.
        /// </summary>
        /// <returns>AlignmentConfig struct with default values</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static AlignmentConfig GetDefaultConfig() =>
            new AlignmentConfig(-1, AlignmentMode.Nw, AlignmentTask.TaskDistance, null, 0);

        /// <summary>
        /// Ceiling division of x / y.
        /// </summary>
        /// <remarks>x and y must be non-negative and x + y must not overflow.</remarks>
        /// <param name="x">Numerator</param>
        /// <param name="y">Denominator</param>
        /// <returns>Result of the division.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CeilDivision(int x, int y) => x % y != 0 ? x / y + 1 : x / y;
        
        /// <summary>
        /// Calculate a similarity measure from an edit distance.
        /// </summary>
        /// <param name="distance">Editing distance of the two strings.</param>
        /// <param name="length">The length of the longer of the two strings the edit distance is from.</param>
        /// <returns>A similarity value from 0 to 1.0 (1 - (length / distance)).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static decimal ToSimilarity(int distance, int length) =>
            (distance < 0) ? -1 : Math.Round(1 - (distance / (decimal) length), 2);

    }
}