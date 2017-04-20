/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using SMBLibrary.NetBios;
using Utilities;
using log4net;

namespace SMBLibrary.Server
{
    internal delegate void LogDelegate(Severity severity, string message);

    internal class ConnectionState
    {
        public Socket ClientSocket;
        public IPEndPoint ClientEndPoint;
        public NBTConnectionReceiveBuffer ReceiveBuffer;
        public BlockingQueue<SessionPacket> SendQueue;
        public SMBDialect Dialect;
        public object AuthenticationContext;

        public ConnectionState()
        {
            ReceiveBuffer = new NBTConnectionReceiveBuffer();
            SendQueue = new BlockingQueue<SessionPacket>();
            Dialect = SMBDialect.NotSet;
        }

        public ConnectionState(ConnectionState state)
        {
            ClientSocket = state.ClientSocket;
            ClientEndPoint = state.ClientEndPoint;
            ReceiveBuffer = state.ReceiveBuffer;
            SendQueue = state.SendQueue;
            Dialect = state.Dialect;
        }

        /// <summary>
        /// Free all resources used by the active sessions in this connection
        /// </summary>
        public virtual void CloseSessions()
        {
        }

        public virtual List<SessionInformation> GetSessionsInformation()
        {
            return new List<SessionInformation>();
        }

        public void LogToServer(ILog logger, Severity severity, string message, Exception exception = null)
        {
            message = String.Format("[{0}] {1}", ConnectionIdentifier, message);
            switch (severity)
            {
                case Severity.Critical:
                    logger.Fatal(message, exception);
                    break;
                case Severity.Error:
                    logger.Error(message, exception);
                    break;
                case Severity.Warning:
                    logger.Warn(message, exception);
                    break;
                case Severity.Information:
                    logger.Info(message, exception);
                    break;
                case Severity.Verbose:
                case Severity.Debug:
                case Severity.Trace:
                    logger.Debug(message, exception);
                    break;
                default:
                    break;
            }

        }

        public void LogToServer(ILog logger, Severity severity, string message, params object[] args)
        {
            LogToServer(logger, severity, String.Format(message, args));
        }

        public string ConnectionIdentifier
        {
            get
            {
                if (ClientEndPoint != null)
                {
                    return ClientEndPoint.Address + ":" + ClientEndPoint.Port;
                }
                return String.Empty;
            }
        }
    }
}
