using System;
using System.Text;
using BTreeNamespace;

namespace BTreeDemo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== DEMO: B-Tree Implementation ===\n");

            BTree tree = new BTree();

            string[] keysToAdd = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "Z", "U", "T", "P", "S", "R", "W", "X", "Y" };

            foreach (var key in keysToAdd)
            {
                tree.Add(Encoding.ASCII.GetBytes(key), Encoding.ASCII.GetBytes($"val_{key}"));
                Console.WriteLine($"\n Dodano klucz: {key}");
                tree.PrintTree();
            }


            Console.WriteLine("\n Usuwanie kluczy:");
            string[] keysToDelete = { "T", "P", "B", "C", "F" };
            foreach (var key in keysToDelete)
            {
                tree.Delete(Encoding.ASCII.GetBytes(key));
                Console.WriteLine($"\n Usunięto: {key}");
                tree.PrintTree();
            }

            Console.WriteLine("\n=== KONIEC DEMONSTRACJI ===");
            Console.ReadKey();
        }
    }
}
