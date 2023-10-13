using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChedVX.UI.Operations
{
    /// <summary>
    /// Represents an operation consisting of multiple <see cref="IOperation"/>s.
    /// </summary>
    public class CompositeOperation : IOperation
    {
        public string Description { get; }

        protected IEnumerable<IOperation> Operations { get; }

        /// <summary>
        /// Initialize this <see cref="CompositeOperation"/> from the operation description and <see cref="IEnumerable{IOperation}"/>.
        /// </summary>
        /// <param name="description">Description of this operation</param>
        /// <param name="operations"><see cref="IEnumerable{IOperation}"/></param> sorted by operations
        public CompositeOperation(string description, IEnumerable<IOperation> operations)
        {
            Description = description;
            Operations = operations;
        }

        public void Redo()
        {
            foreach (var op in Operations) op.Redo();
        }

        public void Undo()
        {
            foreach (var op in Operations.Reverse()) op.Undo();
        }
    }
}
