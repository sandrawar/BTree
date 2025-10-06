using System;
using System.Text;
using System.Threading.Tasks;
using BTreeNamespace;

namespace BTreeDemo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=== THREAD-SAFETY TEST: B-Tree ===\n");

            BTree tree = new BTree();

            string[] keys = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "Z", "U", "T", "P", "S", "R", "W", "X", "Y" };

            Task[] insertTasks = new Task[keys.Length];
            for (int i = 0; i < keys.Length; i++)
            {
                string key = keys[i];
                insertTasks[i] = Task.Run(() =>
                {
                    tree.Add(Encoding.ASCII.GetBytes(key), Encoding.ASCII.GetBytes($"val_{key}"));
                    Console.WriteLine($"[Insert] {key}");
                });
            }

            Task.WaitAll(insertTasks);

            var printTask = Task.Run(() =>
            {
                Console.WriteLine("\n--- Tree After Insertions ---");
                tree.PrintTree();
            });
            printTask.Wait(); 


            string[] keysToDelete = { "T", "P", "B", "C", "F" };
            Task[] deleteTasks = new Task[keysToDelete.Length];
            for (int i = 0; i < keysToDelete.Length; i++)
            {
                string key = keysToDelete[i];
                deleteTasks[i] = Task.Run(() =>
                {
                    tree.Delete(Encoding.ASCII.GetBytes(key));
                    Console.WriteLine($"[Delete] {key}");
                });
            }

            Task.WaitAll(deleteTasks);

            var printTask2 = Task.Run(() =>
            {
                Console.WriteLine("\n--- Tree After Insertions ---");
                tree.PrintTree();
            });
            printTask2.Wait(); 


            Console.WriteLine("\n=== THREAD-SAFETY TEST COMPLETE ===");
            Console.ReadKey();
        }
    }
}
