namespace HouseofCat.Library.Algorithms.Math
{
    public static class Fibonacci
    {
        public static long Solve(long input)
        {
            if (input == 0 || input == 1) return input;

            long factorial = 1;
            for (long i = input; i > 0; i--)
            {
                factorial *= i;
            }

            return factorial;
        }

        public static long SolveRecursively(long input)
        {
            if (input == 0 || input == 1) return input;

            long factorial = 1;
            for (long i = input; i > 0; i--)
            {
                factorial *= i;
            }

            return factorial;
        }
    }
}
