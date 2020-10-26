using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using STak.TakHub.Core.Interfaces.Gateways.Repositories;

namespace STak.TakHub.Core.Specifications
{
    public abstract class BaseSpecification<T> : ISpecification<T>
    {
        public Expression<Func<T, bool>>         Criteria       { get; }
        public List<Expression<Func<T, object>>> Includes       { get; } = new List<Expression<Func<T, object>>>();
        public List<string>                      IncludeStrings { get; } = new List<string>();


        protected BaseSpecification(Expression<Func<T, bool>> criteria)
        {
            Criteria = criteria;
        }


        protected virtual void AddInclude(Expression<Func<T, object>> includeExpression)
        {
            Includes.Add(includeExpression);
        }


        protected virtual void AddInclude(string includeString)
        {
            IncludeStrings.Add(includeString);
        }
    }
}
