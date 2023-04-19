using System;
using System.Collections.Generic;

namespace Echo.Graphing.Analysis.Traversal
{
    /// <summary>
    /// Provides a mechanism to record all parent nodes during a traversal.
    /// </summary>
    /// <remarks>
    /// A node is a parent of another node if it is the parent in the search tree. 
    /// </remarks>
    public class ParentRecorder
    {
        private readonly Dictionary<INode, IEdge> _parents = new();

        /// <summary>
        /// Creates a new parent recorder.
        /// </summary>
        /// <param name="traversal">The traversal to hook into.</param>
        /// <exception cref="ArgumentNullException">Occurs when the provided traversal is <c>null</c>.</exception>
        public ParentRecorder(ITraversal traversal)
        {
            if (traversal == null)
                throw new ArgumentNullException(nameof(traversal));
            traversal.NodeDiscovered += (sender, args) =>
            {
                if (!_parents.ContainsKey(args.NewNode) && args.Origin is not null)
                    _parents[args.NewNode] = args.Origin;
            };
        }

        /// <summary>
        /// Gets the edge that was traversed when discovering the provided node.
        /// </summary>
        /// <param name="node">The node to get the edge to its parent from.</param>
        /// <returns>The edge originating from the parent node, or <c>null</c> if the node was the first node to be discovered.</returns>
        public IEdge? GetParentEdge(INode node)
        {
            _parents.TryGetValue(node, out var edge);
            return edge;
        }

        /// <summary>
        /// Gets the parent of the provided node in the search tree that was recorded.
        /// </summary>
        /// <param name="node">The node to get the parent node from.</param>
        /// <returns>The parent node in the search tree, or <c>null</c> if the node was the first node to be discovered.</returns>
        public INode? GetParent(INode node) => GetParentEdge(node)?.Origin;
    }
}