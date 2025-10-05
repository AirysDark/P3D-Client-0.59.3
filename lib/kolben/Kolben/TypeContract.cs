using System;
using Kolben.Types;

namespace Kolben
{
    internal static class TypeContract
    {
        public static bool Ensure(SObject[] parameters, Type typeContract)
        {
            return Ensure(parameters, new[] { typeContract });
        }

        public static bool Ensure(SObject[] parameters, Type[] typeContract)
        {
            if (parameters.Length >= typeContract.Length)
            {
                var i = 0;

                while (i < parameters.Length)
                {
                    if (i < typeContract.Length && typeContract[i] != SObject.Unbox(parameters[i]).GetType())
                        return false;

                    i++;
                }

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
