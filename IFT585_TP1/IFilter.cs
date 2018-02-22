using System;
namespace IFT585_TP1
{
    /// A filter to be registered in the message processing pipeline
    public interface IFilter<T>
    {
        /// Filter implementing this method would perform processing on the input type T
        T Execute(T input);
    }
}
