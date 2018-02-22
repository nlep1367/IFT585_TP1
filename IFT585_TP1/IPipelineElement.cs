using System;
using System.Collections.Concurrent;

namespace IFT585_TP1
{
    // Pipeline elements interface
    public interface IPipelineElement
    {
        // Set the input stream for the element
        void SetInput(BlockingCollection<Trame> inputStream);

        // Set the output stream for the element
        void SetOutput(BlockingCollection<Trame> outputStream);

        // The element's processing function 
        void Process();
    }
}
