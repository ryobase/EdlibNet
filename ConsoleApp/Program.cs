using System;
using EdlibNet;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string a = "Satellite Girl and Milk Cow (Uribyeol Ilho-wa Eollukso), The";
            string b = "The Satellite Girl and Milk Cow";

            var result = Edlib.Align(a, b, Edlib.GetDefaultConfig()).EditDistance;

            Console.WriteLine($"{Edlib.ToSimilarity(result, Math.Max(a.Length, b.Length))}");
        }
    }
}