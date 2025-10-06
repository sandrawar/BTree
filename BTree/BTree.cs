using System.Diagnostics.Metrics;
using System.Threading;
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
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        public BTree()
        {
            _root = new Node();
        }

        public Node root => _root;

        public void Add(byte[] key, byte[] value)
        {
            _rwLock.EnterWriteLock();
            try
            {
                Node leaf = FindNeededLeaf(key);
                Add(key, value, leaf);
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public void Delete(byte[] key)
        {
            _rwLock.EnterWriteLock();
            try
            {
                Node node = FindNodeWithKey(key);
                if (node == null)
                {
                    return;
                }
                if (node.isLeaf)
                {
                    DeleteKeyFromLeaf(node, key);
                }
                else
                {
                    DeleteKeyFromInnerNode(node, key);
                }
            }
            finally { 
                _rwLock.ExitWriteLock(); 
            }

        }

        private Node FindNodeWithKey(byte[] key)
        {
            Node node = _root;
            while (node != null)
            {
                for (int i = 0; i < node.keyCount; i++)
                {
                    int cmp = CompareKeys(node.keys[i], key);
                    if (cmp == 0)
                    {
                        return node; 
                    }
                    if (cmp > 0)
                    {
                        if (node.isLeaf) return null;
                        node = node.children[i];
                        goto nextIteration;
                    }
                }

                if (node.isLeaf) return null;
                node = node.children[node.keyCount];

            nextIteration:
                continue;
            }

            return null;
        }


        private void DeleteKeyFromLeaf(Node node, byte[] key)
        {
            int keyIdx = -1;
            for (int i = 0; i < node.keyCount; i++)
            {
                if (CompareKeys(node.keys[i], key) == 0)
                {
                    keyIdx = i;
                    break;
                }
            }
            if (keyIdx == -1) return;

            for (int i = keyIdx; i < node.keyCount - 1; i++)
            {
                node.keys[i] = node.keys[i + 1];
                node.values[i] = node.values[i + 1];
            }

            node.keys[node.keyCount - 1] = null;
            node.values[node.keyCount - 1] = null;
            node.keyCount--;

            if (node == _root || node.keyCount >= m) return;

            BalanseTreeFromNode(node);
        }


        private void BalanseTreeFromNode(Node node)
        {
            Node parent = node.parent;
            if (parent == null) return;

            int idxInParent = 0;
            while (idxInParent <= parent.keyCount && parent.children[idxInParent] != node)
                idxInParent++;

            Node leftSibling = (idxInParent > 0) ? parent.children[idxInParent - 1] : null;
            Node rightSibling = (idxInParent < parent.keyCount) ? parent.children[idxInParent + 1] : null;

            if (leftSibling != null && leftSibling.keyCount > m)
            {
                for (int i = node.keyCount; i > 0; i--)
                {
                    node.keys[i] = node.keys[i - 1];
                    node.values[i] = node.values[i - 1];
                }

                if (!node.isLeaf)
                {
                    for (int c = node.keyCount + 1; c > 0; c--)
                        node.children[c] = node.children[c - 1];
                }

                node.keys[0] = parent.keys[idxInParent - 1];
                node.values[0] = parent.values[idxInParent - 1];
                node.keyCount++;

                parent.keys[idxInParent - 1] = leftSibling.keys[leftSibling.keyCount - 1];
                parent.values[idxInParent - 1] = leftSibling.values[leftSibling.keyCount - 1];

                if (!leftSibling.isLeaf)
                {
                    Node moved = leftSibling.children[leftSibling.keyCount];
                    node.children[0] = moved;
                    if (moved != null) moved.parent = node;
                    leftSibling.children[leftSibling.keyCount] = null;
                }

                leftSibling.keys[leftSibling.keyCount - 1] = null;
                leftSibling.values[leftSibling.keyCount - 1] = null;
                leftSibling.keyCount--;

                ClearNodeSlots(leftSibling);
                ClearNodeSlots(node);
                return;
            }

            if (rightSibling != null && rightSibling.keyCount > m)
            {
                node.keys[node.keyCount] = parent.keys[idxInParent];
                node.values[node.keyCount] = parent.values[idxInParent];

                if (!rightSibling.isLeaf)
                {
                    Node moved = rightSibling.children[0];
                    node.children[node.keyCount + 1] = moved;
                    if (moved != null) moved.parent = node;
                }

                node.keyCount++;

                parent.keys[idxInParent] = rightSibling.keys[0];
                parent.values[idxInParent] = rightSibling.values[0];

                for (int i = 0; i < rightSibling.keyCount - 1; i++)
                {
                    rightSibling.keys[i] = rightSibling.keys[i + 1];
                    rightSibling.values[i] = rightSibling.values[i + 1];
                }
                if (!rightSibling.isLeaf)
                {
                    for (int c = 0; c < rightSibling.keyCount; c++)
                        rightSibling.children[c] = rightSibling.children[c + 1];
                    rightSibling.children[rightSibling.keyCount] = null;
                }

                rightSibling.keys[rightSibling.keyCount - 1] = null;
                rightSibling.values[rightSibling.keyCount - 1] = null;
                rightSibling.keyCount--;

                ClearNodeSlots(rightSibling);
                ClearNodeSlots(node);
                return;
            }

            if (leftSibling != null)
            {
                MergeNodes(parent, idxInParent - 1, leftSibling, node);
            }
            else if (rightSibling != null)
            {
                MergeNodes(parent, idxInParent, node, rightSibling);
            }

            if (parent == _root)
            {
                if (parent.keyCount == 0 && !parent.isLeaf)
                {
                    _root = parent.children[0];
                    if (_root != null) _root.parent = null;
                }
            }
            else if (parent.keyCount < m)
            {
                BalanseTreeFromNode(parent);
            }
        }


        private void MergeNodes(Node parent, int parentKeyIdx, Node left, Node right)
        {
            int leftOldCount = left.keyCount;

            left.keys[leftOldCount] = parent.keys[parentKeyIdx];
            left.values[leftOldCount] = parent.values[parentKeyIdx];
            left.keyCount = leftOldCount + 1;

            if (!left.isLeaf)
            {
                for (int c = 0; c <= right.keyCount; c++)
                {
                    left.children[leftOldCount + 1 + c] = right.children[c];
                    if (left.children[leftOldCount + 1 + c] != null)
                        left.children[leftOldCount + 1 + c].parent = left;
                }
            }

            for (int i = 0; i < right.keyCount; i++)
            {
                left.keys[left.keyCount + i] = right.keys[i];
                left.values[left.keyCount + i] = right.values[i];
            }
            left.keyCount += right.keyCount;

            for (int i = parentKeyIdx; i < parent.keyCount - 1; i++)
            {
                parent.keys[i] = parent.keys[i + 1];
                parent.values[i] = parent.values[i + 1];
                parent.children[i + 1] = parent.children[i + 2];
            }

            parent.keys[parent.keyCount - 1] = null;
            parent.values[parent.keyCount - 1] = null;
            parent.children[parent.keyCount] = null;
            parent.keyCount--;

            for (int i = 0; i < right.keys.Length; i++) right.keys[i] = null;
            for (int i = 0; i < right.values.Length; i++) right.values[i] = null;
            for (int i = 0; i < right.children.Length; i++) right.children[i] = null;
            right.keyCount = 0;
            right.parent = null;

            if (parent == _root && parent.keyCount == 0)
            {
                _root = left;
                if (_root != null) _root.parent = null;
            }

            ClearNodeSlots(left);
            ClearNodeSlots(parent);
        }

        private void ClearNodeSlots(Node node)
        {
            for (int k = node.keyCount; k < 2 * m; k++)
            {
                node.keys[k] = null;
                node.values[k] = null;
            }
            for (int c = node.keyCount + 1; c < node.children.Length; c++)
                node.children[c] = null;
        }




        private Node SwitchKeysWithLeaf(Node node, byte[] key)
        {
            int keyIdx = -1;
            for (int i = 0; i < node.keyCount; i++)
            {
                if (CompareKeys(node.keys[i], key) == 0)
                {
                    keyIdx = i;
                    break;
                }
            }

            if (keyIdx == -1)
            {
                return null;
            }

            Node current = node.children[keyIdx];
            if (current == null)
            {
                return null;
            }

            while (!current.isLeaf)
            {
                current = current.children[current.keyCount];
            }

            int predecessorIdx = current.keyCount - 1;

            byte[] tmpKey = node.keys[keyIdx];
            byte[] tmpVal = node.values[keyIdx];

            node.keys[keyIdx] = current.keys[predecessorIdx];
            node.values[keyIdx] = current.values[predecessorIdx];

            current.keys[predecessorIdx] = tmpKey;
            current.values[predecessorIdx] = tmpVal;

            return current;
        }


        private void DeleteKeyFromInnerNode(Node node, byte[] key)
        {
            Node currNodeWithKey = SwitchKeysWithLeaf(node, key);
            if (currNodeWithKey != null)
            {
                DeleteKeyFromLeaf(currNodeWithKey, key);
            }

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
