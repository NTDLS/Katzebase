﻿using NTDLS.Katzebase.Api.Payloads;
using NTDLS.Katzebase.Engine.Interactions.Management;
using NTDLS.Katzebase.Engine.Scripts.Models;
using NTDLS.Katzebase.Engine.Sessions;
using NTDLS.ReliableMessaging;
using System.Diagnostics;
using static NTDLS.Katzebase.Shared.EngineConstants;

namespace NTDLS.Katzebase.Engine.Interactions.APIHandlers
{
    /// <summary>
    /// Public class methods for handling API requests related to sessions.
    /// </summary>
    public class SessionAPIHandlers : IRmMessageHandler
    {
        private readonly EngineCore _core;

        public SessionAPIHandlers(EngineCore core)
        {
            _core = core;

            try
            {
            }
            catch (Exception ex)
            {
                LogManager.Error($"Failed to instantiate session API handlers.", ex);
                throw;
            }
        }

        public KbQueryServerStartSessionReply StartSession(RmContext context, KbQueryServerStartSession param)
        {
            SessionState? session = null;

            using var trace = _core.Trace.CreateTracker(TraceType.SessionStart, context.ConnectionId);

#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                if (string.IsNullOrEmpty(param.Username))
                {
                    throw new Exception("No username was specified.");
                }

                using var ephemeral = _core.Sessions.CreateEphemeralSystemSession();
#if DEBUG
                if (param.Username == "debug")
                {
                    ephemeral.Commit();

                    LogManager.Debug($"Logged in mock user [{param.Username}].");

                    session = _core.Sessions.CreateSession(context.ConnectionId, SessionManager.BuiltInSystemUserName, param.ClientName);
                    Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
                    LogManager.Debug(Thread.CurrentThread.Name);

                    var apiResults = new KbQueryServerStartSessionReply
                    {
                        ProcessId = session.ProcessId,
                        ConnectionId = context.ConnectionId,
                        ServerTimeUTC = DateTime.UtcNow
                    };

                    trace.SetSession(session);
                    trace.Result = TraceResult.Success;

                    return apiResults;
                }
#endif
                var account = ephemeral.Transaction.ExecuteQuery<AccountLogin>("AccountLogin.kbs",
                    new
                    {
                        param.Username,
                        param.PasswordHash
                    }).FirstOrDefault();

                ephemeral.Commit();

                if (account != null)
                {
                    LogManager.Debug($"Logged in user [{param.Username}].");

                    session = _core.Sessions.CreateSession(context.ConnectionId, param.Username, param.ClientName);
                    trace.SetSession(session);
#if DEBUG
                    Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
                    LogManager.Debug(Thread.CurrentThread.Name);
#endif
                    var apiResults = new KbQueryServerStartSessionReply
                    {
                        ProcessId = session.ProcessId,
                        ConnectionId = context.ConnectionId,
                        ServerTimeUTC = DateTime.UtcNow
                    };

                    trace.Result = TraceResult.Success;

                    return apiResults;
                }

                throw new Exception("Invalid username or password.");
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session?.ProcessId}].", ex);
                throw;
            }
        }

        public KbQueryServerCloseSessionReply CloseSession(RmContext context, KbQueryServerCloseSession param)
        {
            using var trace = _core.Trace.CreateTracker(TraceType.SessionClose, context.ConnectionId);

            var session = _core.Sessions.GetSession(context.ConnectionId);
            trace.SetSession(session);

#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                _core.Sessions.TryCloseByProcessID(session.ProcessId);

                var apiResults = new KbQueryServerCloseSessionReply();

                trace.Result = TraceResult.Success;

                return apiResults;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }

        public void Heartbeat(RmContext context, KbNotifySessionHeartbeat param)
        {
            //The call to GetSession() will update the LastCheckInTime.
            _core.Sessions.GetSession(context.ConnectionId);
        }

        public KbQueryServerTerminateProcessReply TerminateSession(RmContext context, KbQueryServerTerminateProcess param)
        {
            using var trace = _core.Trace.CreateTracker(TraceType.SessionTerminate, context.ConnectionId);

            var session = _core.Sessions.GetSession(context.ConnectionId);
            trace.SetSession(session);
#if DEBUG
            Thread.CurrentThread.Name = $"KbAPI:{session.ProcessId}:{param.GetType().Name}";
            LogManager.Debug(Thread.CurrentThread.Name);
#endif
            try
            {
                _core.Sessions.TryCloseByProcessID(param.ReferencedProcessId);

                var apiResult = new KbQueryServerTerminateProcessReply();

                trace.Result = TraceResult.Success;

                return apiResult;
            }
            catch (Exception ex)
            {
                LogManager.Error($"{new StackFrame(1).GetMethod()} failed for process: [{session.ProcessId}].", ex);
                throw;
            }
        }
    }
}
