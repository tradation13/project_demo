using AutoMapper;
using IPTS.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IPTS.Services
{
    public abstract class BaseService<TEntity> where TEntity : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly IMapper _mapper;
        protected readonly DbSet<TEntity> _dbSet;

        public BaseService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
            _dbSet = _context.Set<TEntity>();
        }

        public async Task<List<TViewModel>> GetAllAsync<TViewModel>(
           Func<IQueryable<TEntity>, IQueryable<TEntity>>? includeFunc = null)
        {
            var entities = await GetAllAsync(includeFunc);
            return _mapper.Map<List<TViewModel>>(entities);
        }
        public async Task<List<TEntity>> GetAllAsync(
     Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryFunc = null)
        {
            IQueryable<TEntity> query = _dbSet.AsNoTracking();

            if (queryFunc != null)
                query = queryFunc(query);

            return await query.ToListAsync();
        }

        public Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return _dbSet.AsNoTracking().CountAsync(predicate);
        }

        public async Task<int> CountAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryFunc = null)
        {
            IQueryable<TEntity> query = _dbSet.AsNoTracking();

            if (queryFunc != null)
                query = queryFunc(query);

            return await query.CountAsync();
        }
        public async Task<TEntity?> GetByIdAsync<TKey>(
        TKey id,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? includeFunc = null)
        {
            IQueryable<TEntity> query = _dbSet.AsQueryable();

            if (includeFunc != null)
                query = includeFunc(query);

            var parameter = Expression.Parameter(typeof(TEntity), "e");
            var property = Expression.Property(parameter, "Id");
            var equals = Expression.Equal(
                property,
                Expression.Constant(id, typeof(TKey))
            );

            var lambda = Expression.Lambda<Func<TEntity, bool>>(equals, parameter);

            return await query.FirstOrDefaultAsync(lambda);
        }

        // 2. دالة ترجع ViewModel مع AutoMapper
        public async Task<TViewModel?> GetByIdAsync<TKey, TViewModel>(
            TKey id,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? includeFunc = null)
        {
            var entity = await GetByIdAsync(id, includeFunc);

            if (entity == null) return default;

            return _mapper.Map<TViewModel>(entity);
        }
        public async Task<TViewModel> AddAsync<TViewModel>(TViewModel model)
        {
            var entity = _mapper.Map<TEntity>(model);
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return _mapper.Map<TViewModel>(entity);
        }
         public async Task<TEntity> AddAsync(TEntity model)
        {
            await _dbSet.AddAsync(model);
            await _context.SaveChangesAsync();
            return model;
        }

        public async Task<TViewModel?> UpdateAsync<TViewModel>(int id, TViewModel model)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null) return default;

            _mapper.Map(model, entity);
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();

            return _mapper.Map<TViewModel>(entity);
        }
        public async Task<TEntity?> UpdateAsync(int id, TEntity model)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null) return default;

            _context.Entry(entity).CurrentValues.SetValues(model);
            await _context.SaveChangesAsync();

            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null) return false;

            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> IsExistAsync(Expression<Func<TEntity,bool>> FilterFunction)
        {
            return await _dbSet.AnyAsync(FilterFunction);
        }
    }

}
