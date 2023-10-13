using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChedVX.UI.Operations
{
    /// <summary>
    /// An interface that represents user operations.
    /// </summary>
    public interface IOperation
    {
        /// <summary>
        /// Get the description of this operation.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Undo the operation.
        /// </summary>
        void Undo();

        /// <summary>
        /// Redo the operation.
        /// </summary>
        void Redo();
    }
}
