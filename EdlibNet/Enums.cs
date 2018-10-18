namespace EdlibNet
{
    /// <summary>
    /// Status codes
    /// </summary>
    public enum AlignmentStatus
    {
        Ok = 0,
        Error = 1,
    }
    
    /// <summary>
    /// Alignment tasks - what do you want Edlib to do?
    /// </summary>
    public enum AlignmentTask
    {
        TaskDistance,
        TaskLocate,
        TaskPath
    }

    /// <summary>
    /// Alignment methods - how should Edlib treat gaps before and after query?
    /// </summary>
    public enum AlignmentMode
    {
        /// <summary>
        /// Global method. This is the standard method.
        /// Useful when you want to find out how similar is first sequence to second sequence.
        /// </summary>
        Nw,
        
        /// <summary>
        /// Prefix method. Similar to global method, but with a small twist - gap at query end is not penalized.
        /// What that means is that deleting elements from the end of second sequence is "free"!
        /// </summary>
        Shw,
        
        /// <summary>
        /// Infix method. Similar as prefix method, but with one more twist - gaps at query end and start are
        /// not penalized. What that means is that deleting elements from the start and end of second sequence is "free"!
        /// </summary>
        Hw
    }
    
    /// <summary>
    /// Edit operations.
    /// </summary>
    public enum Ops
    {
        Match = 0,
        Insert = 1,
        Delete = 2,
        Mismatch = 3,
    }

}