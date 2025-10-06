using System.Diagnostics.Metrics;
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
            Node leaf = FindNeededLeaf(key);
            Add(key, value, leaf);

        }

        private void Add(byte[] key, byte[] value, Node node)
        {
            for (int i = 0; i < node.keyCount; i++)
            {
                if (CompareKeys(node.keys[i], key) == 0)
                {
                    node.values[i] = value;
                    return;
                }
            }

            if (node.keyCount < 2 * m)
            {
                InsertIntoNotFullNode(key, value, node);
            }
            else
            {
                InsertIntoFullNode(key, value, node);
            }

        }

        private void InsertIntoNotFullNode(byte[] key, byte[] value, Node node)
        {
            int i;
            for (i = 0; i < node.keyCount; i++)
            {
                if (CompareKeys(node.keys[i], key) > 0)
                {
                    break;
                }
            }
            for (int j = node.keyCount; j > i; j--)
            {
                node.keys[j] = node.keys[j - 1];
                node.values[j] = node.values[j - 1];
            }

            node.keys[i] = key;
            node.values[i] = value;
            node.keyCount++;
        }

        private void InsertIntoFullNode(byte[] key, byte[] value, Node node)
        {
            byte[][] tempKeys = new byte[2 * m + 1][];
            byte[][] tempValues = new byte[2 * m + 1][];
            int i;
            for (i = 0; i < 2 * m; i++)
            {
                tempKeys[i] = node.keys[i];
                tempValues[i] = node.values[i];
            }
            tempKeys[2 * m] = key;
            tempValues[2 * m] = value;

            Array.Sort(tempKeys, tempValues, 0, 2 * m + 1, Comparer<byte[]>.Create(CompareKeys));

            int mid = (2 * m + 1) / 2;
            Node right = new Node();
            right.isLeaf = node.isLeaf;
            right.parent = node.parent;

            if (!node.isLeaf)
            {
                Node[] tempChildren = new Node[2 * m + 2];
                for (i = 0; i <= 2 * m; i++)
                    tempChildren[i] = node.children[i];

                for (i = 0; i <= mid; i++)
                    node.children[i] = tempChildren[i];
                for (i = mid + 1; i <= 2 * m + 1; i++)
                {
                    right.children[i - (mid + 1)] = tempChildren[i];
                    if (right.children[i - (mid + 1)] != null)
                        right.children[i - (mid + 1)].parent = right;
                }
                for (i = mid + 1; i <= 2 * m; i++)
                    node.children[i] = null;

                node.isLeaf = false;
                right.isLeaf = false;
            }

            for (i = mid + 1; i < 2 * m + 1; i++)
            {
                right.keys[i - mid - 1] = tempKeys[i];
                right.values[i - mid - 1] = tempValues[i];
                right.keyCount++;
            }

            for (i = 0; i < mid; i++)
            {
                node.keys[i] = tempKeys[i];
                node.values[i] = tempValues[i];
            }
            node.keyCount = mid;

            byte[] middleKey = tempKeys[mid];
            byte[] middleValue = tempValues[mid];

            if (node.parent == null)
            {
                Node newRoot = new Node();
                newRoot.isLeaf = false;
                newRoot.keyCount = 1;
                newRoot.keys[0] = middleKey;
                newRoot.values[0] = middleValue;
                newRoot.children[0] = node;
                newRoot.children[1] = right;
                node.parent = newRoot;
                right.parent = newRoot;
                _root = newRoot;
            }
            else
            {
                InsertIntoParent(node.parent, middleKey, middleValue, node, right);
            }

        }
        private void InsertIntoParent(Node parent, byte[] key, byte[] value, Node leftChild, Node rightChild)
        {
            int oldKeys = parent.keyCount;           
            int totalKeys = oldKeys + 1;           
            byte[][] tempKeys = new byte[totalKeys][];
            byte[][] tempValues = new byte[totalKeys][];
            Node[] tempChildren = new Node[totalKeys + 1];

            for (int c = 0; c <= oldKeys; c++)
                tempChildren[c] = parent.children[c];

            int insertPos = 0;
            while (insertPos < oldKeys && CompareKeys(parent.keys[insertPos], key) < 0)
                insertPos++;

            for (int k = 0; k < insertPos; k++)
            {
                tempKeys[k] = parent.keys[k];
                tempValues[k] = parent.values[k];
            }

            tempKeys[insertPos] = key;
            tempValues[insertPos] = value;

            for (int k = insertPos; k < oldKeys; k++)
            {
                tempKeys[k + 1] = parent.keys[k];
                tempValues[k + 1] = parent.values[k];
            }


            tempChildren[insertPos] = leftChild;
            tempChildren[insertPos + 1] = rightChild;

            for (int c = insertPos + 1; c <= oldKeys; c++)
            {
                tempChildren[c + 1] = parent.children[c];
            }

            if (leftChild != null) leftChild.parent = parent;
            if (rightChild != null) rightChild.parent = parent;

            if (totalKeys <= 2 * m)
            {
                for (int k = 0; k < totalKeys; k++)
                {
                    parent.keys[k] = tempKeys[k];
                    parent.values[k] = tempValues[k];
                }
                for (int c = 0; c <= totalKeys; c++)
                    parent.children[c] = tempChildren[c];

                parent.keyCount = totalKeys;
                for (int k = parent.keyCount; k < 2 * m; k++)
                {
                    parent.keys[k] = null;
                    parent.values[k] = null;
                    parent.children[k + 1] = null;
                }
                return;
            }

            int mid = totalKeys / 2; 
            Node right = new Node();
            right.isLeaf = parent.isLeaf;
            right.parent = parent.parent;

            for (int k = 0; k < mid; k++)
            {
                parent.keys[k] = tempKeys[k];
                parent.values[k] = tempValues[k];
            }
            parent.keyCount = mid;

            for (int k = mid + 1; k < totalKeys; k++)
            {
                right.keys[k - (mid + 1)] = tempKeys[k];
                right.values[k - (mid + 1)] = tempValues[k];
                right.keyCount++;
            }

            if (!parent.isLeaf)
            {
                for (int c = 0; c <= mid; c++)
                {
                    parent.children[c] = tempChildren[c];
                    if (parent.children[c] != null) parent.children[c].parent = parent;
                }
                for (int c = mid + 1; c <= totalKeys; c++)
                {
                    right.children[c - (mid + 1)] = tempChildren[c];
                    if (right.children[c - (mid + 1)] != null) right.children[c - (mid + 1)].parent = right;
                }
                for (int c = parent.keyCount + 1; c < parent.children.Length; c++)
                    parent.children[c] = null;
            }
            else
            {
                for (int c = parent.keyCount + 1; c < parent.children.Length; c++)
                    parent.children[c] = null;
            }

            byte[] middleKey = tempKeys[mid];
            byte[] middleValue = tempValues[mid];

            if (parent.parent == null)
            {
                Node newRoot = new Node();
                newRoot.isLeaf = false;
                newRoot.keys[0] = middleKey;
                newRoot.values[0] = middleValue;
                newRoot.keyCount = 1;
                newRoot.children[0] = parent;
                newRoot.children[1] = right;
                parent.parent = newRoot;
                right.parent = newRoot;
                _root = newRoot;
            }
            else
            {
                InsertIntoParent(parent.parent, middleKey, middleValue, parent, right);
            }
        }



        public Node FindNeededLeaf(byte[] key)
        {
            return FindNeededLeafInternal(_root, key);
        }

        private Node FindNeededLeafInternal(Node node, byte[] key)
        {
            if (node.isLeaf)
            {
                return node;
            }

            int i = node.keyCount - 1;

            while (i > 0 && CompareKeys(node.keys[i], key) > 0) i--;

            if (node.children[i + 1] == null)
                node.children[i + 1] = new Node();

            return FindNeededLeafInternal(node.children[i + 1], key);
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
