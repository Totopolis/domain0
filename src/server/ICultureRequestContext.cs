using System.Globalization;

namespace Domain0.Service
{
    public interface ICultureRequestContext
    {
        CultureInfo Culture { get; }
    }
}
