namespace EdlibNet.Structs
{
    /// <summary>
    /// Data needed to find alignment.
    /// </summary>
    public struct AlignmentData
    {
        /// <summary>
        /// P values
        /// </summary>
        public ulong[] Ps;
        
        /// <summary>
        /// M values
        /// </summary>
        public ulong[] Ms;
        
        /// <summary>
        /// Score values
        /// </summary>
        public int[] Scores;
        
        /// <summary>
        /// First block values
        /// </summary>
        public int[] FirstBlocks;
        
        /// <summary>
        /// Last block values
        /// </summary>
        public int[] LastBlocks;

        /// <summary>
        /// Constructor with fields.
        /// </summary>
        /// <param name="maxBlocks"></param>
        /// <param name="targetLength"></param>
        public AlignmentData(int maxBlocks, int targetLength)
        {
            Ps = new ulong[maxBlocks * targetLength];
            Ms = new ulong[maxBlocks * targetLength];
            Scores = new int[maxBlocks * targetLength];
            FirstBlocks = new int[targetLength];
            LastBlocks = new int[targetLength];
        }
    }
}