﻿using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text.RegularExpressions;

namespace EmailDomainValidator
{
    public class EmailDomainValidator
    {
        private static readonly MemoryCache Cache = new MemoryCache(new MemoryCacheOptions());

        public static bool HasValidMxRecords(string email)
        {
            var domain = email.Split('@')[1];
            if (Cache.TryGetValue(domain, out bool hasMxRecords))
            {
                return hasMxRecords;
            }

            try
            {
                var mxRecords = Dns.GetHostEntry(domain).AddressList;
                hasMxRecords = mxRecords.Any();
                Cache.Set(domain, hasMxRecords, TimeSpan.FromHours(1));
                return hasMxRecords;
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> HasValidMxRecordsAsync(string email)
        {
            var domain = email.Split('@')[1];
            if (Cache.TryGetValue(domain, out bool hasMxRecords))
            {
                return hasMxRecords;
            }

            try
            {
                var hostEntry = await Dns.GetHostEntryAsync(domain);
                hasMxRecords = hostEntry.AddressList.Any();
                Cache.Set(domain, hasMxRecords, TimeSpan.FromHours(1));
                return hasMxRecords;
            }
            catch
            {
                return false;
            }
        }

        public static bool ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            if (!IsValidFormat(email))
                return false;

            if (IsDisposableEmail(email))
                return false;

            if (!HasValidMxRecords(email))
                return false;

            return true;
        }

        private static readonly Regex EmailRegex = new Regex(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled);

        public static bool IsValidFormat(string email)
        {
            return EmailRegex.IsMatch(email);
        }

        public static bool IsDisposableEmail(string email)
        {
            var domain = email.Split('@')[1];
            return _emailBlockList.Value.Contains(domain);
        }

        private static readonly Lazy<HashSet<string>> _emailBlockList = new Lazy<HashSet<string>>(() =>
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "EmailDomainValidator", "disposable_email_blocklist.conf");
            dir = Path.GetFullPath(dir);

            if (Cache.TryGetValue(dir, out object? cachedValue) && cachedValue is HashSet<string> disposableEmails)
            {
                return disposableEmails;
            }

            var lines = File.ReadLines(dir)
              .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("//"));
            var newDisposableEmails = new HashSet<string>(lines, StringComparer.OrdinalIgnoreCase);
            Cache.Set(dir, newDisposableEmails, TimeSpan.FromHours(1));
            return newDisposableEmails;
        });
    }
}
