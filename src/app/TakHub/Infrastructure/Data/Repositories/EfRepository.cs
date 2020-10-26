using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using STak.TakHub.Core.Interfaces.Gateways.Repositories;
using STak.TakHub.Core.Shared;

namespace STak.TakHub.Infrastructure.Data.Repositories
{
    public abstract class EfRepository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly AppDbContext m_appDbContext;


        protected EfRepository(AppDbContext appDbContext)
        {
            m_appDbContext = appDbContext;
        }


        public virtual async Task<T> GetById(int id)
        {
            return await m_appDbContext.Set<T>().FindAsync(id);
        }


        public async Task<List<T>> ListAll()
        {
            return await m_appDbContext.Set<T>().ToListAsync();
        }


        public async Task<T> GetSingleBySpec(ISpecification<T> spec)
        {
            var result = await List(spec);
            return result.FirstOrDefault();
        }


        public async Task<List<T>> List(ISpecification<T> spec)
        {
            // fetch a Queryable that includes all expression-based includes
            var queryableResultWithIncludes = spec.Includes
                .Aggregate(m_appDbContext.Set<T>().AsQueryable(),
                    (current, include) => current.Include(include));

            // modify the IQueryable to include any string-based include statements
            var secondaryResult = spec.IncludeStrings
                .Aggregate(queryableResultWithIncludes,
                    (current, include) => current.Include(include));

            // return the result of the query using the specification's criteria expression
            return await secondaryResult
                            .Where(spec.Criteria)
                            .ToListAsync();
        }


        public async Task<T> Add(T entity)
        {
            m_appDbContext.Set<T>().Add(entity);
            await m_appDbContext.SaveChangesAsync();
            return entity;
        }


        public async Task Update(T entity)
        {
            m_appDbContext.Entry(entity).State = EntityState.Modified;
            await m_appDbContext.SaveChangesAsync();
        }


        public async Task Delete(T entity)
        {
            m_appDbContext.Set<T>().Remove(entity);
            await m_appDbContext.SaveChangesAsync();
        }
    }
}
