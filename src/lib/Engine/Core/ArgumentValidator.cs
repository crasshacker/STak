using System;

namespace STak.TakEngine
{
    public static class ArgumentValidator
    {
        public static void EnsureNotNull(object obj, string name)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(name);
            }
        }
    }
}
