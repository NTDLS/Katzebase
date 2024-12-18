﻿using NTDLS.Katzebase.Api.Payloads.Response;
using NTDLS.Katzebase.Engine.Atomicity;

namespace NTDLS.Katzebase.Engine.Interactions.Management
{
    internal class TransactionReference : IDisposable
    {
        internal Transaction Transaction { get; private set; }

        private bool _isCommittedOrRolledBack = false;
        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal void Rollback()
        {
            if (_isCommittedOrRolledBack == false)
            {
                _isCommittedOrRolledBack = true;
                Transaction.Rollback();
            }
        }

        internal void Commit()
        {
            if (_isCommittedOrRolledBack == false)
            {
                _isCommittedOrRolledBack = true;
                if (Transaction.Commit())
                {
                    Transaction.Dispose();
                }
            }
        }

        internal KbActionResponse CommitAndApplyMetricsNonQuery(int rowCount)
            => CommitAndApplyMetricsThenReturnResults(rowCount);

        internal KbActionResponse CommitAndApplyMetricsNonQuery(KbQueryResult results)
            => CommitAndApplyMetricsThenReturnResults(results.RowCount);


        internal KbActionResponse CommitAndApplyMetricsNonQuery(KbQueryResultCollection results)
            => CommitAndApplyMetricsThenReturnResults(results.Collection.Sum(o => o.RowCount));

        internal KbActionResponse CommitAndApplyMetricsThenReturnResults(int rowCount)
        {
            Commit();

            return new KbActionResponse
            {
                RowCount = rowCount,
                Metrics = Transaction.Instrumentation.ToCollection(),
                Messages = Transaction.Messages,
                Warnings = Transaction.CloneWarnings(),
                Duration = (DateTime.UtcNow - Transaction.StartTime).TotalMilliseconds
            };
        }

        internal KbActionResponse CommitAndApplyMetricsThenReturnResults()
            => CommitAndApplyMetricsThenReturnResults(0);

        internal T CommitAndApplyMetricsThenReturnResults<T>(T result) where T : KbBaseActionResponse
        {
            Commit();

            result.RowCount = 0;
            result.Metrics = Transaction.Instrumentation.ToCollection();
            result.Messages = Transaction.Messages;
            result.Warnings = Transaction.CloneWarnings();
            result.Duration = (DateTime.UtcNow - Transaction.StartTime).TotalMilliseconds;

            return result;
        }

        internal T CommitAndApplyMetricsThenReturnResults<T>(T result, int rowCount) where T : KbBaseActionResponse
        {
            Commit();

            result.RowCount = rowCount;
            result.Metrics = Transaction.Instrumentation.ToCollection();
            result.Messages = Transaction.Messages;
            result.Warnings = Transaction.CloneWarnings();
            result.Duration = (DateTime.UtcNow - Transaction.StartTime).TotalMilliseconds;

            return result;
        }

        internal TransactionReference(Transaction transaction)
        {
            Transaction = transaction;
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                //Rollback Transaction if its still open:
                if (_isCommittedOrRolledBack == false)
                {
                    _isCommittedOrRolledBack = true;
                    Transaction.Rollback();
                }
            }

            _disposed = true;
        }
    }
}

