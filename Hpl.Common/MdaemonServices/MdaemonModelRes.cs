using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Hpl.Common.MdaemonServices
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Rootobject
    {
        public Mdaemon MDaemon { get; set; }
    }

    public class Mdaemon
    {
        public API API { get; set; }
    }

    public class API
    {
        public Response Response { get; set; }
    }

    public class Response
    {
        public string version { get; set; }
        public string session { get; set; }
        public string et { get; set; }
        public Status Status { get; set; }
        public string ServiceVersion { get; set; }
        public Result Result { get; set; }
    }

    public class Status
    {
        public string id { get; set; }
        public string value { get; set; }
        public string message { get; set; }
    }

    public class Result
    {
        public User User { get; set; }
    }


    public class Details
    {
        public string Email { get; set; }
        public Password Password { get; set; }
        public DateTime Created { get; set; }
        public Lastlogon LastLogon { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DisplayName { get; set; }
        public string Body { get; set; }
        public string Frozen { get; set; }
        public string Disabled { get; set; }
        public string MustChangePassword { get; set; }
        public string DontExpirePassword { get; set; }
        public object NTAccount { get; set; }
        public string Comments { get; set; }
        public string MailDir { get; set; }
    }

    public class Password
    {
        public string note { get; set; }
    }

    public class Lastlogon
    {
        public DateTime Date { get; set; }
        public string IPAddress { get; set; }
    }

    public class Listmembership
    {
        public List[] List { get; set; }
    }

    public class List
    {
        public string id { get; set; }
    }

    public class Permissions
    {
        public string AccessWorldClient { get; set; }
        public string AccessRemoteAdministration { get; set; }
        public string EditFullname { get; set; }
        public string EditPassword { get; set; }
        public string EditMailDir { get; set; }
        public string EditFwd { get; set; }
        public string EditAdvFwd { get; set; }
        public string EditEveryone { get; set; }
        public string EditMailRestrictions { get; set; }
        public string EditQuotas { get; set; }
        public string EditMultiPop { get; set; }
        public string EditAutoResponder { get; set; }
        public string EditImapRules { get; set; }
        public string EditMailbox { get; set; }
        public string EditAttachmentHandling { get; set; }
        public string EditAliases { get; set; }
        public string EditMobileDevice { get; set; }
    }

    public class Protocols
    {
        public POP POP { get; set; }
        public IMAP IMAP { get; set; }
        public Multipop MultiPOP { get; set; }
    }

    public class POP
    {
        public string Enabled { get; set; }
        public string RestrictedToLAN { get; set; }
    }

    public class IMAP
    {
        public string Enabled { get; set; }
        public string RestrictedToLAN { get; set; }
    }

    public class Multipop
    {
        public string Enabled { get; set; }
        public string MaxMessageAge { get; set; }
        public string MaxMessageSize { get; set; }
    }

    public class Autoresponder
    {
        public string Enabled { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public Response1 Response { get; set; }
        public Daysactive DaysActive { get; set; }
    }

    public class Response1
    {
        public string cdatasection { get; set; }
    }

    public class Daysactive
    {
        public string mask { get; set; }
        public object Sunday { get; set; }
        public object Monday { get; set; }
        public object Tuesday { get; set; }
        public object Wednesday { get; set; }
        public object Thursday { get; set; }
        public object Friday { get; set; }
        public object Saturday { get; set; }
    }

    public class Forwarding
    {
        public string Enabled { get; set; }
        public string RetainMail { get; set; }
        public object Host { get; set; }
        public object SendAs { get; set; }
        public string Port { get; set; }
        public Addresses Addresses { get; set; }
    }

    public class Addresses
    {
        public string Address { get; set; }
    }

    public class Restrictions
    {
        public Inbound Inbound { get; set; }
        public Outbound Outbound { get; set; }
    }

    public class Inbound
    {
        public string Enabled { get; set; }
    }

    public class Outbound
    {
        public string Enabled { get; set; }
    }

    public class Quotas
    {
        public string ApplyQuotas { get; set; }
        public string MaxMessageCount { get; set; }
        public string MaxDiskSpace { get; set; }
        public string MaxSentPerDay { get; set; }
        public Usage Usage { get; set; }
    }

    public class Usage
    {
        public string Items { get; set; }
        public string DiskSpace { get; set; }
    }

    public class Pruning
    {
        public string UseDefaultPruning { get; set; }
        public string MaxInactive { get; set; }
        public string MaxMessageAge { get; set; }
        public string MaxDeletedIMAPMessageAge { get; set; }
        public string RecurseIMAP { get; set; }
    }

    public class Worldclient
    {
        public string Enabled { get; set; }
        public string RestrictedToLAN { get; set; }
        public string TFAllowed { get; set; }
        public string TFRequired { get; set; }
        public string TFEnabled { get; set; }
    }

    public class Remoteadmin
    {
        public string Enabled { get; set; }
        public string RestrictedToLAN { get; set; }
        public string EditFullname { get; set; }
        public string EditPassword { get; set; }
        public string EditMailDir { get; set; }
        public string EditFwd { get; set; }
        public string EditAdvFwd { get; set; }
        public string EditEveryone { get; set; }
        public string EditMailRestrictions { get; set; }
        public string EditQuotas { get; set; }
        public string EditMultiPop { get; set; }
        public string EditAutoResponder { get; set; }
        public string EditImapRules { get; set; }
        public string EditMailbox { get; set; }
        public string EditAttachmentHandling { get; set; }
        public string EditAliases { get; set; }
        public string EditMobileDevice { get; set; }
    }

    public class Signatures
    {
        public string Text { get; set; }
        public string HTML { get; set; }
        public object Disclaimer { get; set; }
    }

    public class Roles
    {
        public string IsGlobalAdmin { get; set; }
    }

    public class Options
    {
        public string HideFromEveryone { get; set; }
        public string AutoDecode { get; set; }
        public string CheckAddrBook { get; set; }
        public string UpdateAddrBook { get; set; }
        public object UserDefined { get; set; }
        public object TemplateName { get; set; }
        public string TemplateFlags { get; set; }
        public string AttachmentLinking { get; set; }
        public string ExtractOutbound { get; set; }
        public string ExtractInbound { get; set; }
        public string EnableSubaddressing { get; set; }
        public string ApplyDomainSignature { get; set; }
        public string ExemptFromAuthMatch { get; set; }
        public string ProcessCalendarRequests { get; set; }
        public string DeclineConflictingRequests { get; set; }
        public string DeclineRecurringRequests { get; set; }
    }

    public class EAS
    {
        public string Enabled { get; set; }
        public Settings Settings { get; set; }
    }

    public class Settings
    {
        public string LogXml { get; set; }
        public string LogWbXml { get; set; }
        public string OmitWhiteList { get; set; }
        public string OmitBlackList { get; set; }
        public string OmitNonDefaultEmail { get; set; }
        public string OmitNonDefaultPim { get; set; }
        public string PublicFoldersEnabled { get; set; }
        public string SharedFoldersEnabled { get; set; }
        public string SendReadReceipts { get; set; }
        public string RequestReadReceipts { get; set; }
        public string FlaggedMailTaskCreation { get; set; }
        public string AllowThirdPartyMgmt { get; set; }
        public string EnforceProtocolRestrictions { get; set; }
        public string MaxPublicFolders { get; set; }
        public string BandwidthResetDOM { get; set; }
        public string MaxClientsPerUser { get; set; }
        public string LenientFolderSecurity { get; set; }
        public string PublicFolderSearchEnabled { get; set; }
        public string SharedFolderSearchEnabled { get; set; }
        public string ValidatePimIntegrity { get; set; }
    }

    public class Other
    {
        public string deprecated { get; set; }
        public string version { get; set; }
        public string notes { get; set; }
    }

    #region RESPONSE CỦA DANH SÁCH USER TRÊN SERVER EMAIL. CALL THEO API GetDomainList
    //RESPONSE CỦA DANH SÁCH USER TRÊN SERVER EMAIL. CALL THEO API GetDomainList
    public class Domains
    {
        [JsonProperty("count")]
        public int count { get; set; }
        [JsonProperty("Domain")]
        //public Domain[] Domain { get; set; }
        //Dictionary<string, Domain> Domain { get; set; }
        public List<Domain> Domain { get; set; }
    }

    public class Domain
    {
        [JsonProperty("id")]
        public string id { get; set; }
        [JsonProperty("Users")]
        public Users Users { get; set; }
    }

    public class Users
    {
        [JsonProperty("count")]
        public int count { get; set; }
        [JsonProperty("User")]
        //public User[] User { get; set; }
        //Dictionary<string, User> User { get; set; }
        public List<User> User { get; set; }
    }

    public class User
    {
        [JsonProperty("id")]
        public string? id { get; set; }
    }
    #endregion

}