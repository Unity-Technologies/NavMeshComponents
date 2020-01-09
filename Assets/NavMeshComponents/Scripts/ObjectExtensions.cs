using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class ObjectExtensions
{
    public static bool TryCast<TIn, TOut>(this TIn obj, out TOut result)
        where TIn : class
        where TOut : class
    {
        result = obj as TOut;
        return result != null;
    }
}
