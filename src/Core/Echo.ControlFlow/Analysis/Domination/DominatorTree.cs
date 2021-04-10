using System;
using System.Collections.Generic;
using System.Linq;
using Echo.Core.Graphing.Analysis.Traversal;
using Echo.Core.Graphing;

namespace Echo.ControlFlow.Analysis.Domination
{
    /// <summary>
    /// Represents a dominator tree, where each tree node corresponds to one node in a graph, and each
    /// is immediately dominated by its parent.
    /// </summary>
    public class DominatorTree : IGraph
    {
        private readonly IDictionary<IIdentifiedNode, DominatorTreeNode> _nodes;
        private Dictionary<IIdentifiedNode, ISet<IIdentifiedNode>> _frontier;
        private readonly object _frontierSyncLock = new object();
        
        /// <summary>
        /// Constructs a dominator tree from a control flow graph.
        /// </summary>
        /// <param name="graph">The control flow graph to turn into a dominator tree.</param>
        /// <returns>The constructed dominator tree.</returns>
        public static DominatorTree FromGraph<TContents>(ControlFlowGraph<TContents> graph)
        {
            if (graph.Entrypoint == null)
                throw new ArgumentException("Control flow graph does not have an entrypoint.");
            
            var idoms = GetImmediateDominators(graph.Entrypoint);
            var nodes = ConstructTreeNodes(idoms, graph.Entrypoint);
            return new DominatorTree(nodes, graph.Entrypoint);
        }
        
        /// <summary>
        /// Computes the dominator tree of a control flow graph, defined by its entrypoint.
        /// </summary>
        /// <param name="entrypoint">The entrypoint of the control flow graph.</param>
        /// <returns>A dictionary mapping all the nodes to their immediate dominator.</returns>
        /// <remarks>
        /// The algorithm used is based on the one engineered by Lengauer and Tarjan.
        /// https://www.cs.princeton.edu/courses/archive/fall03/cs528/handouts/a%20fast%20algorithm%20for%20finding.pdf
        /// https://www.cl.cam.ac.uk/~mr10/lengtarj.pdf
        /// </remarks> 
        private static IDictionary<IIdentifiedNode, IIdentifiedNode> GetImmediateDominators(IIdentifiedNode entrypoint)
        {
            var idom = new Dictionary<IIdentifiedNode, IIdentifiedNode>();
            var semi = new Dictionary<IIdentifiedNode, IIdentifiedNode>();
            var ancestor = new Dictionary<IIdentifiedNode, IIdentifiedNode>();
            var bucket = new Dictionary<IIdentifiedNode, ISet<IIdentifiedNode>>();

            var traversal = new DepthFirstTraversal();
            var order = new TraversalOrderRecorder(traversal);
            var parents = new ParentRecorder(traversal);
            traversal.Run(entrypoint);

            var orderedNodes = order.GetTraversal();
            foreach (var node in orderedNodes.Cast<IIdentifiedNode>())
            {
                idom[node] = null;
                semi[node] = node;
                ancestor[node] = null;
                bucket[node] = new HashSet<IIdentifiedNode>();
            }

            for (int i = orderedNodes.Count - 1; i >= 1; i--)
            {
                var current = (IIdentifiedNode) orderedNodes[i];
                var parent = (IIdentifiedNode) parents.GetParent(current);

                // step 2
                foreach (var predecessor in current.GetPredecessors().Cast<IIdentifiedNode>())
                {
                    var u = Eval(predecessor, ancestor, semi, order);
                    if (order.GetIndex(semi[current]) > order.GetIndex(semi[u]))
                        semi[current] = semi[u];
                }

                bucket[semi[current]].Add(current);
                Link(parent, current, ancestor);
                
                // step 3
                foreach (var bucketNode in bucket[parent])
                {
                    var u = Eval(bucketNode, ancestor, semi, order);
                    if (order.GetIndex(semi[u]) < order.GetIndex(semi[bucketNode]))
                        idom[bucketNode] = u;
                    else
                        idom[bucketNode] = parent;
                }

                bucket[parent].Clear();
            }

            // step 4
            for (int i = 1; i < orderedNodes.Count; i++)
            {
                var w = (IIdentifiedNode) orderedNodes[i];
                if (idom[w] != semi[w])
                    idom[w] = idom[idom[w]];
            }

            idom[entrypoint] = entrypoint;
            return idom;
        }
        
        /// <summary>
        /// Constructs a dominator tree from the control flow graph.
        /// </summary>
        /// <returns>The constructed tree. Each node added to the tree is linked to a node in the original graph by
        /// its name.</returns>
        private static IDictionary<IIdentifiedNode, DominatorTreeNode> ConstructTreeNodes(IDictionary<IIdentifiedNode, IIdentifiedNode> idoms, IIdentifiedNode entrypoint)
        {
            var result = new Dictionary<IIdentifiedNode, DominatorTreeNode>
            {
                [entrypoint] = new DominatorTreeNode(entrypoint)
            };
            
            foreach (var entry in idoms)
            {
                var dominator = entry.Value;
                var dominated = entry.Key;

                if (dominator != dominated)
                {
                    if (!result.TryGetValue(dominated, out var child))
                        result[dominated] = child = new DominatorTreeNode(dominated);
                    if (!result.TryGetValue(dominator, out var parent))
                        result[dominator] = parent = new DominatorTreeNode(dominator);

                    parent.Children.Add(child);
                }
            }

            return result;
        }

        private static void Link(IIdentifiedNode parent, IIdentifiedNode node, IDictionary<IIdentifiedNode, IIdentifiedNode> ancestors)
        {
            ancestors[node] = parent;
        }

        private static IIdentifiedNode Eval(IIdentifiedNode node, IDictionary<IIdentifiedNode, IIdentifiedNode> ancestors, IDictionary<IIdentifiedNode, IIdentifiedNode> semi, TraversalOrderRecorder order)
        {
            var a = ancestors[node];
            while (a != null && ancestors[a] != null)
            {
                if (order.GetIndex(semi[node]) > order.GetIndex(semi[a]))
                    node = a;
                a = ancestors[a];
            }

            return node;
        }

        private DominatorTree(IDictionary<IIdentifiedNode, DominatorTreeNode> nodes, IIdentifiedNode root)
        {
            _nodes = nodes;
            Root = nodes[root];
        }
        
        /// <summary>
        /// Gets the root of the dominator tree. That is, the tree node that corresponds to the entrypoint of the
        /// control flow graph.
        /// </summary>
        public DominatorTreeNode Root
        {
            get;
        }

        /// <summary>
        /// Gets the dominator tree node associated to the given control flow graph node.
        /// </summary>
        /// <param name="node">The control flow graph node to get the tree node from.</param>
        public DominatorTreeNode this[IIdentifiedNode node] => _nodes[node];

        /// <summary>
        /// Determines whether one control flow graph node dominates another node. That is, whether execution of the
        /// dominated node means the dominator node has to be executed.
        /// </summary>
        /// <param name="dominator">The node that dominates.</param>
        /// <param name="dominated">The node that is potentially dominated.</param>
        /// <returns>
        /// <c>True</c> if the node in <paramref name="dominator"/> indeed dominates the provided control flow
        /// node in <paramref name="dominated"/>, <c>false</c> otherwise.
        /// </returns>
        public bool Dominates(IIdentifiedNode dominator, IIdentifiedNode dominated)
        {
            var current = this[dominated];

            while (current != null)
            {
                if (current.OriginalNode == dominator)
                    return true;
                current = (DominatorTreeNode) current.Parent;
            }

            return false;
        }

        /// <summary>
        /// Determines the dominance frontier of a specific node. That is, the set of all nodes where the dominance of
        /// the specified node stops.
        /// </summary>
        /// <param name="node">The node to obtain the dominance frontier from.</param>
        /// <returns>A collection of nodes representing the dominance frontier.</returns>
        public IEnumerable<IIdentifiedNode> GetDominanceFrontier(IIdentifiedNode node)
        {
            if (_frontier == null)
            {
                lock (_frontierSyncLock)
                {
                    if (_frontier == null)
                        InitializeDominanceFrontiers();
                }
            }

            return _frontier[node];
        }
        
        private void InitializeDominanceFrontiers()
        {
            var frontier = _nodes.Keys.ToDictionary(x => x, _ => (ISet<IIdentifiedNode>) new HashSet<IIdentifiedNode>());
            
            foreach (var node in _nodes.Keys)
            {
                var predecessors = node
                    .GetPredecessors()
                    .Cast<IIdentifiedNode>()
                    .ToArray();
                
                if (predecessors.Length >= 2)
                {
                    foreach (var p in predecessors)
                    {
                        var runner = p;
                        while (runner != ((DominatorTreeNode) _nodes[node].Parent).OriginalNode)
                        {
                            frontier[runner].Add(node);
                            runner = ((DominatorTreeNode) _nodes[runner].Parent).OriginalNode;
                        }
                    }
                }
            }

            _frontier = frontier;
        }

        /// <inheritdoc />
        IEnumerable<INode> ISubGraph.GetNodes() => _nodes.Values;

        /// <inheritdoc />
        public IEnumerable<ISubGraph> GetSubGraphs() => Enumerable.Empty<ISubGraph>();

        /// <inheritdoc />
        IEnumerable<IEdge> IGraph.GetEdges() => 
            _nodes.Values.SelectMany(n => ((IIdentifiedNode) n).GetOutgoingEdges());
    }
}