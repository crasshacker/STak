using System;
using System.Linq;
using System.Reflection;
using Autofac.Core.Activators.Reflection;

namespace STak.TakHub.Infrastructure
{
    public class InjectionConstructorFinder : IConstructorFinder
    {
        // To support both the default ASP.NET Core DI container and Autofac, we currently use only
        // public constructors.  (The ASP.NET Core DI container requires constructors to be public.)
        public ConstructorInfo[] FindConstructors(Type t) =>
            t.GetTypeInfo().DeclaredConstructors.Where(c => c.IsPublic).ToArray();

        // If we were to commit to using Autofac we could use this to find only internal constructors,
        // and change the access of the relevant injectable classes from public to internal.
        //
        // public ConstructorInfo[] FindConstructors(Type t) => t.GetTypeInfo().DeclaredConstructors.Where(
        //                                                      c => !c.IsPrivate && !c.IsPublic).ToArray();
    }
}
