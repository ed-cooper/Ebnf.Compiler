    /// <summary>
    /// Represents a node in a parse tree.
    /// </summary>
    public class Node
    {
        #region Properties

        /// <summary>
        /// Gets or sets the string text that this node represents.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the type of this node.
        /// </summary>
        public NodeType TypeName { get; set; }

        /// <summary>
        /// Gets or sets the collection of child nodes to this node.
        /// </summary>
        public List<Node> Children { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new instance of the <see cref="Node"/> class.
        /// </summary>
        public Node()
        {
            Children = new List<Node>();
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Node"/> class.
        /// </summary>
        /// <param name="typeName">The type of the node.</param>
        public Node(NodeType typeName)
        {
            Value = "";
            Children = new List<Node>();
            TypeName = typeName;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString()
        {
            return TypeName.ToString() + ": Value";
        }

        #endregion
    }