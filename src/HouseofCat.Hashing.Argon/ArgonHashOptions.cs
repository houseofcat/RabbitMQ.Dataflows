namespace HouseofCat.Hashing
{
    public class ArgonHashOptions
    {
        public int MemorySize { get; set; } = 2048;
        public int Iterations { get; set; } = 12;
        public int DoP { get; set; } = 4;
    }
}
