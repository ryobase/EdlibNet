namespace EdlibNet.Structs
{
    /// <summary>
    /// Block data
    /// </summary>
    public struct Block
    {
        /// <summary>
        /// Pin value
        /// </summary>
        public ulong P;
        
        /// <summary>
        /// Min value
        /// </summary>
        public ulong M;
        
        /// <summary>
        /// Current score
        /// </summary>
        public int Score;

        /// <summary>
        /// Constructor with fields.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="m"></param>
        /// <param name="score"></param>
        public Block(ulong p, ulong m, int score)
        {
            P = p;
            M = m;
            Score = score;
        }
    }
}