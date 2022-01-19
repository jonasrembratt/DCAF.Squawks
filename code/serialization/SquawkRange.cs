namespace DCAF.Squawks
{
    class SquawkRange
    {
        public int From { get; set; }

        public int To { get; set; }

        public int Increment { get; set; }

        public void SwapIfNeeded()
        {
            if (From > To)
            {
                (To, From) = (From, To);
            }
        }
    }
}