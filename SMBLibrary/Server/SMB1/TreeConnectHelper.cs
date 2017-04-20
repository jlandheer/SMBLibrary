/* Copyright (C) 2014-2017 Tal Aloni <tal.aloni.il@gmail.com>. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using log4net;
using SMBLibrary.SMB1;
using Utilities;
using System;
using System.Reflection;

namespace SMBLibrary.Server.SMB1
{
    internal class TreeConnectHelper
    {
        private static readonly ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        internal static SMB1Command GetTreeConnectResponse(SMB1Header header, TreeConnectAndXRequest request, SMB1ConnectionState state, NamedPipeShare services, SMBShareCollection shares)
        {
            SMB1Session session = state.GetSession(header.UID);
            bool isExtended = (request.Flags & TreeConnectFlags.ExtendedResponse) > 0;
            string shareName = ServerPathUtils.GetShareName(request.Path);
            ISMBShare share;
            ServiceName serviceName;
            OptionalSupportFlags supportFlags;
            if (String.Equals(shareName, NamedPipeShare.NamedPipeShareName, StringComparison.InvariantCultureIgnoreCase))
            {
                share = services;
                serviceName = ServiceName.NamedPipe;
                supportFlags = OptionalSupportFlags.SMB_SUPPORT_SEARCH_BITS | OptionalSupportFlags.SMB_CSC_NO_CACHING;
            }
            else
            {
                share = shares.GetShareFromName(shareName);
                serviceName = ServiceName.DiskShare;
                supportFlags = OptionalSupportFlags.SMB_SUPPORT_SEARCH_BITS | OptionalSupportFlags.SMB_CSC_CACHE_MANUAL_REINT;
                if (share == null)
                {
                    header.Status = NTStatus.STATUS_OBJECT_PATH_NOT_FOUND;
                    return new ErrorResponse(request.CommandName);
                }

                if (!((FileSystemShare)share).HasReadAccess(session.SecurityContext, @"\"))
                {
                    state.LogToServer(logger, Severity.Verbose, "Tree Connect to '{0}' failed. User '{1}' was denied access.", share.Name, session.UserName);
                    header.Status = NTStatus.STATUS_ACCESS_DENIED;
                    return new ErrorResponse(request.CommandName);
                }
            }
            ushort? treeID = session.AddConnectedTree(share);
            if (!treeID.HasValue)
            {
                header.Status = NTStatus.STATUS_INSUFF_SERVER_RESOURCES;
                return new ErrorResponse(request.CommandName);
            }
            state.LogToServer(logger, Severity.Information, "Tree Connect: User '{0}' connected to '{1}'", session.UserName, share.Name);
            header.TID = treeID.Value;
            if (isExtended)
            {
                return CreateTreeConnectResponseExtended(serviceName, supportFlags);
            }
            else
            {
                return CreateTreeConnectResponse(serviceName, supportFlags);
            }
        }

        private static TreeConnectAndXResponse CreateTreeConnectResponse(ServiceName serviceName, OptionalSupportFlags supportFlags)
        {
            TreeConnectAndXResponse response = new TreeConnectAndXResponse();
            response.OptionalSupport = supportFlags;
            response.NativeFileSystem = String.Empty;
            response.Service = serviceName;
            return response;
        }

        private static TreeConnectAndXResponseExtended CreateTreeConnectResponseExtended(ServiceName serviceName, OptionalSupportFlags supportFlags)
        {
            TreeConnectAndXResponseExtended response = new TreeConnectAndXResponseExtended();
            response.OptionalSupport = supportFlags;
            response.MaximalShareAccessRights.File = FileAccessMask.FILE_READ_DATA | FileAccessMask.FILE_WRITE_DATA | FileAccessMask.FILE_APPEND_DATA |
                                                        FileAccessMask.FILE_READ_EA | FileAccessMask.FILE_WRITE_EA |
                                                        FileAccessMask.FILE_EXECUTE |
                                                        FileAccessMask.FILE_READ_ATTRIBUTES | FileAccessMask.FILE_WRITE_ATTRIBUTES |
                                                        FileAccessMask.DELETE | FileAccessMask.READ_CONTROL | FileAccessMask.WRITE_DAC | FileAccessMask.WRITE_OWNER | FileAccessMask.SYNCHRONIZE;
            response.GuestMaximalShareAccessRights.File = FileAccessMask.FILE_READ_DATA | FileAccessMask.FILE_WRITE_DATA |
                                                            FileAccessMask.FILE_READ_EA | FileAccessMask.FILE_WRITE_EA |
                                                            FileAccessMask.FILE_READ_ATTRIBUTES | FileAccessMask.FILE_WRITE_ATTRIBUTES |
                                                            FileAccessMask.READ_CONTROL | FileAccessMask.SYNCHRONIZE;
            response.NativeFileSystem = String.Empty;
            response.Service = serviceName;
            return response;
        }

        internal static SMB1Command GetTreeDisconnectResponse(SMB1Header header, TreeDisconnectRequest request, ISMBShare share, SMB1ConnectionState state)
        {
            SMB1Session session = state.GetSession(header.UID);
            session.DisconnectTree(header.TID);
            state.LogToServer(logger, Severity.Information, "Tree Disconnect: User '{0}' disconnected from '{1}'", session.UserName, share.Name);
            return new TreeDisconnectResponse();
        }
    }
}
