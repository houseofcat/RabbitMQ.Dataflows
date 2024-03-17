namespace HouseofCat.Algorithms;

public static class Helpers
{
    public static void Swap(int[] input, int i, int j)
    {
        if (i == j) return;

        var temp = input[i];
        input[i] = input[j];
        input[j] = temp;
    }
}
