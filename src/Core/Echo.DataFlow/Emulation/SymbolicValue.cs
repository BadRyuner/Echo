using System.Collections.Generic;
using System.Linq;
using Echo.Core.Values;

namespace Echo.DataFlow.Emulation
{
    /// <summary>
    /// Represents a symbolic value that resides in memory. 
    /// </summary>
    public class SymbolicValue<T> : DataDependencyBase<T>, IValue
    {
        /// <summary>
        /// Creates a new symbolic value with no data sources.
        /// </summary>
        public SymbolicValue()
        {
        }

        /// <summary>
        /// Creates a new symbolic value with a single data source.
        /// </summary>
        /// <param name="dataSource">The data source of the symbolic value.</param>
        public SymbolicValue(DataFlowNode<T> dataSource)
            : this(new DataSource<T>(dataSource, 0))
        {
        }

        /// <summary>
        /// Creates a new symbolic value with a single data source.
        /// </summary>
        /// <param name="dataSource">The data source of the symbolic value.</param>
        public SymbolicValue(DataSource<T> dataSource)
            : base(dataSource)
        {
        }
        /// <summary>
        /// Creates a new symbolic value with the provided data sources.
        /// </summary>
        /// <param name="dataSources">The data sources of the symbolic value.</param>
        public SymbolicValue(IEnumerable<DataFlowNode<T>> dataSources)
            : this(dataSources.Select(node => new DataSource<T>(node)))
        {
        }
        
        /// <summary>
        /// Creates a new symbolic value with the provided data sources.
        /// </summary>
        /// <param name="dataSources">The data sources of the symbolic value.</param>
        public SymbolicValue(IEnumerable<DataSource<T>> dataSources)
            : base(dataSources.AsEnumerable())
        {
        }

        /// <inheritdoc />
        public override bool IsReadOnly => true;
        
        /// <inheritdoc />
        public bool IsKnown => HasKnownDataSources;

        /// <inheritdoc />
        public int Size => 0;

        /// <summary>
        /// Creates an exact copy of the value.
        /// </summary>
        /// <returns>The copied value.</returns>
        public SymbolicValue<T> Copy() => new(this);

        IValue IValue.Copy() => Copy();
    }
}