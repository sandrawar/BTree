using static BTreeNamespace.BTree;

namespace BTreeNamespace
{
    public class BTree
    {
        private const int m = 2;

        public class Node
        {
            private Node[] _children = new Node[2 * m + 1];
            private Node _parent = null;
            private byte[][] _keys = new byte[2 * m][];
            private byte[][] _values = new byte[2 * m][];
            private int _keyCount = 0;
            private bool _isLeaf = true;

            public Node()
            {
                for (int i = 0; i < _keys.Length; i++)
                {
                    _keys[i] = null;
                    _values[i] = null;
                }
            }

            public Node[] children => _children;

            public Node parent
            {
                get => _parent;
                internal set => _parent = value;
            }

            public byte[][] keys => _keys;

            public byte[][] values => _values;

            public int keyCount
            {
                get => _keyCount;
                internal set => _keyCount = value;
            }

            public bool isLeaf
            {
                get => _isLeaf;
                internal set => _isLeaf = value;
            }
        }

        private Node _root;

        public BTree()
        {
            _root = new Node();
        }

        public Node root => _root;

        public void Add(byte[] key, byte[] value)
        {

        }

        public void Delete(byte[] key)
        {

        }


        private int CompareKeys(byte[] a, byte[] b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;

            int len = Math.Min(a.Length, b.Length);
            for (int i = 0; i < len; i++)
            {
                if (a[i] < b[i]) return -1;
                if (a[i] > b[i]) return 1;
            }

            if (a.Length < b.Length) return -1;
            if (a.Length > b.Length) return 1;

            return 0;
        }


        public void PrintTree()
        {
            Console.WriteLine("=== BTree Structure ===");
            PrintNode(_root, 0);
            Console.WriteLine("========================");
        }

        private void PrintNode(Node node, int level)
        {
            string indent = new string(' ', level * 4);

            Console.Write(indent + "[");
            for (int i = 0; i < node.keyCount; i++)
            {
                Console.Write(KeyToString(node.keys[i]));
                if (i < node.keyCount - 1)
                    Console.Write(", ");
            }
            Console.WriteLine("]");

            if (!node.isLeaf)
            {
                for (int i = 0; i <= node.keyCount; i++)
                {
                    PrintNode(node.children[i], level + 1);
                }
            }
        }

        private string KeyToString(byte[] key)
        {
            if (key == null) return "null";

            bool ascii = true;
            foreach (var b in key)
            {
                if (b < 32 || b > 126)
                {
                    ascii = false;
                    break;
                }
            }

            if (ascii)
                return $"\"{System.Text.Encoding.ASCII.GetString(key)}\"";
            else
                return "0x" + BitConverter.ToString(key).Replace("-", "");
        }

    }
}
