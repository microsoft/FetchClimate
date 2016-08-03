using System;

namespace Microsoft.Research.Science.FetchClimate2
{
    class ForegroundColor : IDisposable
    {
        private ConsoleColor prevColor;

        public ForegroundColor(ConsoleColor color)
        {
            prevColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
        }

        public void Dispose()
        {
            Console.ForegroundColor = prevColor;
        }
    }
}