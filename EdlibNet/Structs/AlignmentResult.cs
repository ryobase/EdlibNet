namespace EdlibNet.Structs
{
    /// <summary>
    /// Container for results of the alignment.
    /// </summary>
    public struct AlignmentResult
    {
        /// <summary>
        /// If error, all other fields will be set to their own respected default value.
        /// </summary>
        public int Status;
        
        /// <summary>
        /// -1 if k is non-negative and editing distance is larger than k.
        /// </summary>
        public int EditDistance;
        
        /// <summary>
        /// Array of zero-based positions in target where optimal alignment paths end.
        /// </summary>
        public int[] EndLocations;
        
        /// <summary>
        /// Array of zero-based positions in target where optimal alignment paths start, they correspond to EndLocations
        /// </summary>
        public int[] StartingLocations;
        
        /// <summary>
        /// Number of end (and start) locations.
        /// </summary>
        public int NumberOfLocations;
        
        /// <summary>
        /// Alignment is found for first pair of start and end locations.
        /// Set to NULL if not calculated.
        /// Alignment is sequence of numbers: 0, 1, 2, 3.
        /// Alignment aligns query to target from beginning of query till end of query.
        /// If gaps are not penalized, they are not in alignment.
        /// </summary>
        public char[] Alignment;
        
        /// <summary>
        /// Length of alignment.
        /// </summary>
        public int AlignmentLength;
        
        /// <summary>
        /// Length of different characters in query and target together.
        /// </summary>
        public int AlphabetLength;
    }
}