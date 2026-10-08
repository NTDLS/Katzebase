using NTDLS.Katzebase.Api.Exceptions;
using NTDLS.Katzebase.Api.Payloads;

namespace NTDLS.Katzebase.Api.Management
{
    /// <summary>
    /// Explicit transactions.
    ///
    /// A session has at most one transaction. Begin() may be called while a transaction is already open (e.g. by a helper
    /// method), in which case it joins the open transaction: it is only committed when every Begin() has been matched by a
    /// Commit(), but a single Rollback() rolls back the entire transaction.
    ///
    /// The server rolls a transaction back automatically when it is chosen as a deadlock victim, times out waiting on a
    /// lock, or the session ends. Committing after that throws, so a lost transaction is never mistaken for a committed one.
    /// </summary>
    public class KbTransactionClient
    {
        private readonly KbClient _client;

        internal KbTransactionClient(KbClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Begins (or joins) the session's transaction. Dispose of the returned scope to roll the transaction back unless it
        /// was committed:
        /// <code>
        /// using (var transaction = client.Transaction.Begin())
        /// {
        ///     ...
        ///     transaction.Commit();
        /// }
        /// </code>
        /// </summary>
        public KbTransactionScope Begin(TimeSpan? queryTimeout = null)
        {
            _client.Send(new KbQueryTransactionBegin(_client.ServerConnectionId), queryTimeout);
            return new KbTransactionScope(this);
        }

        /// <summary>
        /// Begins (or joins) the session's transaction. See <see cref="Begin"/>.
        /// </summary>
        public async Task<KbTransactionScope> BeginAsync(TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
        {
            await _client.SendAsync(new KbQueryTransactionBegin(_client.ServerConnectionId), queryTimeout, cancellationToken).ConfigureAwait(false);
            return new KbTransactionScope(this);
        }

        /// <summary>
        /// Commits the session's transaction (or, if Begin() was called more than once, ends one level of it).
        /// Throws <see cref="KbTransactionCancelledException"/> if there is no open transaction, for example because the server
        /// rolled it back after a deadlock.
        /// </summary>
        public void Commit(TimeSpan? queryTimeout = null)
            => _client.Send(new KbQueryTransactionCommit(_client.ServerConnectionId), queryTimeout);

        /// <summary>
        /// Commits the session's transaction. See <see cref="Commit"/>.
        /// </summary>
        public Task CommitAsync(TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => _client.SendAsync(new KbQueryTransactionCommit(_client.ServerConnectionId), queryTimeout, cancellationToken);

        /// <summary>
        /// Rolls back the session's entire transaction. Does nothing if there is no open transaction.
        /// </summary>
        public void Rollback(TimeSpan? queryTimeout = null)
            => _client.Send(new KbQueryTransactionRollback(_client.ServerConnectionId), queryTimeout);

        /// <summary>
        /// Rolls back the session's entire transaction. Does nothing if there is no open transaction.
        /// </summary>
        public Task RollbackAsync(TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
            => _client.SendAsync(new KbQueryTransactionRollback(_client.ServerConnectionId), queryTimeout, cancellationToken);
    }

    /// <summary>
    /// A transaction begun with <see cref="KbTransactionClient.Begin"/>. Disposing it rolls the transaction back unless
    /// <see cref="Commit"/> or <see cref="Rollback"/> has already been called.
    /// </summary>
    public sealed class KbTransactionScope : IDisposable, IAsyncDisposable
    {
        private readonly KbTransactionClient _transactions;
        private bool _isCompleted;

        internal KbTransactionScope(KbTransactionClient transactions)
        {
            _transactions = transactions;
        }

        public void Commit(TimeSpan? queryTimeout = null)
        {
            EnsureNotCompleted();
            _isCompleted = true;
            _transactions.Commit(queryTimeout);
        }

        public async Task CommitAsync(TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
        {
            EnsureNotCompleted();
            _isCompleted = true;
            await _transactions.CommitAsync(queryTimeout, cancellationToken).ConfigureAwait(false);
        }

        public void Rollback(TimeSpan? queryTimeout = null)
        {
            EnsureNotCompleted();
            _isCompleted = true;
            _transactions.Rollback(queryTimeout);
        }

        public async Task RollbackAsync(TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
        {
            EnsureNotCompleted();
            _isCompleted = true;
            await _transactions.RollbackAsync(queryTimeout, cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            if (_isCompleted == false)
            {
                _isCompleted = true;
                try
                {
                    _transactions.Rollback();
                }
                catch (KbConnectionException)
                {
                    //Not connected: the server rolls back the session's transaction when the connection is lost.
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_isCompleted == false)
            {
                _isCompleted = true;
                try
                {
                    await _transactions.RollbackAsync().ConfigureAwait(false);
                }
                catch (KbConnectionException)
                {
                    //Not connected: the server rolls back the session's transaction when the connection is lost.
                }
            }
        }

        private void EnsureNotCompleted()
        {
            if (_isCompleted)
            {
                throw new KbTransactionCancelledException("This transaction scope has already been committed or rolled back.");
            }
        }
    }
}
