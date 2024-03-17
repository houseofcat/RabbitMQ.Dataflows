namespace HouseofCat.Algorithms.Math;

public static class Factorial
{
    public static long Solve(long input)
    {
        if (input == 0) return 1;

        long factorial = 1;
        for (long i = input; i > 0; i--)
        {
            factorial *= i;
        }

        return factorial;
    }

    public static long SolveRecursively(long input)
    {
        if (input == 0) return 1;

        return input * SolveRecursively(input--);
    }

    // TODO: Tail Recursion/Trampoline solve.
}
