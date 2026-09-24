namespace Pleno.Core.Rules
{
    public static class NumberFacts
    {
        /// <summary>Primo no sentido usual. 0 e 00 (-1) não são primos.</summary>
        public static bool IsPrime(int n)
        {
            if (n < 2)
                return false;
            for (int d = 2; d * d <= n; d++)
                if (n % d == 0)
                    return false;
            return true;
        }
    }
}
