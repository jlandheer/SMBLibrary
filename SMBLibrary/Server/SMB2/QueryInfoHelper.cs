/* Copyright (C) 2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using log4net;
using SMBLibrary.SMB2;
using System.Reflection;
using Utilities;

namespace SMBLibrary.Server.SMB2
{
    internal class QueryInfoHelper
    {
        private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        internal static SMB2Command GetQueryInfoResponse(QueryInfoRequest request, ISMBShare share, SMB2ConnectionState state)
        {
            SMB2Session session = state.GetSession(request.Header.SessionID);
            if (request.InfoType == InfoType.File)
            {
                OpenFileObject openFile = session.GetOpenFileObject(request.FileId);
                if (openFile == null)
                {
                    return new ErrorResponse(request.CommandName, NTStatus.STATUS_FILE_CLOSED);
                }

                if (share is FileSystemShare)
                {
                    if (!((FileSystemShare)share).HasReadAccess(session.SecurityContext, openFile.Path))
                    {
                        state.LogToServer(logger, Severity.Verbose, "GetFileInformation on '{0}{1}' failed. User '{2}' was denied access.", share.Name, openFile.Path, session.UserName);
                        return new ErrorResponse(request.CommandName, NTStatus.STATUS_ACCESS_DENIED);
                    }
                }

                FileInformation fileInformation;
                NTStatus queryStatus = share.FileStore.GetFileInformation(out fileInformation, openFile.Handle, request.FileInformationClass);
                if (queryStatus != NTStatus.STATUS_SUCCESS)
                {
                    state.LogToServer(logger, Severity.Verbose, "GetFileInformation on '{0}{1}' failed. Information class: {2}, NTStatus: {3}", share.Name, openFile.Path, request.FileInformationClass, queryStatus);
                    return new ErrorResponse(request.CommandName, queryStatus);
                }

                state.LogToServer(logger, Severity.Information, "GetFileInformation on '{0}{1}' succeeded. Information class: {2}", share.Name, openFile.Path, request.FileInformationClass);
                QueryInfoResponse response = new QueryInfoResponse();
                response.SetFileInformation(fileInformation);
                return response;
            }
            else if (request.InfoType == InfoType.FileSystem)
            {
                if (share is FileSystemShare)
                {
                    if (!((FileSystemShare)share).HasReadAccess(session.SecurityContext, @"\"))
                    {
                        state.LogToServer(logger, Severity.Verbose, "GetFileSystemInformation on '{0}' failed. User '{1}' was denied access.", share.Name, session.UserName);
                        return new ErrorResponse(request.CommandName, NTStatus.STATUS_ACCESS_DENIED);
                    }

                    FileSystemInformation fileSystemInformation;
                    NTStatus queryStatus = share.FileStore.GetFileSystemInformation(out fileSystemInformation, request.FileSystemInformationClass);
                    if (queryStatus != NTStatus.STATUS_SUCCESS)
                    {
                        state.LogToServer(logger, Severity.Verbose, "GetFileSystemInformation on '{0}' failed. Information class: {1}, NTStatus: {2}", share.Name, request.FileSystemInformationClass, queryStatus);
                        return new ErrorResponse(request.CommandName, queryStatus);
                    }

                    state.LogToServer(logger, Severity.Information, "GetFileSystemInformation on '{0} succeeded. Information class: {1}", share.Name, request.FileSystemInformationClass);
                    QueryInfoResponse response = new QueryInfoResponse();
                    response.SetFileSystemInformation(fileSystemInformation);
                    return response;
                }
            }
            return new ErrorResponse(request.CommandName, NTStatus.STATUS_NOT_SUPPORTED);
        }
    }
}
