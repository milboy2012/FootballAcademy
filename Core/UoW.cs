using Core.Entity;
using Core.Interfaces;
using Core.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class UoW : IUoW
    {
        private readonly ContextAuth _context;
        private bool _disposed;

        //репозитории
        private IGenericRepo<Coach> _coach;
        private IGenericRepo<Player> _player;
        private IGenericRepo<Group> _group;
        private IGenericRepo<Manager> _manager;
        private IGenericRepo<Parent> _parent;

        

        public UoW(ContextAuth context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public IRepo<Coach> Coaches
        {
            get { return _coach ?? new GenericRepo<Coach>(_context); }
        }
        public IRepo<Player> Players{ 
            get { return _player ?? new GenericRepo<Player>(_context); }
        }

        public IRepo<Group> Groups
        {
            get { return _group ?? new GenericRepo<Group>(_context); }
        }
        public IRepo<Manager> Managers
        {
            get { return _manager ?? new GenericRepo<Manager>(_context); }
        }
        public IRepo<Parent> Parents
        {
            get { return _parent ?? new GenericRepo<Parent>(_context); }
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _context.Dispose();
            }
            _disposed = true;
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(true);
            GC.SuppressFinalize(this);
        }
        protected virtual async ValueTask DisposeAsync(bool disposing)
        {
            if (!_disposed && disposing)
            {
                await _context.DisposeAsync();
            }
            _disposed = true;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
        {
            return await SaveChangesAsync(cancellationToken) > 0;
        }

    }
}
