namespace HouseofCat.Hashing.Argon;

public static class Constants
{
    // https://github.com/P-H-C/phc-winner-argon2/blob/master/README.md
    public static class Argon
    {
        // Recommend 4 threads for most security scenarios.
        // Recommend 1 thread for low security scenarios.
        public static int DoP { get; set; } = 2;

        // CAUTION: You most likely need this much RAM per Hash.
        // Recommend 2 GB for the highest security scenarios.
        // Recommend a minimum of 64 MB in high security scenarios.
        // Recommend a minimum of 2 MB in low security scenarios.
        public static int MemorySize { get; set; } = 1024*64;

        // Recommend a minimum of 3 for most security scenarios.
        public static int Iterations { get; set; } = 3;

        public static readonly string ArgonHashKeyNotSet = "Argon hash key has not been defined yet.";
    }
}
