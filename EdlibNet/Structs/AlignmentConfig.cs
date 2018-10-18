namespace EdlibNet.Structs
{
    /// <summary>
    /// Configuration object.
    /// </summary>
    public struct AlignmentConfig
    {
        /// <summary>
        /// Set k to non-negative value to tell edlib that edit distance is not larger than k.
        /// Smaller k can significantly improve speed of computation albeit with lower accuracy.
        /// If edit distance is larger than k, edlib will set edit distance to -1.
        /// Set k to negative value and edlib will internally auto-adjust k until score is found.
        /// </summary>
        public int K;
        
        /// <summary>
        /// Alignment method.
        /// Nw : Needleman-Wunsch (global).
        /// Shw : Gaps after query is not penalized (prefix).
        /// HW : Gaps before and after query are not penalized (infix).
        /// </summary>
        public AlignmentMode Mode;
        
        /// <summary>
        /// Alignment task - tells Edlib what to calculate. Less to calculate, faster it is.
        /// </summary>
        public AlignmentTask Task;
        
        /// <summary>
        /// List of pairs of characters, where each pair defines two characters as equal.
        /// This way you can extend edlib's definition of equality (which is that each character is equal only to itself).
        /// This can be useful if you have some wildcard characters that should match multiple other characters,
        /// or e.g. if you want edlib to be case insensitive. Set to NULL if there are none.
        /// </summary>
        public EqualityPair[] AdditionalEqualities;
        
        /// <summary>
        /// Number of additional equalities, which is non-negative number. 0 if there none.
        /// </summary>
        public int AdditionalEqualitiesLength;

        /// <summary>
        /// Constructor with fields.
        /// </summary>
        /// <param name="k"></param>
        /// <param name="mode"></param>
        /// <param name="task"></param>
        /// <param name="additionalEqualities"></param>
        /// <param name="additionalEqualitiesLength"></param>
        public AlignmentConfig(int k, AlignmentMode mode, AlignmentTask task, EqualityPair[] additionalEqualities,
            int additionalEqualitiesLength)
        {
            K = k;
            Mode = mode;
            Task = task;
            AdditionalEqualities = additionalEqualities;
            AdditionalEqualitiesLength = additionalEqualitiesLength;
        }
    }
}