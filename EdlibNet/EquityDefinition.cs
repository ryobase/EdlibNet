using EdlibNet.Structs;

namespace EdlibNet
{
    /// <summary>
    /// Defines equality relation on alphabet characters.
    /// By default each character is always equal only to itself, but you can also provide additional equalities.
    /// </summary>
    public class EqualityDefinition
    {
//        private readonly bool[,] _matrix = new bool[Constants.MAXUCHAR + 1, Constants.MAXUCHAR + 1];
        private static readonly int MatrixSize = 256; // The largest possible value of a byte.
        private readonly bool[] _matrix = new bool[MatrixSize * MatrixSize]; // 256 x 256 matrix in linear space.

        public EqualityDefinition(string alphabet, EqualityPair[] additionalEqualities, int additionalEqualitiesLength = 0)
        {
           
            for (int i = 0; i < alphabet.Length; i++)
            {
                for (int j = 0; j < alphabet.Length; j++)
                {
                    this._matrix[(i * MatrixSize) + j] = (i == j);
//                    this._matrix[i, j] = (i == j);
                }
            }

            if (additionalEqualities != null)
            {
                for (int i = 0; i < additionalEqualitiesLength; i++)
                {
                    var firstTransformed = alphabet.IndexOf(additionalEqualities[i].First);
                    var secondTransformed = alphabet.IndexOf(additionalEqualities[i].Second);
                    if (firstTransformed != -1 && secondTransformed != -1)
                    {
//                        _matrix[firstTransformed, secondTransformed] = _matrix[secondTransformed, firstTransformed] = true;
                        _matrix[(firstTransformed * MatrixSize) + secondTransformed] =
                            _matrix[(secondTransformed * MatrixSize) + firstTransformed] = true;
                    }
                }
            }
        }

        public bool IsEqual(byte a, byte b) => this._matrix[a * MatrixSize + b];
        public bool IsEqual(char a, char b) => this._matrix[a * MatrixSize + b];
//        public bool IsEqual(byte a, byte b) => this._matrix[a, b];
    }
}