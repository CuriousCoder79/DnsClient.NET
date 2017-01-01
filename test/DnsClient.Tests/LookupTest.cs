﻿using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DnsClient.Protocol;
using Xunit;

namespace DnsClient.Tests
{
    public class LookupTest
    {
        [Fact]
        public void Lookup_Defaults()
        {
            var client = new LookupClient();

            Assert.True(client.UseCache);
            Assert.False(client.EnableAuditTrail);
            Assert.Null(client.MimimumCacheTimeout);
            Assert.True(client.Recursion);
            Assert.False(client.ThrowDnsErrors);
            Assert.Equal(client.Retries, 5);
            Assert.Equal(client.Timeout, TimeSpan.FromSeconds(5));
            Assert.True(client.UseTcpFallback);
            Assert.False(client.UseTcpOnly);
        }

        [Fact]
        public void Lookup_Query_InvalidTimeout()
        {
            var client = new LookupClient();
            
            Action act = () => client.Timeout = TimeSpan.FromMilliseconds(0);

            Assert.ThrowsAny<ArgumentOutOfRangeException>(act);
        }
        
        [Fact]
        public async Task Lookup_GetHostAddresses_Local()
        {
            var client = new LookupClient();

            var result = await client.QueryAsync("localhost", QueryType.A);

            var answer = result.Answers.OfType<ARecord>().First();
            Assert.Equal("127.0.0.1", answer.Address.ToString());
            Assert.Equal(QueryClass.IN, result.Questions.First().QuestionClass);
            Assert.Equal(QueryType.A, result.Questions.First().QuestionType);
            Assert.True(result.Header.AnswerCount > 0);
        }

        [Fact]
        public async Task Lookup_GetHostAddresses_LocalReverse_NoResult()
        {
            // expecting no result as reverse lookup must be explicit
            var client = new LookupClient() { Timeout = TimeSpan.FromMilliseconds(500) };

            client.EnableAuditTrail = true;
            var result = await client.QueryAsync("127.0.0.1", QueryType.A);

            Assert.Equal(QueryClass.IN, result.Questions.First().QuestionClass);
            Assert.Equal(QueryType.A, result.Questions.First().QuestionType);
            Assert.True(result.Header.AnswerCount == 0);
        }

        [Fact]
        public void Lookup_ThrowDnsErrors()
        {
            var client = new LookupClient();
            client.ThrowDnsErrors = true;

            Action act = () => client.QueryAsync("lalacom", (QueryType)12345).GetAwaiter().GetResult();

            var ex = Record.Exception(act) as DnsResponseException;

            // make sure the complex try catch in ResolveQuery doesn't re throw with a messed up message/stack.
            Assert.Equal(ex.Code, DnsResponseCode.NotExistentDomain);
        }

        private async Task<IPAddress> GetDnsEntryAsync()
        {
            // retries the normal host name (without domain)
            var hostname = Dns.GetHostName();
            var hostIp = await Dns.GetHostAddressesAsync(hostname);

            // find the actual IP of the adapter used for inter networking
            var ip = hostIp.Where(p => p.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).First();
            return ip;
        }

        [Fact]
        public async Task Lookup_Reverse()
        {
            var client = new LookupClient();
            var result = await client.QueryReverseAsync(IPAddress.Parse("127.0.0.1"));

            Assert.Equal(result.Answers.PtrRecords().First().PtrDomainName, new DnsName("localhost"));
        }

        [Fact]
        public async Task Lookup_Query_AAAA()
        {
            var client = new LookupClient();
            var result = await client.QueryAsync("google.com", QueryType.AAAA);

            Assert.NotEmpty(result.Answers.AaaaRecords());
            Assert.NotNull(result.Answers.AaaaRecords().First().Address);
        }

        [Fact]
        public async Task Lookup_Query_Any()
        {
            var client = new LookupClient();
            var result = await client.QueryAsync("google.com", QueryType.ANY);

            Assert.NotEmpty(result.Answers);
            Assert.NotEmpty(result.Answers.ARecords());
            Assert.NotEmpty(result.Answers.NsRecords());
        }

        [Fact]
        public async Task Lookup_Query_Mx()
        {
            var client = new LookupClient();
            var result = await client.QueryAsync("google.com", QueryType.MX);

            Assert.NotEmpty(result.Answers.MxRecords());
            Assert.NotNull(result.Answers.MxRecords().First().Exchange);
            Assert.True(result.Answers.MxRecords().First().Preference > 0);
        }

        [Fact]
        public async Task Lookup_Query_NS()
        {
            var client = new LookupClient();
            var result = await client.QueryAsync("google.com", QueryType.NS);

            Assert.NotEmpty(result.Answers.NsRecords());
            Assert.NotNull(result.Answers.NsRecords().First().NSDName);
        }

        [Fact]
        public async Task Lookup_Query_TXT()
        {
            var client = new LookupClient();
            var result = await client.QueryAsync("google.com", QueryType.TXT);

            Assert.NotEmpty(result.Answers.TxtRecords());
            Assert.NotEmpty(result.Answers.TxtRecords().First().Text);
        }

        [Fact]
        public async Task Lookup_Query_SOA()
        {
            var client = new LookupClient();
            var result = await client.QueryAsync("google.com", QueryType.SOA);

            Assert.NotEmpty(result.Answers.SoaRecords());
            Assert.NotNull(result.Answers.SoaRecords().First().MName);
            Assert.NotNull(result.Answers.SoaRecords().First().RName);
        }
    }
}