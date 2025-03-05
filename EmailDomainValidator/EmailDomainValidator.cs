using System.Net;
using System.Text.RegularExpressions;

namespace EmailDomainValidator
{
    public class EmailDomainValidator
    {
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

        public static bool HasValidMxRecords(string email)
        {
            var domain = email.Split('@')[1];
            try
            {
                var mxRecords = Dns.GetHostEntry(domain).AddressList;
                return mxRecords.Any();
            }
            catch
            {
                return false;
            }
        }

        private static readonly Lazy<HashSet<string>> _emailBlockList = new Lazy<HashSet<string>>(() =>
        {
            //var dir = $"{AppDomain.CurrentDomain.BaseDirectory}\\disposable_email_blocklist.conf";
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "EmailDomainValidator", "disposable_email_blocklist.conf");
            dir = Path.GetFullPath(dir);

            var lines = File.ReadLines(dir)
              .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("//"));
            return new HashSet<string>(lines, StringComparer.OrdinalIgnoreCase);
        });

        private static bool IsBlocklisted(string domain) => _emailBlockList.Value.Contains(domain);

    }
}
