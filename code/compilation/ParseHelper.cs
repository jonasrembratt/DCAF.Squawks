namespace DCAF.Squawks
{
    public static class ParseHelper
    {
        public static bool IsToken(this char[] chars, int index, string pattern)
        {
            var pa = pattern.ToCharArray();
            for (var i = 0; i < pa.Length && i < chars.Length; i++)
            {
                if (chars[index + i] != pa[i])
                    return false;
            }

            return true;
        }
        
    }
}