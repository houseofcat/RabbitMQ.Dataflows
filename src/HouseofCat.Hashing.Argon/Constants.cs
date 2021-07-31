namespace HouseofCat.Hashing.Argon
{
    public static class Constants
    {
        public static class Argon
        {
            public const int DoP = 4;
            public const int MemorySize = 2048;
            public const int Iterations = 12;

            public static readonly string ArgonHashKeyNotSet = "Argon hash key has not been defined yet.";
        }
    }
}
